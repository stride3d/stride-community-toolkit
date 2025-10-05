using Stride.CommunityToolkit.Collections;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using System.Runtime.InteropServices;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.CommunityToolkit.DebugShapes.Code;

/// <summary>
/// Renders immediate-mode debug primitives (quads, circles, spheres, cubes, capsules, cylinders, cones, lines).
/// </summary>
internal sealed class DebugPrimitiveRenderer
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct InstanceData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public Color Color;
    }

    private Buffer? _vertexBuffer;
    private Buffer? _indexBuffer;
    private Buffer? _transformBuffer;
    private Buffer? _colorBuffer;
    private Buffer? _lineVertexBuffer;
    private MutablePipelineState? _pipelineState;
    private DynamicEffectInstance? _primitiveEffect;
    private DynamicEffectInstance? _lineEffect;

    private PrimitiveGeometryCache? _geometry;
    private PrimitiveInstanceStore? _store;
    private PrimitivePipeline? _pipeline;

    private InputElementDescription[] _inputElements = [];
    private InputElementDescription[] _lineInputElements = [];

    /// <summary>
    /// Builds GPU buffers, effects, and input layouts required for debug rendering.
    /// Call once from InitializeCore.
    /// </summary>
    /// <param name="device">Graphics device.</param>
    /// <param name="services">Service registry used to initialize effect instances.</param>
    internal void Initialize(GraphicsDevice device, IServiceRegistry services)
    {
        _inputElements = VertexPositionTexture.Layout.CreateInputElements();
        _lineInputElements = LineVertex.Layout.CreateInputElements();

        _pipelineState = new MutablePipelineState(device);
        _pipelineState.State.SetDefaults();

        _primitiveEffect = new DynamicEffectInstance("PrimitiveShader");
        _primitiveEffect.Initialize(services);
        _primitiveEffect.UpdateEffect(device);

        _lineEffect = new DynamicEffectInstance("LinePrimitiveShader");
        _lineEffect.Initialize(services);
        _lineEffect.UpdateEffect(device);

        _geometry = new PrimitiveGeometryCache();
        var vertexData = _geometry.BuildVertexData();
        var indexData = _geometry.BuildIndexData();

        // create vertex and index buffers
        _vertexBuffer = Buffer.Vertex.New(device, vertexData);
        if (indexData.Length >= 0xFFFF && device.Features.CurrentProfile <= GraphicsProfile.Level_9_3)
            throw new InvalidOperationException("Cannot generate more than 65535 indices on feature level HW <= 9.3");
        _indexBuffer = Buffer.Index.New(device, indexData);

        // allocate structured buffers
        _transformBuffer = Buffer.Structured.New<Matrix>(device, 1);
        _colorBuffer = Buffer.Structured.New<Color>(device, 1);
        _lineVertexBuffer = Buffer.Vertex.New(device, new LineVertex[1], GraphicsResourceUsage.Dynamic);

        _pipeline = new PrimitivePipeline(_pipelineState, _primitiveEffect, _lineEffect, _inputElements, _lineInputElements);
        _store = new PrimitiveInstanceStore();
    }

    /// <summary>
    /// Ensures internal instance buffers can hold the expected number of primitives for this frame.
    /// </summary>
    internal void EnsureCapacity(int additionalInstances, int additionalLineVertices)
        => _store!.EnsureCapacity(additionalInstances, additionalLineVertices);

    /// <summary>
    /// Processes renderable commands and writes instance data into internal buffers.
    /// Offsets are updated in-place to reflect consumption.
    /// </summary>
    internal void ProcessRenderables(List<Renderable> renderables, ref Primitives offsets)
        => _store!.ProcessRenderables(renderables, ref offsets);

    /// <summary>
    /// Computes the sum of non-line primitive counts. Used to pre-size instance buffers.
    /// </summary>
    internal static int SumBasicPrimitives(ref Primitives primitives)
    {
        return primitives.Quads
            + primitives.Circles
            + primitives.Spheres
            + primitives.HalfSpheres
            + primitives.Cubes
            + primitives.Capsules
            + primitives.Cylinders
            + primitives.Cones;
    }

    /// <summary>
    /// Computes per-primitive offsets for a packed instances array.
    /// </summary>
    internal static Primitives SetupPrimitiveOffsets(ref Primitives counts, int offset = 0)
    {
        var offsets = new Primitives();
        offsets.Quads = 0 + offset;
        offsets.Circles = offsets.Quads + counts.Quads;
        offsets.Spheres = offsets.Circles + counts.Circles;
        offsets.HalfSpheres = offsets.Spheres + counts.Spheres;
        offsets.Cubes = offsets.HalfSpheres + counts.HalfSpheres;
        offsets.Capsules = offsets.Cubes + counts.Cubes;
        offsets.Cylinders = offsets.Capsules + counts.Capsules;
        offsets.Cones = offsets.Cylinders + counts.Cylinders;
        return offsets;
    }

    /// <summary>
    /// Prepares per-instance transforms and uploads buffer data to the GPU.
    /// Call once per frame before Draw.
    /// </summary>
    internal void Prepare(RenderDrawContext context)
    {
        var store = _store!;
        int count = store._instances.Count;
        store._transforms.SetCount(count);
        store._colors.SetCount(count);

        Core.Threading.Dispatcher.For(0, count, (i) =>
        {
            var instance = store._instances[i];
            Matrix.Transformation(ref instance.Scale, ref instance.Rotation, ref instance.Position, out var m);
            store._transforms[i] = m;
            store._colors[i] = instance.Color;
        });

        CheckBuffers(context);

        store._lineVertices.Clear();
        store._instances.Clear();
    }

    private void CheckBuffers(RenderDrawContext context)
    {
        var store = _store!;
        var transformsSpan = CollectionsMarshal.AsSpan(store._transforms);
        UpdateBufferIfNecessary<Matrix>(context.GraphicsDevice, context.CommandList, ref _transformBuffer!, transformsSpan);

        var colorsSpan = CollectionsMarshal.AsSpan(store._colors);
        UpdateBufferIfNecessary<Color>(context.GraphicsDevice, context.CommandList, ref _colorBuffer!, colorsSpan);

        var lineVertsSpan = CollectionsMarshal.AsSpan(store._lineVertices);
        UpdateBufferIfNecessary<LineVertex>(context.GraphicsDevice, context.CommandList, ref _lineVertexBuffer!, lineVertsSpan);
    }

    private static void UpdateBufferIfNecessary<T>(GraphicsDevice device, CommandList commandList, ref Buffer buffer, ReadOnlySpan<T> data) where T : unmanaged
    {
        if (data.IsEmpty)
            return; // nothing to upload, keep previous content

        int neededBufferSize = data.Length;
        if (neededBufferSize > buffer.ElementCount)
        {
            buffer.Dispose();
            var newBuffer = Buffer.New(device, data, buffer.Flags, PixelFormat.None, buffer.Usage);
            buffer = newBuffer;
        }
        else
        {
            buffer.SetData(commandList, data);
        }
    }

    /// <summary>
    /// Draws all requested primitives for a given render view.
    /// </summary>
    /// <param name="context">Draw context.</param>
    /// <param name="renderView">View providing the view-projection matrix.</param>
    /// <param name="offsets">Instance offset for each primitive type.</param>
    /// <param name="counts">Counts for each primitive type.</param>
    /// <param name="depthTest">Enable or disable depth test.</param>
    /// <param name="fillMode">Triangle fill mode.</param>
    /// <param name="hasTransparency">Enable blend for transparency when true.</param>
    internal void DrawPrimitives(RenderDrawContext context, RenderView renderView, ref Primitives offsets, ref Primitives counts, bool depthTest, FillMode fillMode, bool hasTransparency)
    {
        var commandList = context.CommandList;
        var geometry = _geometry!;
        var store = _store!;

        // bind buffers
        commandList.SetVertexBuffer(0, _vertexBuffer, 0, VertexPositionTexture.Layout.VertexStride);
        commandList.SetIndexBuffer(_indexBuffer, 0, is32bits: true);
        commandList.SetPipelineState(_pipelineState!.CurrentState);

        // set effect params common to all primitive draws
        _primitiveEffect!.Parameters.Set(PrimitiveShaderKeys.LineWidthMultiplier, fillMode == FillMode.Solid ? 10000.0f : 1.0f);
        _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.ViewProjection, renderView.ViewProjection);
        _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.Transforms, _transformBuffer);
        _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.Colors, _colorBuffer);

        _primitiveEffect.UpdateEffect(context.GraphicsDevice);
        _primitiveEffect.Apply(context.GraphicsContext);

        // spheres
        if (counts.Spheres > 0)
        {
            _pipeline!.ConfigurePrimitivePipeline(commandList, depthTest, fillMode, isDoubleSided: false, hasTransparency);
            commandList.SetPipelineState(_pipelineState.CurrentState);
            _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Spheres);
            _primitiveEffect.Apply(context.GraphicsContext);
            commandList.DrawIndexedInstanced(geometry.SphereIndexCount, counts.Spheres, geometry.IndexOffsets.Spheres, geometry.VertexOffsets.Spheres);
        }

        // quads / circles / half spheres (double-sided)
        if (counts.Quads > 0 || counts.Circles > 0 || counts.HalfSpheres > 0)
        {
            _pipeline!.ConfigurePrimitivePipeline(commandList, depthTest, fillMode, isDoubleSided: true, hasTransparency);
            commandList.SetPipelineState(_pipelineState.CurrentState);

            if (counts.Quads > 0)
            {
                _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Quads);
                _primitiveEffect.Apply(context.GraphicsContext);
                commandList.DrawIndexedInstanced(geometry.QuadIndexCount, counts.Quads, geometry.IndexOffsets.Quads, geometry.VertexOffsets.Quads);
            }

            if (counts.Circles > 0)
            {
                _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Circles);
                _primitiveEffect.Apply(context.GraphicsContext);
                commandList.DrawIndexedInstanced(geometry.CircleIndexCount, counts.Circles, geometry.IndexOffsets.Circles, geometry.VertexOffsets.Circles);
            }

            if (counts.HalfSpheres > 0)
            {
                _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.HalfSpheres);
                _primitiveEffect.Apply(context.GraphicsContext);
                commandList.DrawIndexedInstanced(geometry.HalfSphereIndexCount, counts.HalfSpheres, geometry.IndexOffsets.HalfSpheres, geometry.VertexOffsets.HalfSpheres);
            }
        }

        // cubes / capsules / cylinders / cones (single-sided)
        if (counts.Cubes > 0 || counts.Capsules > 0 || counts.Cylinders > 0 || counts.Cones > 0)
        {
            _pipeline!.ConfigurePrimitivePipeline(commandList, depthTest, fillMode, isDoubleSided: false, hasTransparency);
            commandList.SetPipelineState(_pipelineState.CurrentState);

            if (counts.Cubes > 0)
            {
                _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Cubes);
                _primitiveEffect.Apply(context.GraphicsContext);
                commandList.DrawIndexedInstanced(geometry.CubeIndexCount, counts.Cubes, geometry.IndexOffsets.Cubes, geometry.VertexOffsets.Cubes);
            }

            if (counts.Capsules > 0)
            {
                _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Capsules);
                _primitiveEffect.Apply(context.GraphicsContext);
                commandList.DrawIndexedInstanced(geometry.CapsuleIndexCount, counts.Capsules, geometry.IndexOffsets.Capsules, geometry.VertexOffsets.Capsules);
            }

            if (counts.Cylinders > 0)
            {
                _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Cylinders);
                _primitiveEffect.Apply(context.GraphicsContext);
                commandList.DrawIndexedInstanced(geometry.CylinderIndexCount, counts.Cylinders, geometry.IndexOffsets.Cylinders, geometry.VertexOffsets.Cylinders);
            }

            if (counts.Cones > 0)
            {
                _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Cones);
                _primitiveEffect.Apply(context.GraphicsContext);
                commandList.DrawIndexedInstanced(geometry.ConeIndexCount, counts.Cones, geometry.IndexOffsets.Cones, geometry.VertexOffsets.Cones);
            }
        }

        // lines
        if (counts.Lines > 0)
        {
            _pipeline!.ConfigureLinePipeline(commandList, depthTest, hasTransparency);
            commandList.SetVertexBuffer(0, _lineVertexBuffer, 0, LineVertex.Layout.VertexStride);
            commandList.SetPipelineState(_pipelineState.CurrentState);

            _lineEffect!.Parameters.Set(LinePrimitiveShaderKeys.ViewProjection, renderView.ViewProjection);
            _lineEffect.UpdateEffect(context.GraphicsDevice);
            _lineEffect.Apply(context.GraphicsContext);

            commandList.Draw(counts.Lines * 2, offsets.Lines);
        }
    }

    /// <summary>
    /// Releases GPU resources and effect instances. Safe to call multiple times.
    /// </summary>
    internal void Unload()
    {
        _transformBuffer?.Dispose();
        _colorBuffer?.Dispose();
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _lineVertexBuffer?.Dispose();
        _primitiveEffect?.Dispose();
        _lineEffect?.Dispose();
        _transformBuffer = null;
        _colorBuffer = null;
        _vertexBuffer = null;
        _indexBuffer = null;
        _lineVertexBuffer = null;
        _pipelineState = null;
        _primitiveEffect = null;
        _lineEffect = null;
        _geometry = null;
        _store = null;
        _pipeline = null;
    }

    internal void SetPrimitiveRenderingPipelineState(CommandList commandList, bool depthTest, FillMode selectedFillMode, bool hasTransparency)
        => _pipeline!.ConfigurePrimitivePipeline(commandList, depthTest, selectedFillMode, isDoubleSided: false, hasTransparency);
}
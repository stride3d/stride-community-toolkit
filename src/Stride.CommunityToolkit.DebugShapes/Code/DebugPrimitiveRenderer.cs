using Stride.CommunityToolkit.Collections;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using System.Runtime.InteropServices;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.CommunityToolkit.DebugShapes;

/// <summary>
/// Renders immediate-mode debug primitives (quads, circles, spheres, cubes, capsules, cylinders, cones, lines).
/// </summary>
internal sealed class DebugPrimitiveRenderer
{
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

    // Made once: a lambda handed to the dispatcher every frame would allocate a closure each time
    private Action<int>? _prepareInstance;

    /// <summary>
    /// Builds GPU buffers, effects, and input layouts required for debug rendering.
    /// Call once from InitializeCore.
    /// </summary>
    /// <param name="device">Graphics device.</param>
    /// <param name="services">Service registry used to initialize effect instances.</param>
    internal void Initialize(GraphicsDevice device, IServiceRegistry services)
    {
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

        if (indexData.Length >= 0xFFFF && device.Features.CurrentProfile <= GraphicsProfile.Level_9_3)
            throw new InvalidOperationException("Cannot generate more than 65535 indices on feature level HW <= 9.3");

        _vertexBuffer = Buffer.Vertex.New(device, vertexData);
        _indexBuffer = Buffer.Index.New(device, indexData);

        // The instance and line buffers start at one element and grow as frames need
        _transformBuffer = Buffer.Structured.New<Matrix>(device, 1);
        _colorBuffer = Buffer.Structured.New<Color>(device, 1);
        _lineVertexBuffer = Buffer.Vertex.New(device, new LineVertex[1], GraphicsResourceUsage.Dynamic);

        _pipeline = new PrimitivePipeline(_pipelineState, _primitiveEffect, _lineEffect, VertexPositionTexture.Layout.CreateInputElements(), LineVertex.Layout.CreateInputElements());
        _store = new PrimitiveInstanceStore();
        _prepareInstance = PrepareInstance;
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
    /// Prepares per-instance transforms and uploads buffer data to the GPU.
    /// Call once per frame before Draw.
    /// </summary>
    internal void Prepare(RenderDrawContext context)
    {
        var store = _store!;
        var count = store.Instances.Count;

        store.Transforms.SetCount(count);
        store.Colors.SetCount(count);

        Core.Threading.Dispatcher.For(0, count, _prepareInstance!);

        CheckBuffers(context);

        store.LineVertices.Clear();
        store.Instances.Clear();
    }

    // One instance's world matrix and colour, from the dispatcher's worker threads
    private void PrepareInstance(int i)
    {
        var store = _store!;
        var instance = store.Instances[i];

        Matrix.Transformation(ref instance.Scale, ref instance.Rotation, ref instance.Position, out var m);
        store.Transforms[i] = m;
        store.Colors[i] = instance.Color;
    }

    private void CheckBuffers(RenderDrawContext context)
    {
        var store = _store!;

        UpdateBufferIfNecessary<Matrix>(context.GraphicsDevice, context.CommandList, ref _transformBuffer!, CollectionsMarshal.AsSpan(store.Transforms));
        UpdateBufferIfNecessary<Color>(context.GraphicsDevice, context.CommandList, ref _colorBuffer!, CollectionsMarshal.AsSpan(store.Colors));
        UpdateBufferIfNecessary<LineVertex>(context.GraphicsDevice, context.CommandList, ref _lineVertexBuffer!, CollectionsMarshal.AsSpan(store.LineVertices));
    }

    // Grows the buffer to the data when it is too small, else uploads in place; nothing to upload keeps the previous content
    private static void UpdateBufferIfNecessary<T>(GraphicsDevice device, CommandList commandList, ref Buffer buffer, ReadOnlySpan<T> data) where T : unmanaged
    {
        if (data.IsEmpty) return;

        if (data.Length > buffer.ElementCount)
        {
            buffer.Dispose();
            buffer = Buffer.New(device, data, buffer.Flags, PixelFormat.None, buffer.Usage);
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

        // bind buffers
        commandList.SetVertexBuffer(0, _vertexBuffer, 0, VertexPositionTexture.Layout.VertexStride);
        commandList.SetIndexBuffer(_indexBuffer, 0, is32bits: true);
        commandList.SetPipelineState(_pipelineState!.CurrentState);

        // set effect params common to all primitive draws
        _primitiveEffect!.Parameters.Set(PrimitiveShaderKeys.LineWidthMultiplier, fillMode == FillMode.Solid ? 10000.0f : 1.0f);
        _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.ViewProjection, renderView.ViewProjection);
        _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.Transforms, _transformBuffer);
        _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.Colors, _colorBuffer);
        // Colours are sRGB bytes; decoded in the shader only when the backbuffer is sRGB (the default)
        _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.LinearOutput, context.GraphicsDevice.ColorSpace == ColorSpace.Linear ? 1u : 0u);

        _primitiveEffect.UpdateEffect(context.GraphicsDevice);
        _primitiveEffect.Apply(context.GraphicsContext);

        // one pass per pipeline configuration: spheres draw single-sided on their own, quads,
        // circles and half spheres share a double-sided pass, the remaining volumes a single-sided
        // one, and lines use their own effect and vertex buffer.
        DrawSpheres(context, ref offsets, ref counts, depthTest, fillMode, hasTransparency);
        DrawDoubleSidedPrimitives(context, ref offsets, ref counts, depthTest, fillMode, hasTransparency);
        DrawSingleSidedPrimitives(context, ref offsets, ref counts, depthTest, fillMode, hasTransparency);
        DrawLines(context, renderView, ref offsets, ref counts, depthTest, hasTransparency);
    }

    private void DrawSpheres(RenderDrawContext context, ref Primitives offsets, ref Primitives counts, bool depthTest, FillMode fillMode, bool hasTransparency)
    {
        if (counts.Spheres <= 0) return;

        var geometry = _geometry!;
        _pipeline!.ConfigurePrimitivePipeline(context.CommandList, depthTest, fillMode, isDoubleSided: false, hasTransparency);
        context.CommandList.SetPipelineState(_pipelineState!.CurrentState);

        DrawInstanced(context, offsets.Spheres, counts.Spheres, geometry.SphereIndexCount, geometry.IndexOffsets.Spheres, geometry.VertexOffsets.Spheres);
    }

    private void DrawDoubleSidedPrimitives(RenderDrawContext context, ref Primitives offsets, ref Primitives counts, bool depthTest, FillMode fillMode, bool hasTransparency)
    {
        if (counts.Quads <= 0 && counts.Circles <= 0 && counts.HalfSpheres <= 0) return;

        var geometry = _geometry!;
        _pipeline!.ConfigurePrimitivePipeline(context.CommandList, depthTest, fillMode, isDoubleSided: true, hasTransparency);
        context.CommandList.SetPipelineState(_pipelineState!.CurrentState);

        if (counts.Quads > 0)
        {
            DrawInstanced(context, offsets.Quads, counts.Quads, geometry.QuadIndexCount, geometry.IndexOffsets.Quads, geometry.VertexOffsets.Quads);
        }

        if (counts.Circles > 0)
        {
            DrawInstanced(context, offsets.Circles, counts.Circles, geometry.CircleIndexCount, geometry.IndexOffsets.Circles, geometry.VertexOffsets.Circles);
        }

        if (counts.HalfSpheres > 0)
        {
            DrawInstanced(context, offsets.HalfSpheres, counts.HalfSpheres, geometry.HalfSphereIndexCount, geometry.IndexOffsets.HalfSpheres, geometry.VertexOffsets.HalfSpheres);
        }
    }

    private void DrawSingleSidedPrimitives(RenderDrawContext context, ref Primitives offsets, ref Primitives counts, bool depthTest, FillMode fillMode, bool hasTransparency)
    {
        if (counts.Cubes <= 0 && counts.Capsules <= 0 && counts.Cylinders <= 0 && counts.Cones <= 0) return;

        var geometry = _geometry!;
        _pipeline!.ConfigurePrimitivePipeline(context.CommandList, depthTest, fillMode, isDoubleSided: false, hasTransparency);
        context.CommandList.SetPipelineState(_pipelineState!.CurrentState);

        if (counts.Cubes > 0)
        {
            DrawInstanced(context, offsets.Cubes, counts.Cubes, geometry.CubeIndexCount, geometry.IndexOffsets.Cubes, geometry.VertexOffsets.Cubes);
        }

        if (counts.Capsules > 0)
        {
            DrawInstanced(context, offsets.Capsules, counts.Capsules, geometry.CapsuleIndexCount, geometry.IndexOffsets.Capsules, geometry.VertexOffsets.Capsules);
        }

        if (counts.Cylinders > 0)
        {
            DrawInstanced(context, offsets.Cylinders, counts.Cylinders, geometry.CylinderIndexCount, geometry.IndexOffsets.Cylinders, geometry.VertexOffsets.Cylinders);
        }

        if (counts.Cones > 0)
        {
            DrawInstanced(context, offsets.Cones, counts.Cones, geometry.ConeIndexCount, geometry.IndexOffsets.Cones, geometry.VertexOffsets.Cones);
        }
    }

    /// <summary>
    /// Sets the instance offset, applies the effect and issues one instanced draw.
    /// </summary>
    private void DrawInstanced(RenderDrawContext context, int instanceOffset, int instanceCount, int indexCount, int indexStart, int vertexOffset)
    {
        _primitiveEffect!.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, instanceOffset);
        _primitiveEffect.Apply(context.GraphicsContext);
        context.CommandList.DrawIndexedInstanced(indexCount, instanceCount, indexStart, vertexOffset);
    }

    private void DrawLines(RenderDrawContext context, RenderView renderView, ref Primitives offsets, ref Primitives counts, bool depthTest, bool hasTransparency)
    {
        if (counts.Lines <= 0) return;

        var commandList = context.CommandList;
        _pipeline!.ConfigureLinePipeline(commandList, depthTest, hasTransparency);
        commandList.SetVertexBuffer(0, _lineVertexBuffer, 0, LineVertex.Layout.VertexStride);
        commandList.SetPipelineState(_pipelineState!.CurrentState);

        _lineEffect!.Parameters.Set(LinePrimitiveShaderKeys.ViewProjection, renderView.ViewProjection);
        _lineEffect.Parameters.Set(LinePrimitiveShaderKeys.LinearOutput, context.GraphicsDevice.ColorSpace == ColorSpace.Linear ? 1u : 0u);
        _lineEffect.UpdateEffect(context.GraphicsDevice);
        _lineEffect.Apply(context.GraphicsContext);

        commandList.Draw(counts.Lines * 2, offsets.Lines);
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
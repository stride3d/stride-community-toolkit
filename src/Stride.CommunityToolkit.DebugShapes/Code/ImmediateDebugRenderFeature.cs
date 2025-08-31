// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.CommunityToolkit.Collections;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.DebugRendering;
using Stride.Graphics;
using Stride.Rendering;
using System.Runtime.InteropServices;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.CommunityToolkit.DebugShapes.Code;

/// <summary>
/// Provides functionality for rendering immediate debug primitives such as lines, shapes, and other geometric objects.
/// </summary>
/// <remarks>This class is designed to render debug primitives directly to the screen, bypassing typical rendering
/// pipelines. It supports various geometric shapes, including quads, circles, spheres, cubes, capsules, cylinders,
/// cones, and lines. The rendering process is optimized for debugging purposes, allowing developers to visualize
/// objects in real-time.</remarks>
public class ImmediateDebugRenderFeature : RootRenderFeature
{
    /// <inheritdoc/>
    public override Type SupportedRenderObjectType => typeof(ImmediateDebugRenderObject);

    internal struct Primitives
    {
        public int Quads;
        public int Circles;
        public int Spheres;
        public int HalfSpheres;
        public int Cubes;
        public int Capsules;
        public int Cylinders;
        public int Cones;
        public int Lines;

        public void Clear()
        {
            Quads = 0;
            Circles = 0;
            Spheres = 0;
            HalfSpheres = 0;
            Cubes = 0;
            Capsules = 0;
            Cylinders = 0;
            Cones = 0;
            Lines = 0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct LineVertex
    {
        public static readonly VertexDeclaration Layout = new VertexDeclaration(VertexElement.Position<Vector3>(), VertexElement.Color<Color>());

        public Vector3 Position;
        public Color Color;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct InstanceData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public Color Color;
    }

    private const float DefaultCircleRadius = 0.5f;
    private const float DefaultSphereRadius = 0.5f;
    private const float DefaultCubeSize = 1.0f;
    private const float DefaultCapsuleLength = 1.0f;
    private const float DefaultCapsuleRadius = 0.5f;
    private const float DefaultCylinderHeight = 1.0f;
    private const float DefaultCylinderRadius = 0.5f;
    private const float DefaultPlaneSize = 1.0f;
    private const float DefaultConeRadius = 0.5f;
    private const float DefaultConeHeight = 1.0f;

    private const int CircleTesselation = 16;
    private const int SphereTesselation = 8;
    private const int CapsuleTesselation = 8;
    private const int CylinderTesselation = 16;
    private const int ConeTesselation = 16;

    /* mesh data we will use when stuffing things in vertex buffers */
    private readonly (VertexPositionTexture[] Vertices, int[] Indices) _circle = ImmediateDebugPrimitives.GenerateCircle(DefaultCircleRadius, CircleTesselation);
    private readonly (VertexPositionTexture[] Vertices, int[] Indices) _plane = ImmediateDebugPrimitives.GenerateQuad(DefaultPlaneSize, DefaultPlaneSize);
    private readonly (VertexPositionTexture[] Vertices, int[] Indices) _sphere = ImmediateDebugPrimitives.GenerateSphere(DefaultSphereRadius, SphereTesselation, uvSplitOffsetVertical: 1);
    private readonly (VertexPositionTexture[] Vertices, int[] Indices) _cube = ImmediateDebugPrimitives.GenerateCube(DefaultCubeSize);
    private readonly (VertexPositionTexture[] Vertices, int[] Indices) _capsule = ImmediateDebugPrimitives.GenerateCapsule(DefaultCapsuleLength, DefaultCapsuleRadius, CapsuleTesselation);
    private readonly (VertexPositionTexture[] Vertices, int[] Indices) _cylinder = ImmediateDebugPrimitives.GenerateCylinder(DefaultCylinderHeight, DefaultCylinderRadius, CylinderTesselation);
    private readonly (VertexPositionTexture[] Vertices, int[] Indices) _cone = ImmediateDebugPrimitives.GenerateCone(DefaultConeHeight, DefaultConeRadius, ConeTesselation, uvSplits: 8);

    /* vertex and index buffer for our primitive data */
    private Buffer? _vertexBuffer;
    private Buffer? _indexBuffer;

    /* vertex buffer for line rendering */
    private Buffer? _lineVertexBuffer;

    /* offsets into our vertex/index buffer */
    private Primitives _primitiveVertexOffsets;
    private Primitives _primitiveIndexOffsets;

    /* other gpu related data */
    private MutablePipelineState? _pipelineState;
    private InputElementDescription[] _inputElements = Array.Empty<InputElementDescription>();
    private InputElementDescription[] _lineInputElements = Array.Empty<InputElementDescription>();
    private DynamicEffectInstance? _primitiveEffect;
    private DynamicEffectInstance? _lineEffect;
    private Buffer? _transformBuffer;
    private Buffer? _colorBuffer;

    /* intermediate message related data, written to in extract */
    private readonly List<InstanceData> _instances = new(1);

    /* data written to buffers in prepare */
    private readonly List<Matrix> _transforms = new(1);
    private readonly List<Color> _colors = new(1);

    /* data only for line rendering */
    private readonly List<LineVertex> _lineVertices = new(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmediateDebugRenderFeature"/> class.
    /// </summary>
    /// <remarks>This constructor sets the default sort key to ensure that the feature is rendered last,
    /// preventing background elements from being rendered over objects without depth testing in the opaque
    /// stage.</remarks>
    public ImmediateDebugRenderFeature() => SortKey = 0xFF;

    /// <inheritdoc/>
    protected override void InitializeCore()
    {
        var device = Context.GraphicsDevice;

        _inputElements = VertexPositionTexture.Layout.CreateInputElements();
        _lineInputElements = LineVertex.Layout.CreateInputElements();

        // create our pipeline state object
        _pipelineState = new MutablePipelineState(device);
        _pipelineState.State.SetDefaults();

        // TODO: create our associated effect
        _primitiveEffect = new DynamicEffectInstance("PrimitiveShader");
        _primitiveEffect.Initialize(Context.Services);
        _primitiveEffect.UpdateEffect(device);

        _lineEffect = new DynamicEffectInstance("LinePrimitiveShader");
        _lineEffect.Initialize(Context.Services);
        _lineEffect.UpdateEffect(device);

        {
            // create initial vertex and index buffers
            var vertexData = new VertexPositionTexture[
                _circle.Vertices.Length +
                _plane.Vertices.Length +
                _sphere.Vertices.Length +
                _cube.Vertices.Length +
                _capsule.Vertices.Length +
                _cylinder.Vertices.Length +
                _cone.Vertices.Length
            ];

            /* set up vertex buffer data */

            int vertexBufferOffset = 0;

            Array.Copy(_circle.Vertices, vertexData, _circle.Vertices.Length);
            _primitiveVertexOffsets.Circles = vertexBufferOffset;
            vertexBufferOffset += _circle.Vertices.Length;

            Array.Copy(_plane.Vertices, 0, vertexData, vertexBufferOffset, _plane.Vertices.Length);
            _primitiveVertexOffsets.Quads = vertexBufferOffset;
            vertexBufferOffset += _plane.Vertices.Length;

            Array.Copy(_sphere.Vertices, 0, vertexData, vertexBufferOffset, _sphere.Vertices.Length);
            _primitiveVertexOffsets.Spheres = vertexBufferOffset;
            _primitiveVertexOffsets.HalfSpheres = vertexBufferOffset; // same as spheres
            vertexBufferOffset += _sphere.Vertices.Length;

            Array.Copy(_cube.Vertices, 0, vertexData, vertexBufferOffset, _cube.Vertices.Length);
            _primitiveVertexOffsets.Cubes = vertexBufferOffset;
            vertexBufferOffset += _cube.Vertices.Length;

            Array.Copy(_capsule.Vertices, 0, vertexData, vertexBufferOffset, _capsule.Vertices.Length);
            _primitiveVertexOffsets.Capsules = vertexBufferOffset;
            vertexBufferOffset += _capsule.Vertices.Length;

            Array.Copy(_cylinder.Vertices, 0, vertexData, vertexBufferOffset, _cylinder.Vertices.Length);
            _primitiveVertexOffsets.Cylinders = vertexBufferOffset;
            vertexBufferOffset += _cylinder.Vertices.Length;

            Array.Copy(_cone.Vertices, 0, vertexData, vertexBufferOffset, _cone.Vertices.Length);
            _primitiveVertexOffsets.Cones = vertexBufferOffset;
            vertexBufferOffset += _cone.Vertices.Length;

            _vertexBuffer = Buffer.Vertex.New(device, vertexData);
        }

        {
            /* set up index buffer data */

            var indexData = new int[
                _circle.Indices.Length +
                _plane.Indices.Length +
                _sphere.Indices.Length +
                _cube.Indices.Length +
                _capsule.Indices.Length +
                _cylinder.Indices.Length +
                _cone.Indices.Length
            ];

            if (indexData.Length >= 0xFFFF && device.Features.CurrentProfile <= GraphicsProfile.Level_9_3)
                throw new InvalidOperationException("Cannot generate more than 65535 indices on feature level HW <= 9.3");

            // copy all our primitive data into the buffers

            int indexBufferOffset = 0;

            Array.Copy(_circle.Indices, indexData, _circle.Indices.Length);
            _primitiveIndexOffsets.Circles = indexBufferOffset;
            indexBufferOffset += _circle.Indices.Length;

            Array.Copy(_plane.Indices, 0, indexData, indexBufferOffset, _plane.Indices.Length);
            _primitiveIndexOffsets.Quads = indexBufferOffset;
            indexBufferOffset += _plane.Indices.Length;

            Array.Copy(_sphere.Indices, 0, indexData, indexBufferOffset, _sphere.Indices.Length);
            _primitiveIndexOffsets.Spheres = indexBufferOffset;
            _primitiveIndexOffsets.HalfSpheres = indexBufferOffset; // same as spheres
            indexBufferOffset += _sphere.Indices.Length;

            Array.Copy(_cube.Indices, 0, indexData, indexBufferOffset, _cube.Indices.Length);
            _primitiveIndexOffsets.Cubes = indexBufferOffset;
            indexBufferOffset += _cube.Indices.Length;

            Array.Copy(_capsule.Indices, 0, indexData, indexBufferOffset, _capsule.Indices.Length);
            _primitiveIndexOffsets.Capsules = indexBufferOffset;
            indexBufferOffset += _capsule.Indices.Length;

            Array.Copy(_cylinder.Indices, 0, indexData, indexBufferOffset, _cylinder.Indices.Length);
            _primitiveIndexOffsets.Cylinders = indexBufferOffset;
            indexBufferOffset += _cylinder.Indices.Length;

            Array.Copy(_cone.Indices, 0, indexData, indexBufferOffset, _cone.Indices.Length);
            _primitiveIndexOffsets.Cones = indexBufferOffset;
            indexBufferOffset += _cone.Indices.Length;

            var newIndexBuffer = Buffer.Index.New(device, indexData);
            _indexBuffer = newIndexBuffer;
        }

        // allocate our buffers with position/colour etc data
        _transformBuffer = Buffer.Structured.New<Matrix>(device, 1);

        _colorBuffer = Buffer.Structured.New<Color>(device, 1);

        _lineVertexBuffer = Buffer.Vertex.New(device, new LineVertex[1], GraphicsResourceUsage.Dynamic);
    }

    /// <inheritdoc/>
    public override void Extract()
    {
        void ProcessRenderables(List<Renderable> renderables, ref Primitives offsets)
        {
            var span = CollectionsMarshal.AsSpan(renderables);
            for (int i = 0; i < span.Length; ++i)
            {
                ref readonly var cmd = ref span[i];
                switch (cmd.Type)
                {
                    case DebugPrimitiveType.Quad:
                        _instances[offsets.Quads] = new InstanceData
                        {
                            Position = cmd.QuadData.Position,
                            Rotation = cmd.QuadData.Rotation,
                            Scale = new Vector3(cmd.QuadData.Size.X, 1.0f, cmd.QuadData.Size.Y),
                            Color = cmd.QuadData.Color
                        };
                        offsets.Quads++;
                        break;
                    case DebugPrimitiveType.Circle:
                        _instances[offsets.Circles] = new InstanceData
                        {
                            Position = cmd.CircleData.Position,
                            Rotation = cmd.CircleData.Rotation,
                            Scale = new Vector3(cmd.CircleData.Radius * 2.0f, 0.0f, cmd.CircleData.Radius * 2.0f),
                            Color = cmd.CircleData.Color
                        };
                        offsets.Circles++;
                        break;
                    case DebugPrimitiveType.Sphere:
                        _instances[offsets.Spheres] = new InstanceData
                        {
                            Position = cmd.SphereData.Position,
                            Rotation = Quaternion.Identity,
                            Scale = new Vector3(cmd.SphereData.Radius * 2),
                            Color = cmd.SphereData.Color
                        };
                        offsets.Spheres++;
                        break;
                    case DebugPrimitiveType.HalfSphere:
                        _instances[offsets.HalfSpheres] = new InstanceData
                        {
                            Position = cmd.HalfSphereData.Position,
                            Rotation = cmd.HalfSphereData.Rotation,
                            Scale = new Vector3(cmd.HalfSphereData.Radius * 2),
                            Color = cmd.HalfSphereData.Color
                        };
                        offsets.HalfSpheres++;
                        break;
                    case DebugPrimitiveType.Cube:
                        {
                            ref readonly var start = ref cmd.CubeData.Start;
                            ref readonly var end = ref cmd.CubeData.End;
                            _instances[offsets.Cubes] = new InstanceData
                            {
                                Position = start,
                                Rotation = cmd.CubeData.Rotation,
                                Scale = end - start,
                                Color = cmd.CubeData.Color
                            };
                            offsets.Cubes++;
                            break;
                        }
                    case DebugPrimitiveType.Capsule:
                        _instances[offsets.Capsules] = new InstanceData
                        {
                            Position = cmd.CapsuleData.Position,
                            Rotation = cmd.CapsuleData.Rotation,
                            Scale = new Vector3(cmd.CapsuleData.Radius * 2.0f, cmd.CapsuleData.Height, cmd.CapsuleData.Radius * 2.0f),
                            Color = cmd.CapsuleData.Color
                        };
                        offsets.Capsules++;
                        break;
                    case DebugPrimitiveType.Cylinder:
                        _instances[offsets.Cylinders] = new InstanceData
                        {
                            Position = cmd.CylinderData.Position,
                            Rotation = cmd.CylinderData.Rotation,
                            Scale = new Vector3(cmd.CylinderData.Radius * 2.0f, cmd.CylinderData.Height, cmd.CylinderData.Radius * 2.0f),
                            Color = cmd.CylinderData.Color
                        };
                        offsets.Cylinders++;
                        break;
                    case DebugPrimitiveType.Cone:
                        _instances[offsets.Cones] = new InstanceData
                        {
                            Position = cmd.ConeData.Position,
                            Rotation = cmd.ConeData.Rotation,
                            Scale = new Vector3(cmd.ConeData.Radius * 2.0f, cmd.ConeData.Height, cmd.ConeData.Radius * 2.0f),
                            Color = cmd.ConeData.Color
                        };
                        offsets.Cones++;
                        break;
                    case DebugPrimitiveType.Line:
                        _lineVertices[offsets.Lines++] = new LineVertex { Position = cmd.LineData.Start, Color = cmd.LineData.Color };
                        _lineVertices[offsets.Lines++] = new LineVertex { Position = cmd.LineData.End, Color = cmd.LineData.Color };
                        break;
                }
            }
        }

        int SumBasicPrimitives(ref Primitives primitives)
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

        Primitives SetupPrimitiveOffsets(ref Primitives counts, int offset = 0)
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

        int lastOffset = 0;
        int lastLineOffset = 0;

        foreach (RenderObject renderObject in RenderObjects)
        {
            ImmediateDebugRenderObject debugObject = (ImmediateDebugRenderObject)renderObject;

            int primitivesWithDepth = SumBasicPrimitives(ref debugObject.totalPrimitives);
            int primitivesWithoutDepth = SumBasicPrimitives(ref debugObject.totalPrimitivesNoDepth);
            int totalThingsToDraw = primitivesWithDepth + primitivesWithoutDepth;

            _instances.EnsureSize(_instances.Count + totalThingsToDraw);
            _lineVertices.EnsureSize(_lineVertices.Count + debugObject.totalPrimitives.Lines * 2 + debugObject.totalPrimitivesNoDepth.Lines * 2);

            var primitiveOffsets = SetupPrimitiveOffsets(ref debugObject.totalPrimitives, lastOffset);
            var primitiveOffsetsNoDepth = SetupPrimitiveOffsets(ref debugObject.totalPrimitivesNoDepth, primitiveOffsets.Cones + debugObject.totalPrimitives.Cones);

            primitiveOffsets.Lines = 0 + lastLineOffset;
            primitiveOffsetsNoDepth.Lines = debugObject.totalPrimitives.Lines * 2 + lastLineOffset;

            debugObject.instanceOffsets = primitiveOffsets;
            debugObject.instanceOffsetsNoDepth = primitiveOffsetsNoDepth;

            ProcessRenderables(debugObject.renderablesWithDepth, ref primitiveOffsets);
            ProcessRenderables(debugObject.renderablesNoDepth, ref primitiveOffsetsNoDepth);

            debugObject.primitivesToDraw = debugObject.totalPrimitives;
            debugObject.primitivesToDrawNoDepth = debugObject.totalPrimitivesNoDepth;

            lastOffset = debugObject.instanceOffsetsNoDepth.Cones + debugObject.totalPrimitivesNoDepth.Cones;
            lastLineOffset = debugObject.instanceOffsetsNoDepth.Lines + debugObject.totalPrimitivesNoDepth.Lines * 2;

            // Clear per-frame message queues
            debugObject.renderablesWithDepth.Clear();
            debugObject.renderablesNoDepth.Clear();
            debugObject.totalPrimitives.Clear();
            debugObject.totalPrimitivesNoDepth.Clear();
        }
    }

    private static void UpdateBufferIfNecessary<T>(GraphicsDevice device, CommandList commandList, ref Buffer buffer, ReadOnlySpan<T> data) where T : unmanaged
    {
        if (data.IsEmpty)
            return; // nothing to upload, keep previous content

        int neededBufferSize = data.Length;
        if (neededBufferSize > buffer.ElementCount)
        {
            buffer.Dispose();
            // Recreate buffer preserving flags and usage
            var newBuffer = Buffer.New(device, data, buffer.Flags, PixelFormat.None, buffer.Usage);
            buffer = newBuffer;
        }
        else
        {
            buffer.SetData(commandList, data);
        }
    }

    private void CheckBuffers(RenderDrawContext context)
    {
        var transformsSpan = CollectionsMarshal.AsSpan(_transforms);
        UpdateBufferIfNecessary<Matrix>(context.GraphicsDevice, context.CommandList, ref _transformBuffer!, transformsSpan);

        var colorsSpan = CollectionsMarshal.AsSpan(_colors);
        UpdateBufferIfNecessary<Color>(context.GraphicsDevice, context.CommandList, ref _colorBuffer!, colorsSpan);

        var lineVertsSpan = CollectionsMarshal.AsSpan(_lineVertices);
        UpdateBufferIfNecessary<LineVertex>(context.GraphicsDevice, context.CommandList, ref _lineVertexBuffer!, lineVertsSpan);
    }

    /// <inheritdoc/>
    public override void Prepare(RenderDrawContext context)
    {
        int count = _instances.Count;
        _transforms.SetCount(count);
        _colors.SetCount(count);

        Dispatcher.For(0, count, (i) =>
        {
            var instance = _instances[i];
            Matrix.Transformation(ref instance.Scale, ref instance.Rotation, ref instance.Position, out var m);
            _transforms[i] = m;
            _colors[i] = instance.Color;
        }
        );

        CheckBuffers(context);

        _lineVertices.Clear();
        _instances.Clear();
    }

    private void SetPrimitiveRenderingPipelineState(CommandList commandList, bool depthTest, FillMode selectedFillMode, bool isDoubleSided = false, bool hasTransparency = false)
    {
        _pipelineState!.State.SetDefaults();
        _pipelineState.State.PrimitiveType = PrimitiveType.TriangleList;
        _pipelineState.State.RootSignature = _primitiveEffect!.RootSignature;
        _pipelineState.State.EffectBytecode = _primitiveEffect!.Effect.Bytecode;
        _pipelineState.State.DepthStencilState = depthTest ? hasTransparency ? DepthStencilStates.DepthRead : DepthStencilStates.Default : DepthStencilStates.None;
        _pipelineState.State.RasterizerState.FillMode = selectedFillMode;
        _pipelineState.State.RasterizerState.CullMode = selectedFillMode == FillMode.Solid && !isDoubleSided ? CullMode.Back : CullMode.None;
        _pipelineState.State.BlendState = hasTransparency ? BlendStates.NonPremultiplied : BlendStates.Opaque;
        _pipelineState.State.Output.CaptureState(commandList);
        _pipelineState.State.InputElements = _inputElements;
        _pipelineState.Update();
    }

    private void SetLineRenderingPipelineState(CommandList commandList, bool depthTest, bool hasTransparency = false)
    {
        _pipelineState!.State.SetDefaults();
        _pipelineState.State.PrimitiveType = PrimitiveType.LineList;
        _pipelineState.State.RootSignature = _lineEffect!.RootSignature;
        _pipelineState.State.EffectBytecode = _lineEffect.Effect.Bytecode;
        _pipelineState.State.DepthStencilState = depthTest ? hasTransparency ? DepthStencilStates.DepthRead : DepthStencilStates.Default : DepthStencilStates.None;
        _pipelineState.State.RasterizerState.FillMode = FillMode.Solid;
        _pipelineState.State.RasterizerState.CullMode = CullMode.None;
        _pipelineState.State.BlendState = hasTransparency ? BlendStates.NonPremultiplied : BlendStates.Opaque;
        _pipelineState.State.Output.CaptureState(commandList);
        _pipelineState.State.InputElements = _lineInputElements;
        _pipelineState.Update();
    }

    private void DrawPrimitives(RenderDrawContext context, RenderView renderView, ref Primitives offsets, ref Primitives counts, bool depthTest, FillMode fillMode, bool hasTransparency)
    {
        var commandList = context.CommandList;

        // set buffers and our current pipeline state
        commandList.SetVertexBuffer(0, _vertexBuffer, 0, VertexPositionTexture.Layout.VertexStride);
        commandList.SetIndexBuffer(_indexBuffer, 0, is32bits: true);
        commandList.SetPipelineState(_pipelineState!.CurrentState);

        // we set line width to something absurdly high to avoid having to alter our shader substantially for now
        _primitiveEffect!.Parameters.Set(PrimitiveShaderKeys.LineWidthMultiplier, fillMode == FillMode.Solid ? 10000.0f : 1.0f);
        _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.ViewProjection, renderView.ViewProjection);
        _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.Transforms, _transformBuffer);
        _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.Colors, _colorBuffer);

        _primitiveEffect.UpdateEffect(context.GraphicsDevice);
        _primitiveEffect.Apply(context.GraphicsContext);

        // draw spheres
        if (counts.Spheres > 0)
        {
            SetPrimitiveRenderingPipelineState(commandList, depthTest, fillMode, isDoubleSided: false, hasTransparency: hasTransparency);
            commandList.SetPipelineState(_pipelineState.CurrentState);

            _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Spheres);
            _primitiveEffect.Apply(context.GraphicsContext);

            commandList.DrawIndexedInstanced(_sphere.Indices.Length, counts.Spheres, _primitiveIndexOffsets.Spheres, _primitiveVertexOffsets.Spheres);
        }

        if (counts.Quads > 0 || counts.Circles > 0 || counts.HalfSpheres > 0)
        {
            SetPrimitiveRenderingPipelineState(commandList, depthTest, fillMode, isDoubleSided: true, hasTransparency: hasTransparency);
            commandList.SetPipelineState(_pipelineState.CurrentState);

            // draw quads
            if (counts.Quads > 0)
            {
                _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Quads);
                _primitiveEffect.Apply(context.GraphicsContext);

                commandList.DrawIndexedInstanced(_plane.Indices.Length, counts.Quads, _primitiveIndexOffsets.Quads, _primitiveVertexOffsets.Quads);
            }

            // draw circles
            if (counts.Circles > 0)
            {
                _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Circles);
                _primitiveEffect.Apply(context.GraphicsContext);

                commandList.DrawIndexedInstanced(_circle.Indices.Length, counts.Circles, _primitiveIndexOffsets.Circles, _primitiveVertexOffsets.Circles);
            }

            // draw half spheres
            if (counts.HalfSpheres > 0)
            {
                _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.HalfSpheres);
                _primitiveEffect.Apply(context.GraphicsContext);

                // HACK: we sort of abuse knowledge of the mesh primitive here.. :P
                commandList.DrawIndexedInstanced(_sphere.Indices.Length / 2, counts.HalfSpheres, _primitiveIndexOffsets.HalfSpheres, _primitiveVertexOffsets.HalfSpheres);
            }
        }

        if (counts.Cubes > 0 || counts.Capsules > 0 || counts.Cylinders > 0 || counts.Cones > 0)
        {
            SetPrimitiveRenderingPipelineState(commandList, depthTest, fillMode, isDoubleSided: false, hasTransparency: hasTransparency);
            commandList.SetPipelineState(_pipelineState.CurrentState);

            // draw cubes
            if (counts.Cubes > 0)
            {
                _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Cubes);
                _primitiveEffect.Apply(context.GraphicsContext);

                commandList.DrawIndexedInstanced(_cube.Indices.Length, counts.Cubes, _primitiveIndexOffsets.Cubes, _primitiveVertexOffsets.Cubes);
            }

            // draw capsules
            if (counts.Capsules > 0)
            {
                _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Capsules);
                _primitiveEffect.Apply(context.GraphicsContext);

                commandList.DrawIndexedInstanced(_capsule.Indices.Length, counts.Capsules, _primitiveIndexOffsets.Capsules, _primitiveVertexOffsets.Capsules);
            }

            // draw cylinders
            if (counts.Cylinders > 0)
            {
                _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Cylinders);
                _primitiveEffect.Apply(context.GraphicsContext);

                commandList.DrawIndexedInstanced(_cylinder.Indices.Length, counts.Cylinders, _primitiveIndexOffsets.Cylinders, _primitiveVertexOffsets.Cylinders);
            }

            // draw cones
            if (counts.Cones > 0)
            {
                _primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Cones);
                _primitiveEffect.Apply(context.GraphicsContext);

                commandList.DrawIndexedInstanced(_cone.Indices.Length, counts.Cones, _primitiveIndexOffsets.Cones, _primitiveVertexOffsets.Cones);
            }
        }

        // draw lines
        if (counts.Lines > 0)
        {
            SetLineRenderingPipelineState(commandList, depthTest, hasTransparency);
            commandList.SetVertexBuffer(0, _lineVertexBuffer, 0, LineVertex.Layout.VertexStride);
            commandList.SetPipelineState(_pipelineState.CurrentState);

            _lineEffect!.Parameters.Set(LinePrimitiveShaderKeys.ViewProjection, renderView.ViewProjection);
            _lineEffect.UpdateEffect(context.GraphicsDevice);
            _lineEffect.Apply(context.GraphicsContext);

            commandList.Draw(counts.Lines * 2, offsets.Lines);
        }
    }

    /// <inheritdoc/>
    public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
    {
        var commandList = context.CommandList;

        for (int index = startIndex; index < endIndex; index++)
        {
            var renderNodeReference = renderViewStage.SortedRenderNodes[index].RenderNode;
            var debugObject = (ImmediateDebugRenderObject)GetRenderNode(renderNodeReference).RenderObject;
            bool objectHasTransparency = debugObject.Stage == DebugRenderStage.Transparent;

            // update pipeline state, render with depth test first
            SetPrimitiveRenderingPipelineState(commandList, depthTest: true, selectedFillMode: debugObject.CurrentFillMode, hasTransparency: objectHasTransparency);
            DrawPrimitives(context, renderView, ref debugObject.instanceOffsets, ref debugObject.primitivesToDraw, depthTest: true, fillMode: debugObject.CurrentFillMode, hasTransparency: objectHasTransparency);

            // render without depth test second
            SetPrimitiveRenderingPipelineState(commandList, depthTest: false, selectedFillMode: debugObject.CurrentFillMode, hasTransparency: objectHasTransparency);
            DrawPrimitives(context, renderView, offsets: ref debugObject.instanceOffsetsNoDepth, counts: ref debugObject.primitivesToDrawNoDepth, depthTest: false, fillMode: debugObject.CurrentFillMode, hasTransparency: objectHasTransparency);
        }
    }

    /// <inheritdoc/>
    public override void Flush(RenderDrawContext context)
    {
    }

    /* FIXME: is there a nicer way to handle dispose, some xenko idiom? */

    /// <inheritdoc/>
    public override void Unload()
    {
        base.Unload();
        _transformBuffer?.Dispose();
        _colorBuffer?.Dispose();
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _lineVertexBuffer?.Dispose();
    }

}
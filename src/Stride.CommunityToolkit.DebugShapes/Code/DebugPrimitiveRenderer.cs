using Stride.DebugRendering;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.CommunityToolkit.DebugShapes.Code;

public class DebugPrimitiveRenderer
{
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

    private Buffer? _vertexBuffer;
    private Buffer? _indexBuffer;
    private Buffer? _transformBuffer;
    private Buffer? _colorBuffer;
    private Buffer? _lineVertexBuffer;
    private MutablePipelineState? _pipelineState;
    private DynamicEffectInstance? _primitiveEffect;
    private DynamicEffectInstance? _lineEffect;
    private Primitives _primitiveVertexOffsets;
    private Primitives _primitiveIndexOffsets;
    private InputElementDescription[] _inputElements = Array.Empty<InputElementDescription>();
    private InputElementDescription[] _lineInputElements = Array.Empty<InputElementDescription>();

    /* mesh data we will use when stuffing things in vertex buffers */
    private readonly (VertexPositionTexture[] Vertices, int[] Indices) _circle = ImmediateDebugPrimitives.GenerateCircle(DefaultCircleRadius, CircleTesselation);
    private readonly (VertexPositionTexture[] Vertices, int[] Indices) _plane = ImmediateDebugPrimitives.GenerateQuad(DefaultPlaneSize, DefaultPlaneSize);
    private readonly (VertexPositionTexture[] Vertices, int[] Indices) _sphere = ImmediateDebugPrimitives.GenerateSphere(DefaultSphereRadius, SphereTesselation, uvSplitOffsetVertical: 1);
    private readonly (VertexPositionTexture[] Vertices, int[] Indices) _cube = ImmediateDebugPrimitives.GenerateCube(DefaultCubeSize);
    private readonly (VertexPositionTexture[] Vertices, int[] Indices) _capsule = ImmediateDebugPrimitives.GenerateCapsule(DefaultCapsuleLength, DefaultCapsuleRadius, CapsuleTesselation);
    private readonly (VertexPositionTexture[] Vertices, int[] Indices) _cylinder = ImmediateDebugPrimitives.GenerateCylinder(DefaultCylinderHeight, DefaultCylinderRadius, CylinderTesselation);
    private readonly (VertexPositionTexture[] Vertices, int[] Indices) _cone = ImmediateDebugPrimitives.GenerateCone(DefaultConeHeight, DefaultConeRadius, ConeTesselation, uvSplits: 8);

    public DebugPrimitiveRenderer(Buffer? vertexBuffer, Buffer? indexBuffer, MutablePipelineState? pipelineState, DynamicEffectInstance? primitiveEffect, DynamicEffectInstance? lineEffect, Buffer? transformBuffer, Buffer? colorBuffer, Buffer? lineVertexBuffer, InputElementDescription[] inputElements, InputElementDescription[] lineInputElements, Primitives primitiveVertexOffsets, Primitives primitiveIndexOffsets)
    {
        _vertexBuffer = vertexBuffer;
        _indexBuffer = indexBuffer;
        _pipelineState = pipelineState;
        _primitiveEffect = primitiveEffect;
        _lineEffect = lineEffect;
        _transformBuffer = transformBuffer;
        _colorBuffer = colorBuffer;
        _lineVertexBuffer = lineVertexBuffer;
        _inputElements = inputElements;
        _lineInputElements = lineInputElements;
        _primitiveVertexOffsets = primitiveVertexOffsets;
        _primitiveIndexOffsets = primitiveIndexOffsets;
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
}

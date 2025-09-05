using Stride.Graphics;
using Stride.Rendering;

namespace Stride.CommunityToolkit.DebugShapes.Code;

/// <summary>
/// Configures pipeline state and effects for debug primitive rendering (solid and line passes).
/// </summary>
internal sealed class PrimitivePipeline
{
    private readonly MutablePipelineState _pipelineState;
    private readonly DynamicEffectInstance _primitiveEffect;
    private readonly DynamicEffectInstance _lineEffect;
    private readonly InputElementDescription[] _inputElements;
    private readonly InputElementDescription[] _lineInputElements;

    public PrimitivePipeline(MutablePipelineState pipelineState,
                             DynamicEffectInstance primitiveEffect,
                             DynamicEffectInstance lineEffect,
                             InputElementDescription[] inputElements,
                             InputElementDescription[] lineInputElements)
    {
        _pipelineState = pipelineState;
        _primitiveEffect = primitiveEffect;
        _lineEffect = lineEffect;
        _inputElements = inputElements;
        _lineInputElements = lineInputElements;
    }

    /// <summary>
    /// Configures the pipeline for triangle primitives.
    /// </summary>
    public void ConfigurePrimitivePipeline(CommandList commandList, bool depthTest, FillMode selectedFillMode, bool isDoubleSided, bool hasTransparency)
    {
        _pipelineState.State.SetDefaults();
        _pipelineState.State.PrimitiveType = PrimitiveType.TriangleList;
        _pipelineState.State.RootSignature = _primitiveEffect.RootSignature;
        _pipelineState.State.EffectBytecode = _primitiveEffect.Effect.Bytecode;
        _pipelineState.State.DepthStencilState = depthTest ? (hasTransparency ? DepthStencilStates.DepthRead : DepthStencilStates.Default) : DepthStencilStates.None;
        _pipelineState.State.RasterizerState.FillMode = selectedFillMode;
        _pipelineState.State.RasterizerState.CullMode = selectedFillMode == FillMode.Solid && !isDoubleSided ? CullMode.Back : CullMode.None;
        _pipelineState.State.BlendState = hasTransparency ? BlendStates.NonPremultiplied : BlendStates.Opaque;
        _pipelineState.State.Output.CaptureState(commandList);
        _pipelineState.State.InputElements = _inputElements;
        _pipelineState.Update();
    }

    /// <summary>
    /// Configures the pipeline for line primitives.
    /// </summary>
    public void ConfigureLinePipeline(CommandList commandList, bool depthTest, bool hasTransparency)
    {
        _pipelineState.State.SetDefaults();
        _pipelineState.State.PrimitiveType = PrimitiveType.LineList;
        _pipelineState.State.RootSignature = _lineEffect.RootSignature;
        _pipelineState.State.EffectBytecode = _lineEffect.Effect.Bytecode;
        _pipelineState.State.DepthStencilState = depthTest ? (hasTransparency ? DepthStencilStates.DepthRead : DepthStencilStates.Default) : DepthStencilStates.None;
        _pipelineState.State.RasterizerState.FillMode = FillMode.Solid;
        _pipelineState.State.RasterizerState.CullMode = CullMode.None;
        _pipelineState.State.BlendState = hasTransparency ? BlendStates.NonPremultiplied : BlendStates.Opaque;
        _pipelineState.State.Output.CaptureState(commandList);
        _pipelineState.State.InputElements = _lineInputElements;
        _pipelineState.Update();
    }
}
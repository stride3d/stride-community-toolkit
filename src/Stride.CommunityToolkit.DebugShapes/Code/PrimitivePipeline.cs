using Stride.Graphics;
using Stride.Rendering;

namespace Stride.CommunityToolkit.DebugShapes;

/// <summary>
/// Configures pipeline state and effects for debug primitive rendering (solid and line passes).
/// </summary>
/// <param name="pipelineState">The one mutable pipeline state every pass is configured into.</param>
/// <param name="primitiveEffect">The effect the solid and wireframe shapes draw with.</param>
/// <param name="lineEffect">The effect the lines draw with.</param>
/// <param name="inputElements">The vertex layout of the shapes.</param>
/// <param name="lineInputElements">The vertex layout of the lines.</param>
internal sealed class PrimitivePipeline(MutablePipelineState pipelineState, DynamicEffectInstance primitiveEffect, DynamicEffectInstance lineEffect, InputElementDescription[] inputElements, InputElementDescription[] lineInputElements)
{
    /// <summary>
    /// Configures the pipeline for triangle primitives.
    /// </summary>
    internal void ConfigurePrimitivePipeline(CommandList commandList, bool depthTest, FillMode selectedFillMode, bool isDoubleSided, bool hasTransparency)
        => Configure(commandList, primitiveEffect, PrimitiveType.TriangleList, selectedFillMode, selectedFillMode == FillMode.Solid && !isDoubleSided ? CullMode.Back : CullMode.None, depthTest, hasTransparency, inputElements);

    /// <summary>
    /// Configures the pipeline for line primitives.
    /// </summary>
    internal void ConfigureLinePipeline(CommandList commandList, bool depthTest, bool hasTransparency)
        => Configure(commandList, lineEffect, PrimitiveType.LineList, FillMode.Solid, CullMode.None, depthTest, hasTransparency, lineInputElements);

    // Transparent shapes read the depth buffer without writing it, so they blend over each other in order
    private void Configure(CommandList commandList, DynamicEffectInstance effect, PrimitiveType primitiveType, FillMode fillMode, CullMode cullMode, bool depthTest, bool hasTransparency, InputElementDescription[] layout)
    {
        var state = pipelineState.State;

        state.SetDefaults();
        state.PrimitiveType = primitiveType;
        state.RootSignature = effect.RootSignature;
        state.EffectBytecode = effect.Effect.Bytecode;
        state.DepthStencilState = depthTest ? (hasTransparency ? DepthStencilStates.DepthRead : DepthStencilStates.Default) : DepthStencilStates.None;
        state.RasterizerState.FillMode = fillMode;
        state.RasterizerState.CullMode = cullMode;
        state.BlendState = hasTransparency ? BlendStates.NonPremultiplied : BlendStates.Opaque;
        state.Output.CaptureState(commandList);
        state.InputElements = layout;

        pipelineState.Update();
    }
}
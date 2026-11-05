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
    /// <summary>What a pass always draws with: its effect, its primitive type and its vertex layout.</summary>
    private readonly record struct Pass(DynamicEffectInstance Effect, PrimitiveType Type, InputElementDescription[] Layout);

    private readonly Pass _shapes = new(primitiveEffect, PrimitiveType.TriangleList, inputElements);
    private readonly Pass _lines = new(lineEffect, PrimitiveType.LineList, lineInputElements);

    /// <summary>
    /// Configures the pipeline for triangle primitives.
    /// </summary>
    internal void ConfigurePrimitivePipeline(CommandList commandList, bool depthTest, FillMode selectedFillMode, bool isDoubleSided, bool hasTransparency)
        => Configure(commandList, _shapes, selectedFillMode, selectedFillMode == FillMode.Solid && !isDoubleSided ? CullMode.Back : CullMode.None, depthTest, hasTransparency);

    /// <summary>
    /// Configures the pipeline for line primitives.
    /// </summary>
    internal void ConfigureLinePipeline(CommandList commandList, bool depthTest, bool hasTransparency)
        => Configure(commandList, _lines, FillMode.Solid, CullMode.None, depthTest, hasTransparency);

    // Transparent shapes read the depth buffer without writing it, so they blend over each other in order
    private void Configure(CommandList commandList, in Pass pass, FillMode fillMode, CullMode cullMode, bool depthTest, bool hasTransparency)
    {
        var state = pipelineState.State;

        state.SetDefaults();
        state.PrimitiveType = pass.Type;
        state.RootSignature = pass.Effect.RootSignature;
        state.EffectBytecode = pass.Effect.Effect.Bytecode;
        state.DepthStencilState = depthTest ? (hasTransparency ? DepthStencilStates.DepthRead : DepthStencilStates.Default) : DepthStencilStates.None;
        state.RasterizerState.FillMode = fillMode;
        state.RasterizerState.CullMode = cullMode;
        state.BlendState = hasTransparency ? BlendStates.NonPremultiplied : BlendStates.Opaque;
        state.Output.CaptureState(commandList);
        state.InputElements = pass.Layout;

        pipelineState.Update();
    }
}

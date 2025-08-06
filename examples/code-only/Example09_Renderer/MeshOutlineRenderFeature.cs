using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;

namespace Example09_Renderer;

/// <summary>
/// Render feature that applies an outline effect to meshes using the MeshOutlineShader.
/// </summary>
/// <remarks>
/// Only entities with <see cref="MeshOutlineComponent"/> and matching <see cref="RenderGroupMask"/> will be outlined.
/// Ensure this feature is enabled in the Graphics Compositor.
/// </remarks>
public class MeshOutlineRenderFeature : RootRenderFeature
{
    private DynamicEffectInstance? shader;
    private MutablePipelineState? pipelineState;

    /// <summary>
    /// Default sort key for outline rendering.
    /// </summary>
    public const int DefaultSortKey = 255;

    /// <summary>
    /// Adjusts the scale of the model for outline thickness.
    /// </summary>
    [DataMember(10)]
    [DataMemberRange(0.0f, 0.1f, 0.001f, 0.002f, 4)]
    public float ScaleAdjust = 0.001f;

    /// <summary>
    /// Specifies which render groups will have outlines applied.
    /// </summary>
    [DataMember(5)]
    public RenderGroupMask RenderGroupMask;

    /// <inheritdoc/>
    public override Type SupportedRenderObjectType => typeof(RenderMesh);

    /// <summary>
    /// Initializes a new instance of the <see cref="MeshOutlineRenderFeature"/> class.
    /// </summary>
    public MeshOutlineRenderFeature() => SortKey = DefaultSortKey;

    /// <summary>
    /// Initializes the shader and pipeline state for outline rendering.
    /// </summary>
    protected override void InitializeCore()
    {
        base.InitializeCore();

        try
        {
            shader = new DynamicEffectInstance("MeshOutlineShader");
            shader.Initialize(Context.Services);

            pipelineState = new MutablePipelineState(Context.GraphicsDevice);
            pipelineState.State.SetDefaults();
            pipelineState.State.InputElements = VertexPositionNormalTexture.Layout.CreateInputElements();
            pipelineState.State.BlendState = BlendStates.AlphaBlend;
            pipelineState.State.RasterizerState.CullMode = CullMode.Front;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to initialize MeshOutlineRenderFeature.", ex);
        }
    }

    /// <inheritdoc/>
    public override void Prepare(RenderDrawContext context)
        => base.Prepare(context);

    /// <inheritdoc/>
    public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage)
    {
        if (shader == null || pipelineState == null) return;

        shader.UpdateEffect(context.GraphicsDevice);

        // Cache frequently used values outside the loop
        var viewProjection = renderView.ViewProjection;
        var viewport = new Vector4(context.RenderContext.RenderView.ViewSize, 0, 0);
        var worldScale = new Vector3(ScaleAdjust + 1.0f);

        foreach (var renderNode in renderViewStage.SortedRenderNodes)
        {
            if (renderNode.RenderObject is not RenderMesh renderMesh)
            {
                continue;
            }

            if (!RenderGroupMask.Contains(renderMesh.RenderGroup))
            {
                continue;
            }

            MeshOutlineComponent? outlineScript = null;

            if (renderMesh.Source is ModelComponent component)
            {
                outlineScript = component.Entity.Get<MeshOutlineComponent>();
            }

            if (outlineScript == null || !outlineScript.Enabled)
            {
                continue;
            }

            MeshDraw drawData = renderMesh.ActiveMeshDraw;

            for (int slot = 0; slot < drawData.VertexBuffers.Length; slot++)
            {
                var vertexBuffer = drawData.VertexBuffers[slot];
                context.CommandList.SetVertexBuffer(slot, vertexBuffer.Buffer, vertexBuffer.Offset, vertexBuffer.Stride);
            }

            // Use cached values
            shader.Parameters.Set(TransformationKeys.WorldViewProjection, renderMesh.World * viewProjection);
            shader.Parameters.Set(TransformationKeys.WorldScale, worldScale);
            shader.Parameters.Set(MeshOutlineShaderKeys.Viewport, viewport);
            shader.Parameters.Set(MeshOutlineShaderKeys.Color, outlineScript.Color);
            shader.Parameters.Set(MeshOutlineShaderKeys.Intensity, outlineScript.Intensity);

            pipelineState.State.RootSignature = shader.RootSignature;
            pipelineState.State.EffectBytecode = shader.Effect.Bytecode;
            pipelineState.State.PrimitiveType = drawData.PrimitiveType;

            pipelineState.State.Output.CaptureState(context.CommandList);
            pipelineState.Update();

            context.CommandList.SetIndexBuffer(drawData.IndexBuffer.Buffer, drawData.IndexBuffer.Offset, drawData.IndexBuffer.Is32Bit);
            context.CommandList.SetPipelineState(pipelineState.CurrentState);

            shader.Apply(context.GraphicsContext);

            if (drawData.IndexBuffer != null)
            {
                context.CommandList.DrawIndexed(drawData.DrawCount, drawData.StartLocation);
            }
            else
            {
                context.CommandList.Draw(drawData.DrawCount, drawData.StartLocation);
            }
        }
    }
}
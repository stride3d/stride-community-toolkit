using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;

namespace Example18_Box2DPhysics;

/// <summary>
/// Render feature that applies an outline effect to meshes using the MeshOutlineShader.
/// </summary>
/// <remarks>
/// Only entities with <see cref="MeshOutlineComponent"/> and matching <see cref="RenderGroupMask"/> will be outlined.
/// Ensure this feature is enabled in the Graphics Compositor.
/// </remarks>
public class OuterOutline2DShaderRenderFeature : RootRenderFeature
{
    private DynamicEffectInstance? _shader;
    private MutablePipelineState? _pipelineState;

    /// <summary>
    /// Default sort key for outline rendering.
    /// </summary>
    public const int DefaultSortKey = 255;

    /// <inheritdoc/>
    public override Type SupportedRenderObjectType => typeof(RenderMesh);

    /// <summary>
    /// Initializes a new instance of the <see cref="SDFPerimeterOutline2DShaderRenderFeature"/> class.
    /// </summary>
    public OuterOutline2DShaderRenderFeature() => SortKey = DefaultSortKey;

    /// <summary>
    /// Initializes the shader and pipeline state for outline rendering.
    /// </summary>
    protected override void InitializeCore()
    {
        base.InitializeCore();

        try
        {
            _shader = new DynamicEffectInstance("OuterOutline2DShader");
            _shader.Initialize(Context.Services);

            _pipelineState = new MutablePipelineState(Context.GraphicsDevice);
            _pipelineState.State.SetDefaults();
            _pipelineState.State.InputElements = VertexPositionNormalTexture.Layout.CreateInputElements();
            //_pipelineState.State.BlendState = BlendStates.AlphaBlend;
            _pipelineState.State.BlendState = BlendStates.Opaque; // solid crisp border
            _pipelineState.State.RasterizerState.CullMode = CullMode.Back;
            _pipelineState.State.DepthStencilState = DepthStencilStates.DepthRead;
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
        if (_shader == null || _pipelineState == null) return;

        _shader.UpdateEffect(context.GraphicsDevice);

        var viewProjection = renderView.ViewProjection;

        foreach (var renderNode in renderViewStage.SortedRenderNodes)
        {
            if (renderNode.RenderObject is not RenderMesh renderMesh)
            {
                continue;
            }

            MeshOutlineComponent? outlineScript = null;

            if (renderMesh.Source is ModelComponent component)
            {
                outlineScript = component.Entity.Get<MeshOutlineComponent>();
            }

            if (outlineScript is null || !outlineScript.Enabled)
            {
                continue;
            }

            MeshDraw drawData = renderMesh.ActiveMeshDraw;

            for (int slot = 0; slot < drawData.VertexBuffers.Length; slot++)
            {
                var vertexBuffer = drawData.VertexBuffers[slot];
                context.CommandList.SetVertexBuffer(slot, vertexBuffer.Buffer, vertexBuffer.Offset, vertexBuffer.Stride);
            }

            _shader.Parameters.Set(TransformationKeys.WorldViewProjection, renderMesh.World * viewProjection);
            _shader.Parameters.Set(TransformationKeys.WorldScale, Vector3.One);
            _shader.Parameters.Set(OuterOutline2DShaderKeys.Color, outlineScript.Color);
            _shader.Parameters.Set(OuterOutline2DShaderKeys.Intensity, outlineScript.Intensity);
            _shader.Parameters.Set(OuterOutline2DShaderKeys.OutlineThickness, outlineScript.OutlineThickness);

            _pipelineState.State.RootSignature = _shader.RootSignature;
            _pipelineState.State.EffectBytecode = _shader.Effect.Bytecode;
            _pipelineState.State.PrimitiveType = drawData.PrimitiveType;

            _pipelineState.State.Output.CaptureState(context.CommandList);
            _pipelineState.Update();

            context.CommandList.SetIndexBuffer(drawData.IndexBuffer.Buffer, drawData.IndexBuffer.Offset, drawData.IndexBuffer.Is32Bit);
            context.CommandList.SetPipelineState(_pipelineState.CurrentState);

            _shader.Apply(context.GraphicsContext);

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
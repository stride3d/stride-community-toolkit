using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;

namespace Example13_MeshOutline;

/// <summary>
/// Draws an outline around every mesh whose entity carries a <see cref="MeshOutlineComponent"/>.
/// </summary>
/// <remarks>
/// The trick is to draw each mesh a second time, slightly inflated along its normals and with front
/// faces culled, so only the back faces remain visible as a shell peeking out from behind the
/// original mesh. Stride calls <see cref="Draw(RenderDrawContext, RenderView, RenderViewStage)"/>
/// before the regular mesh draws, so the shell ends up underneath and reads as an outline.
/// </remarks>
public class MeshOutlineRenderFeature : RootRenderFeature
{
    private DynamicEffectInstance? _shader;
    private MutablePipelineState? _pipelineState;

    /// <summary>
    /// How far the outline sticks out, in the model's own units (the shell is offset along the
    /// vertex normals before the world matrix, so the entity's scale multiplies this).
    /// A unit-diameter sphere has a radius of 0.5, so 0.03 gives a rim about 6% of the radius.
    /// </summary>
    public float Thickness = 0.001f;

    /// <summary>
    /// This feature draws meshes that belong to Stride's own <c>MeshRenderFeature</c>, so it never
    /// owns a render object of its own. Reporting a private marker type keeps Stride from handing
    /// it the scene's <see cref="RenderMesh"/> objects - a render object goes to the first feature
    /// that accepts it, so claiming <see cref="RenderMesh"/> here would stop the meshes rendering
    /// normally whenever this feature happened to be registered first.
    /// </summary>
    public override Type SupportedRenderObjectType => typeof(NeverCreated);

    /// <inheritdoc/>
    protected override void InitializeCore()
    {
        base.InitializeCore();

        _shader = new DynamicEffectInstance("MeshOutlineShader");
        _shader.Initialize(Context.Services);

        _pipelineState = new MutablePipelineState(Context.GraphicsDevice);
        _pipelineState.State.SetDefaults();
        _pipelineState.State.InputElements = VertexPositionNormalTexture.Layout.CreateInputElements();
        _pipelineState.State.BlendState = BlendStates.AlphaBlend;

        // Cull the front faces so only the inflated shell behind the mesh survives
        _pipelineState.State.RasterizerState.CullMode = CullMode.Front;
    }

    /// <inheritdoc/>
    protected override void Destroy()
    {
        _shader?.Dispose();
        _shader = null;

        base.Destroy();
    }

    /// <inheritdoc/>
    public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage)
    {
        if (_shader is null || _pipelineState is null) return;

        // Stride calls Draw once per render view and render stage, which includes the shadow map
        // passes. Those bind a depth buffer only, so drawing outlines there is wasted work and makes
        // the outline pixel shader write to a render target that is not bound - which the D3D debug
        // layer reports every frame. Outlines only make sense where colour is being written.
        if (context.CommandList.RenderTargetCount == 0) return;

        _shader.UpdateEffect(context.GraphicsDevice);

        // Everything except the primitive type is the same for every mesh, so set it up once
        _pipelineState.State.RootSignature = _shader.RootSignature;
        _pipelineState.State.EffectBytecode = _shader.Effect.Bytecode;
        _pipelineState.State.Output.CaptureState(context.CommandList);

        var viewProjection = renderView.ViewProjection;
        var outlineScale = new Vector3(1f + Thickness);

        foreach (var renderNode in renderViewStage.SortedRenderNodes)
        {
            if (renderNode.RenderObject is not RenderMesh renderMesh) continue;

            if (renderMesh.Source is not ModelComponent model) continue;

            var outline = model.Entity.Get<MeshOutlineComponent>();
            if (outline is null || !outline.Enabled) continue;

            DrawOutline(context, renderMesh, outline, viewProjection, outlineScale);
        }
    }

    private void DrawOutline(RenderDrawContext context, RenderMesh renderMesh, MeshOutlineComponent outline,
        Matrix viewProjection, Vector3 outlineScale)
    {
        var drawData = renderMesh.ActiveMeshDraw;
        var commandList = context.CommandList;

        for (var slot = 0; slot < drawData.VertexBuffers.Length; slot++)
        {
            var vertexBuffer = drawData.VertexBuffers[slot];
            commandList.SetVertexBuffer(slot, vertexBuffer.Buffer, vertexBuffer.Offset, vertexBuffer.Stride);
        }

        _shader!.Parameters.Set(TransformationKeys.WorldViewProjection, renderMesh.World * viewProjection);
        _shader.Parameters.Set(TransformationKeys.WorldScale, outlineScale);
        _shader.Parameters.Set(MeshOutlineShaderKeys.Color, outline.Color);
        _shader.Parameters.Set(MeshOutlineShaderKeys.Intensity, outline.Intensity);

        _pipelineState!.State.PrimitiveType = drawData.PrimitiveType;
        _pipelineState.Update();
        commandList.SetPipelineState(_pipelineState.CurrentState);

        _shader.Apply(context.GraphicsContext);

        if (drawData.IndexBuffer is null)
        {
            commandList.Draw(drawData.DrawCount, drawData.StartLocation);
        }
        else
        {
            commandList.SetIndexBuffer(drawData.IndexBuffer.Buffer, drawData.IndexBuffer.Offset, drawData.IndexBuffer.Is32Bit);
            commandList.DrawIndexed(drawData.DrawCount, drawData.StartLocation);
        }
    }

    /// <summary>
    /// Placeholder render object type - see <see cref="SupportedRenderObjectType"/>. Nothing ever
    /// creates one.
    /// </summary>
    private sealed class NeverCreated : RenderObject;
}
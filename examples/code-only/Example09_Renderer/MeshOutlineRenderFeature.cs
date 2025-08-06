using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;

namespace Example09_Renderer;

public class MeshOutlineRenderFeature : RootRenderFeature
{
    DynamicEffectInstance shader;
    MutablePipelineState pipelineState;

    /// <summary>
    /// Adjust scale a bit of model for outline thickness
    /// </summary>
    [DataMember(10)]
    [DataMemberRange(0.0f, 0.1f, 0.001f, 0.002f, 4)]
    public float ScaleAdjust = 0.001f;

    /// <summary>
    /// Apply outlines to specified render groups
    /// </summary>
    [DataMember(5)] public RenderGroupMask RenderGroupMask;

    public override Type SupportedRenderObjectType => typeof(RenderMesh);

    public MeshOutlineRenderFeature()
    {
        SortKey = 255;
    }

    protected override void InitializeCore()
    {
        base.InitializeCore();

        shader = new DynamicEffectInstance("MeshOutlineShader");
        shader.Initialize(Context.Services);

        pipelineState = new MutablePipelineState(Context.GraphicsDevice);
        pipelineState.State.SetDefaults();
        pipelineState.State.InputElements = VertexPositionNormalTexture.Layout.CreateInputElements();
        pipelineState.State.BlendState = BlendStates.AlphaBlend;
        pipelineState.State.RasterizerState.CullMode = CullMode.Front;
    }

    public override void Prepare(RenderDrawContext context)
    {
        base.Prepare(context);
    }

    public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage)
    {
        shader.UpdateEffect(context.GraphicsDevice);

        foreach (var renderNode in renderViewStage.SortedRenderNodes)
        {
            var renderMesh = renderNode.RenderObject as RenderMesh;

            if (renderMesh is null) continue;

            if (!RenderGroupMask.Contains(renderMesh.RenderGroup))
                continue;

            MeshOutlineComponent outlineScript = null;
            if (renderMesh.Source is ModelComponent component)
                outlineScript = component.Entity.Get<MeshOutlineComponent>();

            if (outlineScript == null || !outlineScript.Enabled)
                continue;

            MeshDraw drawData = renderMesh.ActiveMeshDraw;

            for (int slot = 0; slot < drawData.VertexBuffers.Length; slot++)
            {
                var vertexBuffer = drawData.VertexBuffers[slot];
                context.CommandList.SetVertexBuffer(slot, vertexBuffer.Buffer, vertexBuffer.Offset, vertexBuffer.Stride);
            }

            shader.Parameters.Set(TransformationKeys.WorldViewProjection, renderMesh.World * renderView.ViewProjection);
            shader.Parameters.Set(TransformationKeys.WorldScale, new Vector3(ScaleAdjust + 1.0f));
            shader.Parameters.Set(MeshOutlineShaderKeys.Viewport, new Vector4(context.RenderContext.RenderView.ViewSize, 0, 0));
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

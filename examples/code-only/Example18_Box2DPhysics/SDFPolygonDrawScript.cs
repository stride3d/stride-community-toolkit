using Example18_Box2DPhysics.Helpers;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Compositing;

public class SDFPolygonRendererStartup : StartupScript
{
    private PolygonSDFRenderer renderer;

    public override void Start()
    {
        var effectSystem = Game.Services.GetService<EffectSystem>();
        renderer = new PolygonSDFRenderer(Game.GraphicsDevice, effectSystem);

        var sceneRenderer = new DelegateSceneRenderer(drawContext =>
        {
            var points = new[] {
                new Vector2(-0.5f, -0.5f),
                new Vector2(0.5f, -0.5f),
                new Vector2(0.0f, 0.5f)
            };
            int count = 3;
            float radius = 0.0f;
            float thickness = 0.05f;
            var color = new Color4(1, 0.8f, 0.2f, 1);

            var graphicsContext = drawContext.GraphicsContext;
            var commandList = graphicsContext.CommandList;

            renderer.DrawPolygon(commandList, graphicsContext, points, count, radius, thickness, color);
        });

        SceneSystem.GraphicsCompositor.AddSceneRenderer(sceneRenderer);
    }

    public override void Cancel()
    {
        renderer?.Dispose();
    }
}
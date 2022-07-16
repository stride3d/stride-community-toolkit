using Stride.Core;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;

namespace Stride.GameDefaults.Skyboxes;

// Taken from Stride.Assets.Skyboxes
public class SkyboxGeneratorContext : ShaderGeneratorContext
{
    public IServiceRegistry Services { get; private set; }

    public GraphicsDevice GraphicsDevice { get; private set; }

    public RenderContext RenderContext { get; private set; }

    public RenderDrawContext RenderDrawContext { get; private set; }

    public SkyboxGeneratorContext(Game game)
    {
        Services = game.Services;
        GraphicsDevice = game.GraphicsDevice;
        RenderContext = RenderContext.GetShared(Services);
        RenderDrawContext = new RenderDrawContext(Services, RenderContext, game.GraphicsContext);
    }
}
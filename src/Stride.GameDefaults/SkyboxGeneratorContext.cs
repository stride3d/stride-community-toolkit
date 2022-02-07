namespace Stride.GameDefaults;

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


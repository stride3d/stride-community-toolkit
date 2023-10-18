using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;

namespace Stride.CommunityToolkit.Skyboxes;

/// <summary>
/// Provides context for generating a skybox by encapsulating relevant services and rendering contexts.
/// </summary>
/// <remarks>
/// This class is a simplified version tailored for code-only usage, inspired by the more complex `SkyboxGeneratorContext` class in the `Stride.Assets.Skyboxes` namespace.
/// </remarks>
public class SkyboxGeneratorContext : ShaderGeneratorContext
{
    /// <summary>Gets the service registry.</summary>
    public IServiceRegistry Services { get; }

    /// <summary>Gets the graphics device.</summary>
    public GraphicsDevice GraphicsDevice { get; }

    /// <summary>Gets the render context.</summary>
    public RenderContext RenderContext { get; }

    /// <summary>Gets the render draw context.</summary>
    public RenderDrawContext RenderDrawContext { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SkyboxGeneratorContext"/> class using the provided game instance.
    /// </summary>
    /// <param name="game">The game instance used to access necessary services and contexts.</param>
    public SkyboxGeneratorContext(Game game)
    {
        Services = game.Services;
        GraphicsDevice = game.GraphicsDevice;
        RenderContext = RenderContext.GetShared(Services);
        RenderDrawContext = new RenderDrawContext(Services, RenderContext, game.GraphicsContext);
    }
}
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Rendering.Overlay;

/// <summary>
/// Adds the <see cref="EntityInfoRenderer"/> to a <see cref="GraphicsCompositor"/> or a game.
/// </summary>
public static class EntityInfoRendererExtensions
{
    /// <summary>
    /// Adds an <see cref="EntityInfoRenderer"/> to the <see cref="GraphicsCompositor"/> for rendering entity debug information.
    /// </summary>
    /// <param name="graphicsCompositor">The <see cref="GraphicsCompositor"/> to which the entity debug renderer will be added.</param>
    /// <param name="options">Optional settings to customize the appearance of the debug renderer. If not provided, default options will be used.</param>
    /// <returns>The modified <see cref="GraphicsCompositor"/> instance with the entity debug renderer added.</returns>
    /// <remarks>
    /// This method adds a custom <see cref="EntityInfoRenderer"/> to the graphics compositor, allowing the display of debug information
    /// such as entity names and positions in a 3D scene. The renderer can be customized using the <paramref name="options"/> parameter,
    /// which allows the user to define font size, color, and other settings.
    /// </remarks>
    /// <example>
    /// The following example demonstrates how to add an entity debug renderer with default settings:
    /// <code>
    /// graphicsCompositor.AddEntityInfoRenderer();
    /// </code>
    /// You can also specify custom options:
    /// <code>
    /// var options = new EntityInfoRendererOptions { FontSize = 16, FontColor = Color.Red };
    /// graphicsCompositor.AddEntityInfoRenderer(options);
    /// </code>
    /// </example>
    public static GraphicsCompositor AddEntityInfoRenderer(this GraphicsCompositor graphicsCompositor, EntityInfoRendererOptions? options = null)
    {
        graphicsCompositor.AddSceneRenderer(new EntityInfoRenderer(options));

        return graphicsCompositor;
    }

    /// <summary>
    /// Adds an <see cref="EntityInfoRenderer"/> to the game's <see cref="GraphicsCompositor"/> for rendering entity debug information.
    /// </summary>
    /// <param name="game">The <see cref="Game"/> instance to which the entity debug renderer will be added.</param>
    /// <param name="options">Optional settings to customize the appearance of the debug renderer. If not provided, default options will be used.</param>
    /// <returns>The renderer, already in the compositor.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="GraphicsCompositor"/> is not set in the game's <see cref="SceneSystem"/>.</exception>
    /// <remarks>
    /// This method adds a custom <see cref="EntityInfoRenderer"/> to the game's graphics compositor, allowing the display of debug information
    /// such as entity names and positions in a 3D scene. The renderer can be customized using the <paramref name="options"/> parameter,
    /// which allows the user to define font size, color, and other settings.
    /// <br/><br/>
    /// IMPORTANT: This may significantly reduce frame rate. Use only for development or debugging, or in scenes with very few entities.
    /// </remarks>
    /// <example>
    /// The following example demonstrates how to add an entity debug renderer with default settings:
    /// <code>
    /// game.AddEntityInfoRenderer();
    /// </code>
    /// You can also specify custom options:
    /// <code>
    /// var options = new EntityInfoRendererOptions { FontSize = 16, FontColor = Color.Red };
    /// game.AddEntityInfoRenderer(options);
    /// </code>
    /// </example>
    public static EntityInfoRenderer AddEntityInfoRenderer(this IGame game, EntityInfoRendererOptions? options = null)
    {
        // The service registry rather than the Engine helper: this namespace is used by Engine, and must not use it back
        var graphicsCompositor = game.Services.GetSafeServiceAs<SceneSystem>().GraphicsCompositor
            ?? throw new InvalidOperationException("The game has no graphics compositor; call AddGraphicsCompositor first.");

        var renderer = new EntityInfoRenderer(options);

        graphicsCompositor.AddSceneRenderer(renderer);

        return renderer;
    }
}
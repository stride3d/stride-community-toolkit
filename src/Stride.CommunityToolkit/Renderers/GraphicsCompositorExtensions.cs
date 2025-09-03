using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Engine;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Renderers;

/// <summary>
/// Provides extension methods for the <see cref="GraphicsCompositor"/> class to enhance its functionality.
/// These methods allow for the addition of UI stages, scene renderers, and debug render features,
/// as well as utility methods for working with render stages.
/// </summary>
public static class GraphicsCompositorExtensions
{
    /// <summary>
    /// Adds an <see cref="EntityDebugSceneRenderer"/> to the <see cref="GraphicsCompositor"/> for rendering entity debug information.
    /// </summary>
    /// <param name="graphicsCompositor">The <see cref="GraphicsCompositor"/> to which the entity debug renderer will be added.</param>
    /// <param name="options">Optional settings to customize the appearance of the debug renderer. If not provided, default options will be used.</param>
    /// <returns>The modified <see cref="GraphicsCompositor"/> instance with the entity debug renderer added.</returns>
    /// <remarks>
    /// This method adds a custom <see cref="EntityDebugSceneRenderer"/> to the graphics compositor, allowing the display of debug information
    /// such as entity names and positions in a 3D scene. The renderer can be customized using the <paramref name="options"/> parameter,
    /// which allows the user to define font size, color, and other settings.
    /// </remarks>
    /// <example>
    /// The following example demonstrates how to add an entity debug renderer with default settings:
    /// <code>
    /// graphicsCompositor.AddEntityDebugRenderer();
    /// </code>
    /// You can also specify custom options:
    /// <code>
    /// var options = new EntityDebugSceneRendererOptions { FontSize = 16, FontColor = Color.Red };
    /// graphicsCompositor.AddEntityDebugRenderer(options);
    /// </code>
    /// </example>
    public static GraphicsCompositor AddEntityDebugRenderer(this GraphicsCompositor graphicsCompositor, EntityDebugSceneRendererOptions? options = null)
    {
        graphicsCompositor.AddSceneRenderer(new EntityDebugSceneRenderer(options));

        return graphicsCompositor;
    }

    /// <summary>
    /// Adds an <see cref="EntityDebugSceneRenderer"/> to the game's <see cref="GraphicsCompositor"/> for rendering entity debug information.
    /// </summary>
    /// <param name="game">The <see cref="Game"/> instance to which the entity debug renderer will be added.</param>
    /// <param name="options">Optional settings to customize the appearance of the debug renderer. If not provided, default options will be used.</param>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="GraphicsCompositor"/> is not set in the game's <see cref="SceneSystem"/>.</exception>
    /// <remarks>
    /// This method adds a custom <see cref="EntityDebugSceneRenderer"/> to the game's graphics compositor, allowing the display of debug information
    /// such as entity names and positions in a 3D scene. The renderer can be customized using the <paramref name="options"/> parameter,
    /// which allows the user to define font size, color, and other settings.
    /// <br/><br/>
    /// IMPORTANT: This may significantly reduce frame rate. Use only for development or debugging, or in scenes with very few entities.
    /// </remarks>
    /// <example>
    /// The following example demonstrates how to add an entity debug renderer with default settings:
    /// <code>
    /// game.AddEntityDebugSceneRenderer();
    /// </code>
    /// You can also specify custom options:
    /// <code>
    /// var options = new EntityDebugSceneRendererOptions { FontSize = 16, FontColor = Color.Red };
    /// game.AddEntityDebugSceneRenderer(options);
    /// </code>
    /// </example>
    public static void AddEntityDebugSceneRenderer(this Game game, EntityDebugSceneRendererOptions? options = null)
    {
        var graphicsCompositor = game.SceneSystem.GraphicsCompositor ?? throw new InvalidOperationException(GameDefaults.GraphicsCompositorNotSet);

        graphicsCompositor.AddEntityDebugRenderer(options);
    }
}
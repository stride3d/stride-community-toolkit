using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Games;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.DebugShapes;

/// <summary>
/// Provides extension methods for adding and managing debug rendering features in a game.
/// </summary>
/// <remarks>This static class includes methods to integrate immediate debug rendering capabilities into a game,
/// such as adding debug render features to the graphics compositor, registering debug systems, and managing render
/// stages. These methods are designed to simplify the process of enabling and using debug rendering in a game
/// development environment.</remarks>
public static class DebugShapeExtensions
{
    /// <summary>
    /// <para>Adds <see cref="ImmediateDebugRenderFeature"/> and <see cref="ImmediateDebugRenderSystem"/> to the game.</para>
    /// <para>Registers the system to the service registry for easy access.</para>
    /// </summary>
    /// <param name="game">The game to add debug shapes to.</param>
    /// <param name="debugShapeRenderGroup">The render group the shapes are drawn in.</param>
    /// <returns>The system the shapes are drawn through, also registered as a service.</returns>
    /// <remarks>Shapes are visible by default only in a Debug build of your game or of the toolkit; set <see cref="ImmediateDebugRenderSystem"/>.<c>Visible</c> otherwise.</remarks>
    public static ImmediateDebugRenderSystem AddDebugShapes(this IGame game, RenderGroup debugShapeRenderGroup = RenderGroup.Group1)
    {
        game.GetSceneSystem().GraphicsCompositor.AddImmediateDebugRenderFeature();

        var debugDraw = new ImmediateDebugRenderSystem(game.Services, debugShapeRenderGroup);
#if DEBUG
        debugDraw.Visible = true;
#endif
        game.Services.AddService(debugDraw);
        game.GameSystems.Add(debugDraw);

        return debugDraw;
    }

    /// <summary>
    /// Adds an immediate debug render feature to the specified <see cref="GraphicsCompositor"/>.
    /// This method ensures the debug render feature is added only once and links it with both the
    /// "Opaque" and "Transparent" render stages.
    /// </summary>
    /// <param name="graphicsCompositor">The <see cref="GraphicsCompositor"/> to modify.</param>
    /// <exception cref="InvalidOperationException">
    /// The "Opaque" or "Transparent" render stage is not in the <paramref name="graphicsCompositor"/>.
    /// </exception>
    /// <returns>The feature now in the compositor: the existing one, or the one just added.</returns>
    public static ImmediateDebugRenderFeature AddImmediateDebugRenderFeature(this GraphicsCompositor graphicsCompositor)
    {
        if (graphicsCompositor.RenderFeatures.OfType<ImmediateDebugRenderFeature>().FirstOrDefault() is { } existing)
        {
            return existing;
        }

        if (!graphicsCompositor.TryGetRenderStage(RenderStageNames.Opaque, out var opaqueRenderStage))
        {
            throw new InvalidOperationException("The graphics compositor has no 'Opaque' render stage, which the debug shapes draw into. AddGraphicsCompositor() creates it.");
        }

        if (!graphicsCompositor.TryGetRenderStage(RenderStageNames.Transparent, out var transparentRenderStage))
        {
            throw new InvalidOperationException("The graphics compositor has no 'Transparent' render stage, which the debug shapes draw into. AddGraphicsCompositor() creates it.");
        }

        var renderFeature = new ImmediateDebugRenderFeature
        {
            RenderStageSelectors =
            {
                new ImmediateDebugRenderStageSelector { OpaqueRenderStage = opaqueRenderStage, TransparentRenderStage = transparentRenderStage },
            },
        };

        graphicsCompositor.RenderFeatures.Add(renderFeature);

        return renderFeature;
    }
}
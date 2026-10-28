using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Rendering.Gizmos;

/// <summary>
/// Adds the <see cref="OrientationGizmoRenderer"/> to a game.
/// </summary>
public static class OrientationGizmoExtensions
{
    /// <summary>
    /// Draws the world axes in a corner of the screen, turning with the camera, so the scene's
    /// orientation can be read at a glance wherever the camera is.
    /// </summary>
    /// <param name="game">The game.</param>
    /// <param name="anchor">The corner or edge the gizmo sits in. The default is the bottom left.</param>
    /// <returns>The renderer, for its size, colours and opacity.</returns>
    /// <remarks>
    /// Unlike <c>AddGroundGizmo</c>, which marks the world's origin in the scene, this is screen-space
    /// and ignores depth, the way an editor viewport's axis widget does. Calling it again returns the
    /// renderer already in the compositor and moves it to <paramref name="anchor"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The <see cref="GraphicsCompositor"/> is not set in the game's <see cref="SceneSystem"/>.</exception>
    public static OrientationGizmoRenderer AddOrientationGizmo(this IGame game, Anchor anchor = Anchor.BottomLeft)
    {
        ArgumentNullException.ThrowIfNull(game);

        var renderer = (game.Services.GetSafeServiceAs<SceneSystem>().GraphicsCompositor
            ?? throw new InvalidOperationException("The graphics compositor is not set in the game's SceneSystem."))
            .EnsureSceneRenderer(() => new OrientationGizmoRenderer());

        renderer.Anchor = anchor;

        return renderer;
    }
}
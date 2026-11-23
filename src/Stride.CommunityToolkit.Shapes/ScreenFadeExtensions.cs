using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// Puts a <see cref="ScreenFade"/> in the game.
/// </summary>
public static class ScreenFadeExtensions
{
    /// <summary>
    /// Adds a <see cref="ScreenFade"/> to the root scene on an entity of its own and returns it, ready for
    /// <see cref="ScreenFade.Transition"/> and the rest.
    /// </summary>
    /// <param name="game">The game, with its scene and compositor set up.</param>
    /// <param name="color">The colour the screen fades to, or <see langword="null"/> for black.</param>
    /// <returns>The fade, to start transitions on.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="game"/> is <see langword="null"/>.</exception>
    public static ScreenFade AddScreenFade(this IGame game, Color? color = null)
    {
        ArgumentNullException.ThrowIfNull(game);

        var fade = new ScreenFade { Color = color ?? Color.Black };
        var entity = new Entity("Screen fade") { fade };

        entity.Scene = game.GetSceneSystem().SceneInstance?.RootScene
            ?? throw new InvalidOperationException("The game has no scene yet; add the fade from the Start callback or later.");

        return fade;
    }
}
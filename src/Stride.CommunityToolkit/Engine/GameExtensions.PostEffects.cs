using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering.Images;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Post-effects access on the game's current compositor. The work is done by the
/// <see cref="GraphicsCompositorExtensions"/> twins; these exist so that code holding only the
/// <see cref="Game"/> - a script's <c>Update</c>, say - does not have to walk to
/// <c>SceneSystem.GraphicsCompositor</c> first.
/// </summary>
public static partial class GameExtensions
{
    /// <summary>
    /// The post effects of the game's current compositor, or <see langword="null"/> when there are none.
    /// </summary>
    /// <param name="game">The game.</param>
    /// <returns>See <see cref="GraphicsCompositorExtensions.GetPostEffects"/>.</returns>
    /// <remarks>
    /// The runtime half of <see cref="ConfigurePostEffects"/>: toggle an effect on a key press with
    /// <c>game.GetPostEffects()?.Bloom.Enabled ^= true</c>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="game"/> is <see langword="null"/>.</exception>
    public static PostProcessingEffects? GetPostEffects(this IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        return game.GetSceneSystem().GraphicsCompositor?.GetPostEffects();
    }

    /// <summary>
    /// Configures the post effects of the game's current compositor.
    /// </summary>
    /// <param name="game">The game.</param>
    /// <param name="configure">Receives the live <see cref="PostProcessingEffects"/>.</param>
    /// <returns>The game, for chaining.</returns>
    /// <remarks>See <see cref="GraphicsCompositorExtensions.ConfigurePostEffects"/> for what is on by default (nothing but tone mapping) and what must be added rather than enabled.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="game"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The game has no compositor, or its compositor has no post effects.</exception>
    public static IGame ConfigurePostEffects(this IGame game, Action<PostProcessingEffects> configure)
    {
        ArgumentNullException.ThrowIfNull(game);

        var compositor = game.GetSceneSystem().GraphicsCompositor
            ?? throw new InvalidOperationException(GameDefaults.GraphicsCompositorNotSet);

        compositor.ConfigurePostEffects(configure);

        return game;
    }
}
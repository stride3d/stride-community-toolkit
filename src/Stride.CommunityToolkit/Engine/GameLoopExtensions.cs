using Stride.Engine;
using Stride.Games;
using System.Reflection;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Settings for the game loop itself, as opposed to what is in the scene.
/// </summary>
public static class GameLoopExtensions
{
    /// <summary>
    /// Pins the game loop so that frame N is the same simulated instant on every machine and every
    /// run: a fixed timestep, draws in step with updates, and exactly one update per draw. What a
    /// replay, a lockstep game, a test or a screenshot wants; not what a game that should feel
    /// smooth on a slow machine wants, since a late frame is no longer caught up by extra updates.
    /// </summary>
    /// <param name="game">The game to pin.</param>
    /// <param name="step">The fixed step; left out, the game's current <see cref="GameBase.TargetElapsedTime"/> stays.</param>
    /// <remarks>
    /// A fixed timestep alone still lets a slow tick - the first frames while shaders compile, or any
    /// run on a software renderer - run two or more updates before one draw to catch up, so frame N
    /// would sit at a different simulated time from one run to the next. One update per draw is the
    /// setting that pins it, and it is <c>protected internal</c> on <see cref="GameBase"/>, which a
    /// plain <see cref="Game"/> cannot reach; reflection is the honest cost of not subclassing. A test
    /// reads the same property back, so the engine renaming it fails a test rather than silently
    /// un-pinning every golden image.
    /// </remarks>
    public static void SetDeterministic(this IGame game, TimeSpan? step = null)
    {
        ArgumentNullException.ThrowIfNull(game);

        game.IsFixedTimeStep = true;
        game.IsDrawDesynchronized = false;

        if (step is { } fixedStep)
        {
            game.TargetElapsedTime = fixedStep;
        }

        ForceOneUpdatePerDraw?.SetValue(game, true);
    }

    /// <summary>The engine's one-update-per-draw switch, protected internal on <see cref="GameBase"/>; <see langword="null"/> if the engine has renamed it.</summary>
    private static readonly PropertyInfo? ForceOneUpdatePerDraw = typeof(GameBase).GetProperty("ForceOneUpdatePerDraw", BindingFlags.Instance | BindingFlags.NonPublic);
}
using Stride.CommunityToolkit.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Extension methods for <see cref="IGame"/> that set up the Box2D helpers.
/// </summary>
public static class GameExtensions
{
    /// <summary>
    /// Puts a <see cref="Grabber2DScript"/> for <paramref name="simulation"/> on the camera entity, so the left mouse
    /// button picks up any dynamic body, drags it across its plane and throws it on release.
    /// </summary>
    /// <param name="game">The game whose camera entity gets the grabber.</param>
    /// <param name="simulation">The simulation whose bodies can be grabbed.</param>
    /// <returns>The grabber, for tuning its button, spring and force.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="game"/> or <paramref name="simulation"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The script is also a Game Studio component, under Physics, for a project that would rather place it on the
    /// camera entity in the editor; its simulation is then set from code, since a simulation is not an asset.
    /// </remarks>
    public static Grabber2DScript AddGrabber2D(this IGame game, Box2DSimulation simulation)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(simulation);

        var grabber = new Grabber2DScript { Simulation = simulation };

        game.GetCameraEntity().Add(grabber);

        return grabber;
    }
}
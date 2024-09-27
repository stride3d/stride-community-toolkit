using Stride.Games;

namespace Stride.CommunityToolkit.Games;

/// <summary>
/// Provides extension methods for the <see cref="IGame"/> interface, enhancing game management and performance tuning functionality.
/// </summary>
/// <remarks>
/// These methods offer additional control over the game's timing, frame rate, and vertical synchronization, allowing for both performance optimization and flexibility.
/// </remarks>
public static class GameExtensions
{
    /// <summary>
    /// Gets the time elapsed since the last game update in seconds as a single-precision floating-point number.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> interface providing access to game timing information.</param>
    /// <returns>The time elapsed since the last game update in seconds.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="game"/> is null.</exception>
    public static float DeltaTime(this IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        return (float)game.UpdateTime.Elapsed.TotalSeconds;
    }

    /// <summary>
    /// Gets the time elapsed since the last game update in seconds as a double-precision floating-point number.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> interface providing access to game timing information.</param>
    /// <returns>The time elapsed since the last game update in seconds with double precision.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="game"/> is null.</exception>
    public static double DeltaTimeAccurate(this IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        return game.UpdateTime.Elapsed.TotalSeconds;
    }

    /// <summary>
    /// Retrieves the current frames per second (FPS) rate of the running game.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance from which to obtain the FPS rate.</param>
    /// <returns>The current FPS rate of the game.</returns>
    public static float FPS(this IGame game) => game.UpdateTime.FramePerSecond;

    /// <summary>
    /// Sets the maximum frames per second (FPS) rate for the game when not in focus.
    /// Set <paramref name="targetFPS"/> to 0 for uncapped FPS.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance on which to set the FPS limit.</param>
    /// <param name="targetFPS">The target FPS rate. Set to 0 for uncapped FPS.</param>
    public static void SetFocusLostFPS(this IGame game, int targetFPS)
    {
        var gameBase = (GameBase)game;
        gameBase.MinimizedMinimumUpdateRate.MinimumElapsedTime = TimeSpan.FromMilliseconds(1000f / targetFPS);
    }

    /// <summary>
    /// Sets the maximum frames per second (FPS) rate for the game.
    /// Set <paramref name="targetFPS"/> to 0 for uncapped FPS.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance on which to set the FPS limit.</param>
    /// <param name="targetFPS">The target FPS rate. Set to 0 for uncapped FPS.</param>
    public static void SetMaxFPS(this IGame game, int targetFPS)
    {
        var gameBase = (GameBase)game;
        gameBase.WindowMinimumUpdateRate.MinimumElapsedTime = TimeSpan.FromMilliseconds(1000f / targetFPS);
    }

    /// <summary>
    /// Enables vertical synchronization (VSync) to prevent screen tearing by synchronizing the frame rate with the display's refresh rate.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance on which to enable VSync.</param>
    public static void EnableVSync(this IGame game)
    {
        game.GraphicsDevice.Presenter.PresentInterval = Stride.Graphics.PresentInterval.Two;
    }

    /// <summary>
    /// Disables vertical synchronization (VSync) to allow for uncapped frame rates, potentially increasing performance at the cost of possible screen tearing.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance on which to disable VSync.</param>
    public static void DisableVSync(this IGame game)
    {
        game.GraphicsDevice.Presenter.PresentInterval = Stride.Graphics.PresentInterval.Immediate;
    }

    /// <summary>
    /// Exits the game if it inherits from <see cref="GameBase"/>; otherwise, throws an exception.
    /// </summary>
    /// <param name="game">The game to exit.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="game"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="game"/> does not inherit from <see cref="GameBase"/>.</exception>
    public static void Exit(this IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        if (game is not GameBase gameBase)
        {
            throw new ArgumentException($"The provided game instance must inherit from {nameof(GameBase)} in order to exit properly.", nameof(game));
        }

        gameBase.Exit();
    }
}
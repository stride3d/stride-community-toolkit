using Stride.Games;

namespace Stride.CommunityToolkit.Games;

public static class GameExtensions
{
    /// <summary>
    /// Gets the time elapsed since the last game update in seconds as a single-precision floating-point number.
    /// </summary>
    /// <param name="gameTime">The IGame interface providing access to game timing information.</param>
    /// <returns>The time elapsed since the last game update in seconds.</returns>
    public static float DeltaTime(this IGame gameTime)
    {
        return (float)gameTime.UpdateTime.Elapsed.TotalSeconds;
    }

    /// <summary>
    /// Gets the time elapsed since the last game update in seconds as a double-precision floating-point number.
    /// </summary>
    /// <param name="gameTime">The IGame interface providing access to game timing information.</param>
    /// <returns>The time elapsed since the last game update in seconds with double precision.</returns>
    public static double DeltaTimeAccurate(this IGame gameTime)
    {
        return gameTime.UpdateTime.Elapsed.TotalSeconds;
    }

    /// <summary>
    /// Retrieves the current frames per second (FPS) rate of the running game.
    /// </summary>
    /// <param name="game">The game instance from which to obtain the FPS rate.</param>
    /// <returns>The current FPS rate of the game.</returns>
    public static float FPS(this IGame game) => game.UpdateTime.FramePerSecond;

    /// <summary>
    /// Sets the maximum frames per second (FPS) rate for the game when not in focus.
    /// Set <param name="targetFPS"/> to 0 for max possible FPS.
    /// </summary>
    /// <param name="game"></param>
    /// <param name="targetFPS"></param>
    public static void SetFocusLostFPS(this IGame game, int targetFPS)
    {
        var gameBase = (GameBase)game;
        gameBase.MinimizedMinimumUpdateRate.MinimumElapsedTime = TimeSpan.FromMilliseconds(1000f / targetFPS);
    }

    /// <summary>
    /// Sets the maximum frames per second (FPS) rate for the game.
    /// Set <param name="targetFPS"/> to 0 for max possible FPS.
    /// </summary>
    /// <param name="game"></param>
    /// <param name="targetFPS"></param>
    public static void SetMaxFPS(this IGame game, int targetFPS)
    {
        var gameBase = (GameBase)game;
        gameBase.WindowMinimumUpdateRate.MinimumElapsedTime = TimeSpan.FromMilliseconds(1000f / targetFPS);
    }

    public static void EnableVSync(this IGame game)
    {
        game.GraphicsDevice.Presenter.PresentInterval = Stride.Graphics.PresentInterval.Two;
    }

    public static void DisableVSync(this IGame game)
    {
        game.GraphicsDevice.Presenter.PresentInterval = Stride.Graphics.PresentInterval.Immediate;
    }
}
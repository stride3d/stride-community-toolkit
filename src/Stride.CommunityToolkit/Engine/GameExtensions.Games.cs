using Stride.Games;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The <see cref="IGame"/>-level helpers: frame timing, update-rate limits, vertical sync and exit.
/// </summary>
/// <remarks>
/// These need nothing from the scene, which is why they take <see cref="IGame"/> rather than
/// <see cref="Stride.Engine.Game"/>. They sit in this partial file, apart from the scene-building
/// helpers, because in the engine they belong to the <c>Stride.Games</c> layer below <c>Stride.Engine</c>;
/// keeping that seam in the file layout, but not in the namespace, means a caller needs one
/// <c>using</c> for everything on <c>game.</c> while an upstream move would still be a file move.
/// </remarks>
public static partial class GameExtensions
{
    /// <summary>
    /// Gets the elapsed update time for the current frame, in seconds.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance that provides timing information.</param>
    /// <returns>The elapsed update time as a single-precision floating-point value.</returns>
    /// <remarks>
    /// Use this value for frame-rate independent movement and animation. For calculations that need more precision, use <see cref="DeltaTimeAccurate"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="game"/> is <see langword="null"/>.</exception>
    public static float DeltaTime(this IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        return (float)game.UpdateTime.Elapsed.TotalSeconds;
    }

    /// <summary>
    /// Gets the elapsed update time for the current frame, in seconds, with double precision.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance that provides timing information.</param>
    /// <returns>The elapsed update time as a double-precision floating-point value.</returns>
    /// <remarks>
    /// This method returns the same elapsed update interval as <see cref="DeltaTime"/>, but avoids conversion to <see cref="float"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="game"/> is <see langword="null"/>.</exception>
    public static double DeltaTimeAccurate(this IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        return game.UpdateTime.Elapsed.TotalSeconds;
    }

    /// <summary>
    /// Gets the current update frame rate, in frames per second.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance that provides timing information.</param>
    /// <returns>The current frame rate as a floating-point value.</returns>
    /// <remarks>
    /// This value is provided by <see cref="IGame.UpdateTime"/> and can be used for diagnostics, performance overlays, or gameplay-independent monitoring.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="game"/> is <see langword="null"/>.</exception>
    public static float FPS(this IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        return game.UpdateTime.FramePerSecond;
    }

    /// <summary>
    /// Sets the update interval used while the window is minimized, or unfocused when <see cref="GameBase.TreatNotFocusedLikeMinimized"/> is set.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance to configure.</param>
    /// <param name="targetFPS">The target update rate, in frames per second, used to calculate the minimized update interval. Must be greater than 0.</param>
    /// <remarks>
    /// <para>This method configures <see cref="GameBase.MinimizedMinimumUpdateRate"/> and is useful for reducing resource usage while the game is minimized.</para>
    /// <para>The <paramref name="game"/> instance must be a <see cref="GameBase"/> implementation.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="game"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidCastException">Thrown when <paramref name="game"/> is not a <see cref="GameBase"/> instance.</exception>
    public static void SetFocusLostFPS(this IGame game, int targetFPS)
    {
        ArgumentNullException.ThrowIfNull(game);

        var gameBase = (GameBase)game;
        gameBase.MinimizedMinimumUpdateRate.MinimumElapsedTime = TimeSpan.FromMilliseconds(1000f / targetFPS);
    }

    /// <summary>
    /// Sets the minimum update interval used while the game window is active.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance to configure.</param>
    /// <param name="targetFPS">The target update rate, in frames per second, used to calculate the active-window update interval. Must be greater than 0.</param>
    /// <remarks>
    /// <para>This method configures <see cref="GameBase.WindowMinimumUpdateRate"/> and can be used to limit the update rate while the game is running normally.</para>
    /// <para>The <paramref name="game"/> instance must be a <see cref="GameBase"/> implementation.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="game"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidCastException">Thrown when <paramref name="game"/> is not a <see cref="GameBase"/> instance.</exception>
    public static void SetMaxFPS(this IGame game, int targetFPS)
    {
        ArgumentNullException.ThrowIfNull(game);

        var gameBase = (GameBase)game;
        gameBase.WindowMinimumUpdateRate.MinimumElapsedTime = TimeSpan.FromMilliseconds(1000f / targetFPS);
    }

    /// <summary>
    /// Sets the presentation interval to wait for every second vertical blank.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance to configure.</param>
    /// <remarks>
    /// <para>This method sets <see cref="Stride.Graphics.GraphicsPresenter.PresentInterval"/> to <see cref="Stride.Graphics.PresentInterval.Two"/>.</para>
    /// <para>Waiting for vertical blanks can reduce tearing, but may increase presentation latency and reduce the effective frame rate.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="game"/> is <see langword="null"/>.</exception>
    public static void EnableVSync(this IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        game.GraphicsDevice.Presenter.PresentInterval = Stride.Graphics.PresentInterval.Two;
    }

    /// <summary>
    /// Sets the presentation interval to present frames immediately.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance to configure.</param>
    /// <remarks>
    /// <para>This method sets <see cref="Stride.Graphics.GraphicsPresenter.PresentInterval"/> to <see cref="Stride.Graphics.PresentInterval.Immediate"/>.</para>
    /// <para>Immediate presentation can improve responsiveness, but may cause visible tearing.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="game"/> is <see langword="null"/>.</exception>
    public static void DisableVSync(this IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        game.GraphicsDevice.Presenter.PresentInterval = Stride.Graphics.PresentInterval.Immediate;
    }

    /// <summary>
    /// Requests the game to exit.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance to exit.</param>
    /// <remarks>
    /// The <paramref name="game"/> instance must be a <see cref="GameBase"/> implementation because <see cref="GameBase.Exit"/> performs the shutdown request.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="game"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="game"/> is not a <see cref="GameBase"/> instance.</exception>
    public static void Exit(this IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        if (game is not GameBase gameBase)
            throw new ArgumentException($"The provided game instance must inherit from {nameof(GameBase)} in order to exit properly.", nameof(game));

        gameBase.Exit();
    }
}
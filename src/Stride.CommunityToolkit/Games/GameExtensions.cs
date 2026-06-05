using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Engine;
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
    /// Creates an entity containing a 3D procedural primitive model of the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance providing access to game services.</param>
    /// <param name="type">The type of 3D primitive to build.</param>
    /// <param name="options">Optional creation parameters including size, material, render group, and entity name. If null, default options are used.</param>
    /// <returns>A new <see cref="Entity"/> containing a configured <see cref="ModelComponent"/> with the generated primitive model.</returns>
    /// <remarks>
    /// <para>The returned entity must be added to a scene to be rendered. The entity can be further customized using fluent extension methods.</para>
    /// <para>If a material is provided in <paramref name="options"/>, it will be added to the model's material collection.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="game"/> is null.</exception>
    public static Entity Create3DPrimitive(this IGame game, PrimitiveModelType type, Primitive3DEntityOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(game);

        options ??= new();

        var modelBase = Procedural3DModelBuilder.Build(type, options.Size);

        var model = modelBase.Generate(game.Services);

        if (options.Material != null)
            model.Materials.Add(options.Material);

        var entity = new Entity(options.EntityName) { new ModelComponent(model) { RenderGroup = options.RenderGroup } };

        return entity;
    }

    /// <summary>
    /// Creates an entity containing a 2D (flat) procedural primitive model of the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance providing access to game services.</param>
    /// <param name="type">The type of 2D primitive to build.</param>
    /// <param name="options">Optional creation parameters including size, custom polygon vertices, depth, material, render group, and entity name. If null, default options are used.</param>
    /// <returns>A new <see cref="Entity"/> containing a configured <see cref="ModelComponent"/> with the generated primitive model.</returns>
    /// <remarks>
    /// <para>The returned entity must be added to a scene to be rendered. The entity can be further customized using fluent extension methods.</para>
    /// <para>If a material is provided in <paramref name="options"/>, it will be added to the model's material collection.</para>
    /// <para>The <c>Depth</c> parameter in options controls the Z-axis thickness of the 2D primitive.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="game"/> is null.</exception>
    public static Entity Create2DPrimitive(this IGame game, Primitive2DModelType type, Primitive2DEntityOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(game);

        options ??= new();
        options.Size ??= type switch
        {
            Primitive2DModelType.Capsule => new Vector2(0.25f, 1f),
            Primitive2DModelType.Rectangle => new Vector2(0.5f, 1f),
            _ => options.Size
        };

        var modelBase = Procedural2DModelBuilder.Build(type, options.Size, options.Depth, options.Vertices);

        var model = modelBase.Generate(game.Services);

        if (options.Material != null)
            model.Materials.Add(options.Material);

        var entity = new Entity(options.EntityName) { new ModelComponent(model) { RenderGroup = options.RenderGroup } };

        return entity;
    }

    /// <summary>
    /// Gets the time elapsed since the last game update in seconds as a single-precision floating-point number.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance providing access to game timing information.</param>
    /// <returns>The time elapsed since the last game update in seconds.</returns>
    /// <remarks>
    /// This value is commonly used to achieve frame-rate independent movement and animations by multiplying it with velocity or rate values.
    /// For higher precision timing requirements, use <see cref="DeltaTimeAccurate"/> instead.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="game"/> is null.</exception>
    public static float DeltaTime(this IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        return (float)game.UpdateTime.Elapsed.TotalSeconds;
    }

    /// <summary>
    /// Gets the time elapsed since the last game update in seconds as a double-precision floating-point number.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance providing access to game timing information.</param>
    /// <returns>The time elapsed since the last game update in seconds with double precision.</returns>
    /// <remarks>
    /// This method provides higher precision than <see cref="DeltaTime"/> and is suitable for calculations requiring greater accuracy.
    /// Use this for precise physics simulations or timing-critical operations where floating-point precision loss could accumulate over time.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="game"/> is null.</exception>
    public static double DeltaTimeAccurate(this IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        return game.UpdateTime.Elapsed.TotalSeconds;
    }

    /// <summary>
    /// Retrieves the current frames per second (FPS) rate of the running game.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance from which to retrieve the FPS rate.</param>
    /// <returns>The current FPS rate of the game as a floating-point value.</returns>
    /// <remarks>
    /// This value represents the actual frame rate at which the game is currently running and is updated each frame.
    /// It can be used for performance monitoring, debugging, or displaying FPS information to the user.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="game"/> is null.</exception>
    public static float FPS(this IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        return game.UpdateTime.FramePerSecond;
    }

    /// <summary>
    /// Sets the maximum frames per second (FPS) rate for the game when the window is not in focus or minimized.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance on which to set the FPS limit.</param>
    /// <param name="targetFPS">The target FPS rate when the window loses focus. Set to 0 for uncapped FPS.</param>
    /// <remarks>
    /// <para>This method is useful for reducing resource consumption when the game window is not actively being used.</para>
    /// <para>Setting <paramref name="targetFPS"/> to 0 removes the FPS cap, allowing the game to run at maximum speed even when not in focus.</para>
    /// <para>The game instance must be castable to <see cref="GameBase"/> for this method to work properly.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="game"/> is null.</exception>
    /// <exception cref="InvalidCastException">Thrown if <paramref name="game"/> cannot be cast to <see cref="GameBase"/>.</exception>
    public static void SetFocusLostFPS(this IGame game, int targetFPS)
    {
        ArgumentNullException.ThrowIfNull(game);

        var gameBase = (GameBase)game;
        gameBase.MinimizedMinimumUpdateRate.MinimumElapsedTime = TimeSpan.FromMilliseconds(1000f / targetFPS);
    }

    /// <summary>
    /// Sets the maximum frames per second (FPS) rate for the game when the window is in focus.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance on which to set the FPS limit.</param>
    /// <param name="targetFPS">The target FPS rate. Set to 0 for uncapped FPS.</param>
    /// <remarks>
    /// <para>This method limits the game's frame rate to the specified value when the window is active and in focus.</para>
    /// <para>Setting <paramref name="targetFPS"/> to 0 removes the FPS cap, allowing the game to run at maximum speed.</para>
    /// <para>The game instance must be castable to <see cref="GameBase"/> for this method to work properly.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="game"/> is null.</exception>
    /// <exception cref="InvalidCastException">Thrown if <paramref name="game"/> cannot be cast to <see cref="GameBase"/>.</exception>
    public static void SetMaxFPS(this IGame game, int targetFPS)
    {
        ArgumentNullException.ThrowIfNull(game);

        var gameBase = (GameBase)game;
        gameBase.WindowMinimumUpdateRate.MinimumElapsedTime = TimeSpan.FromMilliseconds(1000f / targetFPS);
    }

    /// <summary>
    /// Enables vertical synchronization (VSync) to prevent screen tearing by synchronizing the frame rate with the display's refresh rate.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance on which to enable VSync.</param>
    /// <remarks>
    /// <para>When VSync is enabled, the frame rate is limited to the display's refresh rate (typically 60Hz, 120Hz, or 144Hz).</para>
    /// <para>This eliminates screen tearing but may introduce input latency and can reduce the overall frame rate.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="game"/> is null.</exception>
    public static void EnableVSync(this IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        game.GraphicsDevice.Presenter.PresentInterval = Stride.Graphics.PresentInterval.Two;
    }

    /// <summary>
    /// Disables vertical synchronization (VSync) to allow for uncapped frame rates, potentially increasing performance at the cost of possible screen tearing.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance on which to disable VSync.</param>
    /// <remarks>
    /// <para>When VSync is disabled, the frame rate is no longer limited by the display's refresh rate, allowing for higher FPS.</para>
    /// <para>This may improve input responsiveness but can cause visual screen tearing artifacts.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="game"/> is null.</exception>
    public static void DisableVSync(this IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        game.GraphicsDevice.Presenter.PresentInterval = Stride.Graphics.PresentInterval.Immediate;
    }

    /// <summary>
    /// Exits the game if it inherits from <see cref="GameBase"/>; otherwise, throws an exception.
    /// </summary>
    /// <param name="game">The game to exit.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="game"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="game"/> does not inherit from <see cref="GameBase"/>.</exception>
    public static void Exit(this IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        if (game is not GameBase gameBase)
            throw new ArgumentException($"The provided game instance must inherit from {nameof(GameBase)} in order to exit properly.", nameof(game));

        gameBase.Exit();
    }
}
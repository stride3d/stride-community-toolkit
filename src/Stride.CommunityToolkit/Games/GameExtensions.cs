using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.Games;

/// <summary>
/// Provides convenience extension methods for <see cref="IGame"/> instances.
/// </summary>
/// <remarks>
/// Includes helpers for creating primitive entities, reading timing information, adjusting update rates, changing presentation settings, and exiting the game.
/// </remarks>
public static class GameExtensions
{
    /// <summary>
    /// Creates an entity with a 3D procedural primitive model of the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance used to access game services.</param>
    /// <param name="type">The 3D primitive type to create.</param>
    /// <param name="options">Optional creation parameters, including size, material, render group, entity name, and position. If <see langword="null"/>, default options are used.</param>
    /// <returns>A new <see cref="Entity"/> with a <see cref="ModelComponent"/> containing the generated primitive model.</returns>
    /// <remarks>
    /// <para>The returned entity is not added to a scene automatically. Assign it to a scene before rendering.</para>
    /// <para>If a material is specified in <paramref name="options"/>, it is added to the generated model's material collection.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="game"/> is <see langword="null"/>.</exception>
    public static Entity Create3DPrimitive(this IGame game, PrimitiveModelType type, Primitive3DEntityOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(game);

        options ??= new();

        var modelBase = Procedural3DModelBuilder.Build(type, options.Size);

        var model = modelBase.Generate(game.Services);

        if (options.Material != null)
        {
            model.Materials.Add(options.Material);
        }

        var entity = new Entity(options.EntityName) { new ModelComponent(model) { RenderGroup = options.RenderGroup } };

        if (options.Position is { } position)
        {
            entity.Transform.Position = position;
        }

        return entity;
    }

    /// <summary>
    /// Creates an entity with a 2D procedural primitive model of the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance used to access game services.</param>
    /// <param name="type">The 2D primitive type to create.</param>
    /// <param name="options">Optional creation parameters, including size, custom polygon vertices, depth, material, render group, entity name, and position. If <see langword="null"/>, default options are used.</param>
    /// <returns>A new <see cref="Entity"/> with a <see cref="ModelComponent"/> containing the generated primitive model.</returns>
    /// <remarks>
    /// <para>The returned entity is not added to a scene automatically. Assign it to a scene before rendering.</para>
    /// <para>If a material is specified in <paramref name="options"/>, it is added to the generated model's material collection.</para>
    /// <para>If no size is specified for capsules or rectangles, this method applies default dimensions before building the model.</para>
    /// <para>The <c>Depth</c> option controls the generated mesh thickness along the Z axis.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="game"/> is <see langword="null"/>.</exception>
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
        {
            model.Materials.Add(options.Material);
        }

        var entity = new Entity(options.EntityName) { new ModelComponent(model) { RenderGroup = options.RenderGroup } };

        if (options.Position is { } position)
        {
            entity.Transform.Position = position;
        }

        return entity;
    }

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
    /// Sets the minimum update interval used while the game window is minimized.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance to configure.</param>
    /// <param name="targetFPS">The target update rate, in frames per second, used to calculate the minimized update interval. Must be greater than 0.</param>
    /// <remarks>
    /// <para>This method configures <see cref="GameBase.MinimizedMinimumUpdateRate"/> and is useful for reducing resource usage while the game is minimized.</para>
    /// <para>Setting <paramref name="targetFPS"/> to zero disables throttling.</para>
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
    /// <para>Setting <paramref name="targetFPS"/> to zero disables throttling.</para>
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
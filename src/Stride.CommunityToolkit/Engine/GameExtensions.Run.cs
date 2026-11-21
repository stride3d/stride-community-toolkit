using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Games;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The <c>Run</c> overloads: the game loop with a <see cref="SceneSetup"/>, a start callback and a per-frame update.
/// </summary>
public static partial class GameExtensions
{
    /// <summary>
    /// Starts the game loop, calling <paramref name="start"/> once the root scene exists and
    /// <paramref name="update"/> on every frame after that.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The engine creates the root scene only inside <see cref="GameBase.Run(GameContext)"/>, so
    /// scene setup cannot happen before this call. <c>Run</c> schedules a script that invokes
    /// <paramref name="start"/> on the first frame, then loops <paramref name="update"/> once per frame
    /// until the game exits. Both callbacks run on the game thread, inside the script system, with
    /// the same timing and exception behaviour as a <see cref="StartupScript"/> or <see cref="SyncScript"/>:
    /// an exception thrown by either one propagates out of this method.
    /// </para>
    /// <para>
    /// <paramref name="start"/> may be asynchronous - see the <see cref="Run(Game, Func{Scene, Task}, Action{Scene, GameTime}, GameContext)"/>
    /// overload. <paramref name="update"/> is deliberately synchronous: it is the per-frame callback, and
    /// anything that needs to await across frames belongs in an async <paramref name="start"/> that runs
    /// its own loop.
    /// </para>
    /// </remarks>
    /// <param name="game">The game to run.</param>
    /// <param name="start">Called once, with the root scene, before the first <paramref name="update"/>. Optional.</param>
    /// <param name="update">
    /// Called every frame with the current root scene and the current <see cref="GameBase.UpdateTime"/>.
    /// The scene is read fresh each frame, so after a scene switch it is the new root scene. Optional.
    /// </param>
    /// <param name="context">
    /// The windowing context to run in - a host control to render into, a fixed initial size, an SDL or
    /// headless backend. Leave <see langword="null"/> to let the engine pick the platform default.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="game"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// using var game = new Game();
    ///
    /// game.Run(start: Start, update: Update);
    ///
    /// void Start(Scene scene) { /* the default 3D scene is in place: add entities */ }
    ///
    /// void Update(Scene scene, GameTime time) { /* per frame */ }
    /// </code>
    /// </example>
    public static void Run(this Game game, Action<Scene>? start = null, Action<Scene, GameTime>? update = null, GameContext? context = null)
        => game.Run(new SceneSetup3D(), start, update, context);

    /// <summary>
    /// Starts the game loop with an asynchronous <paramref name="start"/>: the update loop begins only
    /// after the returned task completes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <paramref name="start"/> runs as a script, so it can wait on the script system between steps -
    /// <c>await game.GetScriptSystem().NextFrame()</c> to let physics attach, or
    /// <see cref="ScriptSystemExtensions.Delay(ScriptSystem, float)"/> for a countdown - without writing
    /// a <see cref="StartupScript"/> or <see cref="AsyncScript"/> class. A <paramref name="start"/> that never
    /// completes is valid, and is how to write a per-frame loop that also awaits: the same idiom as
    /// <see cref="AsyncScript.Execute"/>.
    /// </para>
    /// <para>
    /// C# binds an <c>async</c> lambda or a <see cref="Task"/>-returning method group to this overload
    /// and a synchronous one to <see cref="Run(Game, Action{Scene}, Action{Scene, GameTime}, GameContext)"/>,
    /// so an <c>async</c> start is never silently compiled as <c>async void</c>.
    /// </para>
    /// </remarks>
    /// <param name="game">The game to run.</param>
    /// <param name="start">Called once with the root scene; the update loop starts when its task completes.</param>
    /// <param name="update">
    /// Called every frame with the current root scene and the current <see cref="GameBase.UpdateTime"/>.
    /// The scene is read fresh each frame, so after a scene switch it is the new root scene. Optional.
    /// </param>
    /// <param name="context">
    /// The windowing context to run in. Leave <see langword="null"/> to let the engine pick the platform default.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="game"/> or <paramref name="start"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// using var game = new Game();
    ///
    /// game.Run(start: async scene =>
    /// {
    ///     await game.GetScriptSystem().Delay(3);   // countdown, splash, loading...
    ///
    ///     SpawnPlayer(scene);
    /// }, update: Update);
    /// </code>
    /// </example>
    public static void Run(this Game game, Func<Scene, Task> start, Action<Scene, GameTime>? update = null, GameContext? context = null)
        => game.Run(new SceneSetup3D(), start, update, context);

    /// <summary>
    /// Applies <paramref name="setup"/> and runs the game: the setup's pieces are added to the root scene in a fixed
    /// order before <paramref name="start"/> runs, then <paramref name="update"/> runs every frame.
    /// </summary>
    /// <param name="game">The game to run.</param>
    /// <param name="setup">What the scene gets before <paramref name="start"/>: a <see cref="SceneSetup3D"/>, a <see cref="SceneSetup2D"/>, or <see cref="SceneSetup.None"/> for nothing.</param>
    /// <param name="start">Runs once, after the setup, with the root scene.</param>
    /// <param name="update">Runs every frame with the root scene and the game time, after <paramref name="start"/> has run.</param>
    /// <param name="context">The game context, or <see langword="null"/> for a window.</param>
    /// <remarks>
    /// The setup's <see cref="SceneSetup.GameSettings"/> and <see cref="SceneSetup.Physics"/> are applied before the
    /// engine starts; the compositor, camera, lights, sky and ground once the root scene exists, so
    /// <paramref name="start"/> finds them in place.
    /// </remarks>
    /// <example>
    /// <code>
    /// game.Run(new SceneSetup3D { Lighting = LightingSetup.Studio() }.UseBepu(), start: Start);
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="game"/> or <paramref name="setup"/> is <see langword="null"/>.</exception>
    public static void Run(this Game game, SceneSetup setup, Action<Scene>? start = null, Action<Scene, GameTime>? update = null, GameContext? context = null)
    {
        RunCore(game, setup, start is null ? null : scene => { start(scene); return Task.CompletedTask; }, update, context);
    }

    /// <summary>
    /// Applies <paramref name="setup"/> and runs the game with an awaitable <paramref name="start"/>; <paramref name="update"/>
    /// begins once it has completed.
    /// </summary>
    /// <inheritdoc cref="Run(Game, SceneSetup, Action{Scene}?, Action{Scene, GameTime}?, GameContext?)"/>
    public static void Run(this Game game, SceneSetup setup, Func<Scene, Task> start, Action<Scene, GameTime>? update = null, GameContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(start);

        RunCore(game, setup, start, update, context);
    }

    private static void RunCore(Game game, SceneSetup setup, Func<Scene, Task>? start, Action<Scene, GameTime>? update, GameContext? context)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(setup);

        // The settings and the physics service: read while the engine initialises, so before Run
        setup.Prepare(game);

        game.GetScriptSystem().Scheduler.Add(RootScript);

        // Opt-in, environment-driven, and a no-op unless a capture was asked for. Every example reaches
        // the loop through Run, which is what makes this one place instead of sixty.
        ScreenshotCapture.TrySchedule(game);

        game.Run(context);

        async Task RootScript()
        {
            // The root scene exists now: the compositor, camera, lights, sky and ground, then the caller's content
            setup.Apply(game);

            if (start is not null)
                await start(RootScene());

            // A master with no render feature draws as one model and says nothing; this says it once
            InstancingSupport.WarnIfMissing(game);

            if (update is null) return;

            while (true)
            {
                update(RootScene(), game.UpdateTime);

                await game.GetScriptSystem().NextFrame();
            }
        }

        // Re-read every frame on purpose. Both SceneSystem.SceneInstance and SceneInstance.RootScene
        // have public setters and replacing them is how a game switches scenes; a scene captured once
        // here would keep handing update the detached one.
        Scene RootScene() => game.GetSceneSystem().SceneInstance.RootScene;
    }
}
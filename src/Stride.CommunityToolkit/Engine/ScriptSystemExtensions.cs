using Stride.Core.MicroThreading;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Engine.Processors;
using Stride.Games;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides extension methods for the <see cref="ScriptSystem"/> to facilitate time-based operations, including delays and frame-based executions. These extensions are useful for managing time in game logic, such as delaying actions or executing logic over a period of time.
/// </summary>
/// <example>
///
/// <para>Example 1: Delaying an action for 2 seconds in game time (affected by time warp): </para>
/// <code>
/// await scriptSystem.DelayWarped(2.0f);
/// // Action will be delayed for 2 in-game seconds, accounting for any time warp factors.
/// </code>
///
/// <para>Example 2: Running a continuous action for 5 seconds of real time (unaffected by time warp): </para>
/// <code>
/// await scriptSystem.ExecuteInTime(5.0f, elapsed =>
/// {
///     // Perform action based on the elapsed time in real seconds.
///     DebugText.Print($"Time elapsed: {elapsed} seconds");
/// });
/// </code>
///
/// <para>Example 3: Delaying an action for 3 seconds of game time, unaffected by time warp: </para>
/// <code>
/// await scriptSystem.Delay(3.0f);
/// // Action will be delayed for 3 seconds of game time, however the time warp is set.
/// </code>
/// </example>
/// <remarks>
/// These extensions allow you to control how game logic interacts with time, whether you need frame-based operations or time delays. The methods are useful for both real-time and in-game time-based operations.
/// </remarks>
public static class ScriptSystemExtensions
{
    /// <summary>
    /// Waits for a specified amount of time, adjusting for any time warp factors.
    /// </summary>
    /// <param name="script">The <see cref="ScriptSystem"/> instance used to execute the delay.</param>
    /// <param name="seconds">The number of seconds to delay execution.</param>
    /// <returns>A <see cref="Task"/> representing the delay.</returns>
    /// <remarks>This delay takes into account the game's time warp factor, using the <c>WarpElapsed</c> property of <c>UpdateTime</c>.</remarks>
    public static Task DelayWarped(this ScriptSystem script, float seconds)
        => RunFor(script, seconds, warped: true, action: null);

    /// <summary>
    /// Waits <paramref name="seconds"/> of game time, counted frame by frame and not scaled by the time warp;
    /// <see cref="DelayWarped"/> is the scaled one.
    /// </summary>
    /// <param name="script">The <see cref="ScriptSystem"/> instance used to execute the delay.</param>
    /// <param name="seconds">The number of seconds to wait.</param>
    /// <returns>A <see cref="Task"/> representing the delay.</returns>
    /// <remarks>The wait is on the game's clock, so it does not count while the game is paused, and it ends early when the game stops.</remarks>
    public static Task Delay(this ScriptSystem script, float seconds)
        => RunFor(script, seconds, warped: false, action: null);

    /// <summary>
    /// Executes an action every frame for a specified duration, adjusting for any time warp factors.
    /// </summary>
    /// <param name="script">The <see cref="ScriptSystem"/> instance used to execute the action.</param>
    /// <param name="seconds">The duration in seconds for which the action will be executed.</param>
    /// <param name="action">The action to perform on each frame, which receives the elapsed time as a parameter.</param>
    /// <returns>A <see cref="Task"/> representing the action execution.</returns>
    /// <remarks>This method accounts for the game's <c>WarpElapsed</c> time factor during execution.</remarks>
    public static Task ExecuteInWarpedTime(this ScriptSystem script, float seconds, Action<float> action)
        => RunFor(script, seconds, warped: true, action);

    /// <summary>
    /// Executes an action every frame for a specified duration, counted in the game's elapsed time without the warp factor.
    /// </summary>
    /// <param name="script">The <see cref="ScriptSystem"/> instance used to execute the action.</param>
    /// <param name="seconds">The duration in seconds for which the action will be executed.</param>
    /// <param name="action">The action to perform on each frame, which receives the elapsed time as a parameter.</param>
    /// <returns>A <see cref="Task"/> representing the action execution.</returns>
    /// <remarks>Counts the game's elapsed time, <c>UpdateTime.Elapsed</c>, leaving out only the warp factor; it is not wall-clock time.</remarks>
    public static Task ExecuteInTime(this ScriptSystem script, float seconds, Action<float> action)
        => RunFor(script, seconds, warped: false, action);

    /// <summary>
    /// Waits for the specified delay <paramref name="delay"/>.
    /// </summary>
    /// <param name="scriptSystem">The <see cref="ScriptSystem"/>.</param>
    /// <param name="delay">The amount of time to wait.</param>
    /// <returns>The <see cref="Task"/> to await.</returns>
    public static Task WaitFor(this ScriptSystem scriptSystem, TimeSpan delay)
        => scriptSystem.WaitFor(delay, default);

    /// <summary>
    /// Adds a micro thread function to the <paramref name="scriptSystem"/> that executes when the event is published.
    /// </summary>
    /// <typeparam name="T">The type of the event handler parameter.</typeparam>
    /// <param name="scriptSystem">The <see cref="ScriptSystem"/>.</param>
    /// <param name="eventKey">The event to wait for.</param>
    /// <param name="action">The micro thread function to execute.</param>
    /// <param name="priority">The priority of the micro thread action being added.</param>
    /// <returns>The <see cref="MicroThread"/>.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="scriptSystem"/>, <paramref name="eventKey"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// If the <paramref name="action"/> is a <see cref="ScriptComponent"/> instance method the micro thread will be automatically stopped if the <see cref="ScriptComponent"/> or <see cref="Entity"/> is removed.
    /// </remarks>
    public static MicroThread AddOnEventAction<T>(this ScriptSystem scriptSystem, EventKey<T> eventKey, Action<T> action, long priority = 0L)
    {
        ArgumentNullException.ThrowIfNull(eventKey);

        return scriptSystem.AddOnEventAction(new EventReceiver<T>(eventKey), action, priority);
    }

    /// <summary>
    /// Adds a micro thread function to the <paramref name="scriptSystem"/> that executes when the event is published.
    /// </summary>
    /// <typeparam name="T">The type of the event handler parameter.</typeparam>
    /// <param name="scriptSystem">The <see cref="ScriptSystem"/>.</param>
    /// <param name="receiver">The event receiver to listen to for.</param>
    /// <param name="action">The micro thread function to execute.</param>
    /// <param name="priority">The priority of the micro thread action being added.</param>
    /// <returns>The <see cref="MicroThread"/>.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="scriptSystem"/>, <paramref name="receiver"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// If the <paramref name="action"/> is a <see cref="ScriptComponent"/> instance method the micro thread will be automatically stopped if the <see cref="ScriptComponent"/> or <see cref="Entity"/> is removed.
    /// </remarks>
    public static MicroThread AddOnEventAction<T>(this ScriptSystem scriptSystem, EventReceiver<T> receiver, Action<T> action, long priority = 0L)
    {
        ArgumentNullException.ThrowIfNull(action);

        return scriptSystem.AddOnEvent(receiver, action, data => { action(data); return Task.CompletedTask; }, priority);
    }

    /// <inheritdoc cref="AddOnEventAction{T}(ScriptSystem, EventKey{T}, Action{T}, long)"/>
    public static MicroThread AddOnEventTask<T>(this ScriptSystem scriptSystem, EventKey<T> eventKey, Func<T, Task> action, long priority = 0L)
    {
        ArgumentNullException.ThrowIfNull(eventKey);

        return scriptSystem.AddOnEventTask(new EventReceiver<T>(eventKey), action, priority);
    }

    /// <inheritdoc cref="AddOnEventAction{T}(ScriptSystem, EventReceiver{T}, Action{T}, long)"/>
    public static MicroThread AddOnEventTask<T>(this ScriptSystem scriptSystem, EventReceiver<T> receiver, Func<T, Task> action, long priority = 0L)
    {
        ArgumentNullException.ThrowIfNull(action);

        return scriptSystem.AddOnEvent(receiver, action, action, priority);
    }

    /// <summary>
    /// Adds a micro thread function to the <paramref name="scriptSystem"/> that executes after waiting specified delay.
    /// </summary>
    /// <param name="scriptSystem">The <see cref="ScriptSystem"/>.</param>
    /// <param name="action">The micro thread function to execute.</param>
    /// <param name="delay">The amount of time to wait for.</param>
    /// <param name="priority">The priority of the micro thread action being added.</param>
    /// <returns>The <see cref="MicroThread"/>.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="scriptSystem"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="delay"/> is zero or negative.</exception>
    /// <remarks>
    /// If the <paramref name="action"/> is a <see cref="ScriptComponent"/> instance method the micro thread will be automatically stopped if the <see cref="ScriptComponent"/> or <see cref="Entity"/> is removed.
    /// </remarks>
    public static MicroThread AddTask(this ScriptSystem scriptSystem, Func<Task> action, TimeSpan delay, long priority = 0L)
    {
        ArgumentNullException.ThrowIfNull(action);

        return scriptSystem.AddDelayed(action, action, delay, priority);
    }

    /// <inheritdoc cref="AddTask(ScriptSystem, Func{Task}, TimeSpan, long)"/>
    public static MicroThread AddAction(this ScriptSystem scriptSystem, Action action, TimeSpan delay, long priority = 0L)
    {
        ArgumentNullException.ThrowIfNull(action);

        return scriptSystem.AddDelayed(action, () => { action(); return Task.CompletedTask; }, delay, priority);
    }

    /// <summary>
    /// Adds a micro thread function to the <paramref name="scriptSystem"/> that executes after waiting specified delay and repeats execution.
    /// </summary>
    /// <param name="scriptSystem">The <see cref="ScriptSystem"/>.</param>
    /// <param name="action">The micro thread function to execute.</param>
    /// <param name="delay">The amount of time to wait for.</param>
    /// <param name="repeatEvery">The amount of time to wait for between repetition.</param>
    /// <param name="priority">The priority of the micro thread action being added.</param>
    /// <returns>The <see cref="MicroThread"/>.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="scriptSystem"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="delay"/> or <paramref name="repeatEvery"/> is zero or negative.</exception>
    /// <remarks>
    /// If the <paramref name="action"/> is a <see cref="ScriptComponent"/> instance method the micro thread will be automatically stopped if the <see cref="ScriptComponent"/> or <see cref="Entity"/> is removed.
    /// </remarks>
    public static MicroThread AddTask(this ScriptSystem scriptSystem, Func<Task> action, TimeSpan delay, TimeSpan repeatEvery, long priority = 0L)
    {
        ArgumentNullException.ThrowIfNull(action);

        return scriptSystem.AddRepeating(action, action, delay, repeatEvery, priority);
    }

    /// <inheritdoc cref="AddTask(ScriptSystem, Func{Task}, TimeSpan, TimeSpan, long)"/>
    public static MicroThread AddAction(this ScriptSystem scriptSystem, Action action, TimeSpan delay, TimeSpan repeatEvery, long priority = 0L)
    {
        ArgumentNullException.ThrowIfNull(action);

        return scriptSystem.AddRepeating(action, () => { action(); return Task.CompletedTask; }, delay, repeatEvery, priority);
    }

    /// <summary>
    /// Adds a micro thread function to the <paramref name="scriptSystem"/> that runs every frame for <paramref name="duration"/>, passing the progress.
    /// </summary>
    /// <param name="scriptSystem">The <see cref="ScriptSystem"/>.</param>
    /// <param name="action">The micro thread function to execute. The parameter is the progress over time from 0.0f to 1.0f.</param>
    /// <param name="duration">The duration of the time to execute the micro thread function for.</param>
    /// <param name="priority">The priority of the micro thread action being added.</param>
    /// <returns>The <see cref="MicroThread"/>.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="scriptSystem"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="duration"/> is zero or negative.</exception>
    /// <remarks>
    /// If the <paramref name="action"/> is a <see cref="ScriptComponent"/> instance method the micro thread will be automatically stopped if the <see cref="ScriptComponent"/> or <see cref="Entity"/> is removed.
    /// </remarks>
    public static MicroThread AddOverTimeAction(this ScriptSystem scriptSystem, Action<float> action, TimeSpan duration, long priority = 0L)
    {
        ArgumentNullException.ThrowIfNull(action);
        ThrowIfNotPositive(duration);

        return scriptSystem.AddWatched(action, async (game, watcher) =>
        {
            var elapsed = TimeSpan.Zero;

            while (game.IsRunning && watcher.IsActive)
            {
                elapsed += game.UpdateTime.Elapsed;

                if (elapsed >= duration)
                {
                    action(1.0f);
                    break;
                }

                action((float)(elapsed / duration));

                await scriptSystem.NextFrame();
            }
        }, priority);
    }

    /// <summary>
    /// Runs an action on the game thread, from any thread: a network callback, a timer, a task that
    /// finished on the thread pool. The action runs on the next update, after the scripts scheduled
    /// before it.
    /// </summary>
    /// <param name="script">The script system; <c>game.Script</c> or a script's <c>Script</c> property.</param>
    /// <param name="action">What to run. Touch the scene, entities and components only here.</param>
    /// <returns>The micro thread, which can be cancelled before it runs.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="script"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Scenes and entity collections are not thread-safe; adding an entity from another thread throws
    /// "Collection was modified". Build the entity where you are, then add it to the scene inside the action.
    /// Scheduling is safe from any thread because the scheduler takes continuations from the thread pool
    /// the same way. The action runs even if the script that scheduled it has been removed by then, so an
    /// action that touches its own script checks that it is still attached.
    /// </remarks>
    public static MicroThread RunOnGameThread(this ScriptSystem script, Action action)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(action);

        return script.AddTask(() =>
        {
            action();

            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Runs a function on the game thread, from any thread, and hands its result back as a task: read a
    /// position, count the entities, or build and add an entity and return it.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="script">The script system; <c>game.Script</c> or a script's <c>Script</c> property.</param>
    /// <param name="function">What to run on the game thread.</param>
    /// <returns>A task that completes with the function's result, or faults with its exception, once the game thread has run it.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="script"/> or <paramref name="function"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Await it from a background thread only. Awaiting it from the game thread itself deadlocks nothing,
    /// since the continuation is queued, but it waits a frame for a value that was already in reach.
    /// </remarks>
    public static Task<T> RunOnGameThreadAsync<T>(this ScriptSystem script, Func<T> function)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(function);

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        script.AddTask(() =>
        {
            try
            {
                completion.SetResult(function());
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }

            return Task.CompletedTask;
        });

        return completion.Task;
    }

    /// <summary>
    /// Cancels all <see cref="MicroThread"/> and clears the <paramref name="microThreads"/> collection.
    /// </summary>
    /// <param name="microThreads">A collection of <see cref="MicroThread"/> to cancel.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="microThreads"/> is <see langword="null"/>.</exception>
    public static void CancelAll(this ICollection<MicroThread> microThreads)
    {
        ArgumentNullException.ThrowIfNull(microThreads);

        foreach (var thread in microThreads)
        {
            thread.Cancel();
        }

        microThreads.Clear();
    }

    /// <summary>
    /// Counts <paramref name="seconds"/> of game time, frame by frame, calling <paramref name="action"/> with the
    /// running total when there is one. <paramref name="warped"/> counts <see cref="GameTime.WarpElapsed"/>, otherwise
    /// <see cref="GameTime.Elapsed"/>.
    /// </summary>
    private static async Task RunFor(ScriptSystem script, float seconds, bool warped, Action<float>? action)
    {
        ArgumentNullException.ThrowIfNull(script);

        var game = GetGame(script);
        var elapsed = 0f;

        while (game.IsRunning && elapsed < seconds)
        {
            elapsed += (float)(warped ? game.UpdateTime.WarpElapsed : game.UpdateTime.Elapsed).TotalSeconds;

            action?.Invoke(elapsed);

            await script.NextFrame();
        }
    }

    /// <summary>
    /// Waits <paramref name="delay"/> on the game clock, giving up early when the game stops or the script
    /// behind <paramref name="watcher"/> leaves the scene.
    /// </summary>
    private static async Task WaitFor(this ScriptSystem scriptSystem, TimeSpan delay, ScriptDelegateWatcher watcher)
    {
        ArgumentNullException.ThrowIfNull(scriptSystem);
        ThrowIfNotPositive(delay);

        var game = GetGame(scriptSystem);

        while (game.IsRunning && watcher.IsActive && delay >= TimeSpan.Zero)
        {
            delay -= game.UpdateTime.Elapsed;

            await scriptSystem.NextFrame();
        }
    }

    /// <summary>
    /// Polls <paramref name="receiver"/> every frame and runs <paramref name="invoke"/> on what arrives, until the game
    /// stops or the script that owns <paramref name="owner"/> leaves the scene.
    /// </summary>
    private static MicroThread AddOnEvent<T>(this ScriptSystem scriptSystem, EventReceiver<T> receiver, Delegate owner, Func<T, Task> invoke, long priority)
    {
        ArgumentNullException.ThrowIfNull(receiver);

        return scriptSystem.AddWatched(owner, async (game, watcher) =>
        {
            while (game.IsRunning && watcher.IsActive)
            {
                if (receiver.TryReceive(out var data))
                    await invoke(data);

                await scriptSystem.NextFrame();
            }
        }, priority);
    }

    /// <summary>Runs <paramref name="invoke"/> once, after <paramref name="delay"/>, unless the game or the owning script stopped first.</summary>
    private static MicroThread AddDelayed(this ScriptSystem scriptSystem, Delegate owner, Func<Task> invoke, TimeSpan delay, long priority)
    {
        ThrowIfNotPositive(delay);

        return scriptSystem.AddWatched(owner, async (game, watcher) =>
        {
            await scriptSystem.WaitFor(delay, watcher);

            if (game.IsRunning && watcher.IsActive)
                await invoke();
        }, priority);
    }

    /// <summary>Runs <paramref name="invoke"/> after <paramref name="delay"/>, then every <paramref name="repeatEvery"/>, on the game clock.</summary>
    private static MicroThread AddRepeating(this ScriptSystem scriptSystem, Delegate owner, Func<Task> invoke, TimeSpan delay, TimeSpan repeatEvery, long priority)
    {
        ThrowIfNotPositive(delay);
        ThrowIfNotPositive(repeatEvery);

        return scriptSystem.AddWatched(owner, async (game, watcher) =>
        {
            var elapsed = TimeSpan.Zero;

            while (game.IsRunning && watcher.IsActive)
            {
                elapsed += game.UpdateTime.Elapsed;

                if (elapsed >= delay)
                {
                    elapsed -= delay;
                    delay = repeatEvery;

                    await invoke();
                }

                await scriptSystem.NextFrame();
            }
        }, priority);
    }

    /// <summary>
    /// Schedules <paramref name="body"/> with the game and a watcher on <paramref name="owner"/>: the delegate the caller
    /// passed, so that a <see cref="ScriptComponent"/> instance method still stops with its script after being wrapped.
    /// </summary>
    private static MicroThread AddWatched(this ScriptSystem scriptSystem, Delegate owner, Func<GameBase, ScriptDelegateWatcher, Task> body, long priority)
    {
        ArgumentNullException.ThrowIfNull(scriptSystem);
        ArgumentNullException.ThrowIfNull(owner);

        var game = GetGame(scriptSystem);
        var watcher = new ScriptDelegateWatcher(owner);

        return scriptSystem.AddTask(() => body(game, watcher), priority);
    }

    private static void ThrowIfNotPositive(TimeSpan value, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(name, "Must be greater than zero.");
    }

    /// <summary>
    /// Returns the game the <paramref name="scriptSystem"/> belongs to.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// If the <paramref name="scriptSystem"/> has no game, which Stride only allows in a mock environment.
    /// </exception>
    /// <remarks>
    /// Typed as the concrete <see cref="GameBase"/> rather than <see cref="IGame"/> because that is what
    /// the script system already holds; every caller here is on a per-frame path, so the
    /// interface dispatch this avoids is worth more than the abstraction on a private helper.
    /// </remarks>
    private static GameBase GetGame(ScriptSystem scriptSystem) =>
        scriptSystem.Game ?? throw new InvalidOperationException($"The {nameof(ScriptSystem)} is not attached to a game.");

    /// <summary>
    /// Remembers which <see cref="ScriptComponent"/>, if any, a delegate belongs to, so a scheduled callback can
    /// stop once that script has left the scene instead of running against a dead entity. The default value
    /// watches nothing and is always active.
    /// </summary>
    private readonly record struct ScriptDelegateWatcher
    {
        private readonly ScriptComponent? _script;

        internal ScriptDelegateWatcher(Delegate @delegate)
        {
            var invocationList = @delegate.GetInvocationList();

            _script = invocationList.Length == 1 ? invocationList[0].Target as ScriptComponent : null;
        }

        /// <summary>
        /// <see langword="true"/> while the owning script is still in a scene, or always when the delegate
        /// did not come from a script.
        /// </summary>
        internal bool IsActive => _script is null || (_script.Entity != null && _script.SceneSystem?.SceneInstance?.Contains(_script.Entity) == true);
    }
}
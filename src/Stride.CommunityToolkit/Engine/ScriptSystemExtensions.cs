using Stride.Engine.Processors;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides extension methods for the <see cref="ScriptSystem"/> to facilitate time-based operations, including delays and frame-based executions.
/// These extensions are useful for managing time in game logic, such as delaying actions or executing logic over a period of time.
/// </summary>
/// <example>
/// <para>
/// Example 1: Delaying an action for 2 seconds in game time (affected by time warp):
/// </para>
/// <code>
/// await scriptSystem.DelayWarped(2.0f);
/// // Action will be delayed for 2 in-game seconds, accounting for any time warp factors.
/// </code>
/// <para>
/// Example 2: Running a continuous action for 5 seconds of real time (unaffected by time warp):
/// </para>
/// <code>
/// await scriptSystem.ExecuteInTime(5.0f, elapsed =>
/// {
///     // Perform action based on the elapsed time in real seconds.
///     DebugText.Print($"Time elapsed: {elapsed} seconds");
/// });
/// </code>
/// <para>
/// Example 3: Delaying an action for 3 real-time seconds (unaffected by time warp):
/// </para>
/// <code>
/// await scriptSystem.Delay(3.0f);
/// // Action will be delayed for exactly 3 real seconds.
/// </code>
/// </example>
/// <remarks>
/// These extensions allow you to control how game logic interacts with time, whether you need frame-based operations or time delays.
/// The methods are useful for both real-time and in-game time-based operations.
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
    public static async Task DelayWarped(this ScriptSystem script, float seconds)
    {
        float t = 0f;
        while (script.Game.IsRunning && t < seconds)
        {
            t += (float)script.Game.UpdateTime.WarpElapsed.TotalSeconds;
            await script.NextFrame();
        }
    }

    /// <summary>
    /// Waits for a specified amount of real time (not accounting for any time warp factors).
    /// </summary>
    /// <param name="script">The <see cref="ScriptSystem"/> instance used to execute the delay.</param>
    /// <param name="seconds">The number of seconds to delay execution.</param>
    /// <returns>A <see cref="Task"/> representing the delay.</returns>
    /// <remarks>This delay operates in real time without considering the game's time warp.</remarks>
    public static async Task Delay(this ScriptSystem script, float seconds) =>
        await Task.Delay((int)(seconds * 1000f));

    /// <summary>
    /// Executes an action every frame for a specified duration, adjusting for any time warp factors.
    /// </summary>
    /// <param name="script">The <see cref="ScriptSystem"/> instance used to execute the action.</param>
    /// <param name="seconds">The duration in seconds for which the action will be executed.</param>
    /// <param name="action">The action to perform on each frame, which receives the elapsed time as a parameter.</param>
    /// <returns>A <see cref="Task"/> representing the action execution.</returns>
    /// <remarks>This method accounts for the game's <c>WarpElapsed</c> time factor during execution.</remarks>
    public static async Task ExecuteInWarpedTime(this ScriptSystem script, float seconds, Action<float> action)
    {
        float t = 0f;
        while (script.Game.IsRunning && t < seconds)
        {
            t += (float)script.Game.UpdateTime.WarpElapsed.TotalSeconds;
            action?.Invoke(t);
            await script.NextFrame();
        }
    }

    /// <summary>
    /// Executes an action every frame for a specified duration, using real time (not accounting for any time warp factors).
    /// </summary>
    /// <param name="script">The <see cref="ScriptSystem"/> instance used to execute the action.</param>
    /// <param name="seconds">The duration in seconds for which the action will be executed.</param>
    /// <param name="action">The action to perform on each frame, which receives the elapsed time as a parameter.</param>
    /// <returns>A <see cref="Task"/> representing the action execution.</returns>
    /// <remarks>This method operates in real time, without considering the game's time warp.</remarks>
    public static async Task ExecuteInTime(this ScriptSystem script, float seconds, Action<float> action)
    {
        float t = 0f;
        while (script.Game.IsRunning && t < seconds)
        {
            t += (float)script.Game.UpdateTime.Elapsed.TotalSeconds;
            action?.Invoke(t);
            await script.NextFrame();
        }
    }
}
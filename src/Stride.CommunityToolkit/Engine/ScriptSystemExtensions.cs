using Stride.Engine.Processors;

namespace Stride.CommunityToolkit.Engine;

public static class ScriptSystemExtensions
{
    /// <summary>
    /// Waits for a specified amount of time while taking into account the Update Time factor.
    /// </summary>
    /// <param name="script">The script system.</param>
    /// <param name="seconds">The amount of time to be delayed.</param>
    /// <returns>A <see cref="Task"/> that represents the time delay.</returns>
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
    /// Waits for a specified amount of time.
    /// </summary>
    /// <param name="script">The script system.</param>
    /// <param name="seconds">The amount of time to be delayed.</param>
    /// <returns>A <see cref="Task"/> that represents the time delay.</returns>
    public static async Task Delay(this ScriptSystem script, float seconds) =>
        await Task.Delay((int)(seconds * 1000f));

    /// <summary>
    /// Continuously executes an action every frame during a specified amount of time while taking into account the Update Time factor.
    /// </summary>
    /// <param name="script">The script system.</param>
    /// <param name="seconds">The duration in seconds.</param>
    /// <param name="action">The action that will be executed every frame.</param>
    /// <returns>A <see cref="Task"/> that represents the execution.</returns>
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
    /// Continuously executes an action every frame during a specified amount of time.
    /// </summary>
    /// <param name="script">The script system.</param>
    /// <param name="seconds">The duration in seconds.</param>
    /// <param name="action">The action that will be executed every frame.</param>
    /// <returns>A <see cref="Task"/> that represents the execution.</returns>
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
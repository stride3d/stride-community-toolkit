using Stride.Core;
using Stride.Engine;
using Stride.Input;
using Stride.Profiling;

namespace Stride.CommunityToolkit.Scripts;

/// <summary>
/// Provides in-game profiling functionality, allowing the monitoring and analysis of game performance in real time.
/// This script facilitates the toggling of profiling, setting display preferences, and navigating through profiling data.
/// </summary>
/// <remarks>
/// This class provides keyboard shortcuts for toggling the profiler on/off with Shift + Ctrl + P,
/// changing the filtering mode with F1, altering the sorting mode with F2, navigating result pages with F3 and F4,
/// and adjusting the refresh interval with the plus and minus keys.
/// </remarks>
[Display("Game Profiler")]
[ComponentCategory("Debug")]
public class GameProfiler : AsyncScript
{
    /// <summary>
    /// Gets or sets whether profiling is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The color of the text displayed during profiling
    /// </summary>
    [Display(4, "Text color")]
    public Color TextColor { get; set; } = Color.LightGreen;

    /// <summary>
    /// The time between two refreshes of the profiling information in milliseconds.
    /// </summary>
    [Display(2, "Refresh interval (ms)")]
    public double RefreshTime { get; set; } = 500;

    /// <summary>
    /// Gets or sets the sorting mode of the profiling entries.
    /// </summary>
    [Display(1, "Sort by")]
    public GameProfilingSorting SortingMode { get; set; } = GameProfilingSorting.ByTime;

    /// <summary>
    /// Gets or sets which profiling results are displayed: frame rate, CPU events or GPU events.
    /// </summary>
    [Display(0, "Filter")]
    public GameProfilingResults FilteringMode { get; set; } = GameProfilingResults.Fps;

    /// <summary>
    /// Gets or sets the current profiling result page to display.
    /// </summary>
    [Display(3, "Display page")]
    public uint ResultPage { get; set; } = 1;

    /// <summary>
    /// Main asynchronous loop that applies user input to configure and display profiling information.
    /// </summary>
    public override async Task Execute()
    {
        if (Enabled)
            GameProfiler.EnableProfiling();

        while (Game.IsRunning)
        {
            GameProfiler.TextColor = TextColor;
            GameProfiler.RefreshTime = RefreshTime;
            GameProfiler.SortingMode = SortingMode;
            GameProfiler.FilteringMode = FilteringMode;
            GameProfiler.CurrentResultPage = ResultPage;
            ResultPage = GameProfiler.CurrentResultPage;

            if (Input.IsKeyDown(Keys.LeftShift) && Input.IsKeyDown(Keys.LeftCtrl) && Input.IsKeyReleased(Keys.P))
            {
                Enabled = !Enabled;

                if (Enabled)
                {
                    GameProfiler.EnableProfiling();
                }
                else
                {
                    GameProfiler.DisableProfiling();
                }
            }

            if (Enabled)
            {
                // F1 and F2 cycle the filter and the sort order
                if (Input.IsKeyPressed(Keys.F1)) FilteringMode = Next(FilteringMode);
                if (Input.IsKeyPressed(Keys.F2)) SortingMode = Next(SortingMode);

                // F3 and F4 page back and forward; 1 to 5 jump to that page
                if (Input.IsKeyPressed(Keys.F3)) ResultPage = Math.Max(1u, ResultPage - 1);
                if (Input.IsKeyPressed(Keys.F4)) ResultPage++;

                for (var page = 1; page <= 5; page++)
                {
                    if (Input.IsKeyPressed(Keys.D0 + page)) ResultPage = (uint)page;
                }

                // Minus refreshes half as often, plus twice as often
                if (Input.IsKeyPressed(Keys.Subtract) || Input.IsKeyPressed(Keys.OemMinus)) RefreshTime = Math.Min(RefreshTime * 2, 10000);
                if (Input.IsKeyPressed(Keys.Add) || Input.IsKeyPressed(Keys.OemPlus)) RefreshTime = Math.Max(RefreshTime / 2, 100);
            }

            await Script.NextFrame();
        }
    }

    /// <summary>The value after <paramref name="value"/> in its enum, wrapping round to the first.</summary>
    private static T Next<T>(T value) where T : struct, Enum
    {
        var values = Enum.GetValues<T>();

        return values[(Array.IndexOf(values, value) + 1) % values.Length];
    }
}
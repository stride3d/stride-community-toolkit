using Stride.CommunityToolkit.Input;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Engine;
using Stride.Input;
using Stride.Profiling;

namespace Stride.CommunityToolkit.Scripts;

/// <summary>
/// Provides in-game profiling functionality, allowing the monitoring and analysis of game performance in real time.
/// This script facilitates the toggling of profiling, setting display preferences, and navigating through profiling data.
/// </summary>
/// <remarks>
/// <para>
/// Every key is chorded with Ctrl, so the plain F2, F3 and F4 stay with the camera help and the
/// <see cref="Rendering.Overlay.TextOverlay"/>, which are on screen in the same scenes.
/// <list type="bullet">
///   <item><description>Shift + Ctrl + P toggles the profiler on and off.</description></item>
///   <item><description>Ctrl + F1 changes the filter: frame rate, CPU events or GPU events.</description></item>
///   <item><description>Ctrl + F2 changes the sort order.</description></item>
///   <item><description>Ctrl + F3 and Ctrl + F4 page back and forward; Ctrl + 1 to Ctrl + 5 jump to a page.</description></item>
///   <item><description>Ctrl + minus and Ctrl + plus halve and double the refresh rate.</description></item>
///   <item><description><see cref="HelpToggleKey"/>, F1, expands this list on the <see cref="TextOverlay"/> with the
///   current values; the list's title names the chord last used, for a few seconds.</description></item>
/// </list>
/// </para>
/// </remarks>
[DataContract(nameof(GameProfiler))]
[Display("Game Profiler")]
[ComponentCategory("Debug")]
public class GameProfiler : AsyncScript
{
    /// <summary>How long the title names the chord last used, in seconds.</summary>
    private const double FeedbackSeconds = 3;

    private const string HelpTitle = "Profiler";

    private TextOverlay? _overlay;
    private TextOverlaySection? _help;
    private double _feedbackUntil;

    /// <summary>
    /// Gets or sets whether profiling is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the key that expands and collapses the profiler's key list on the <see cref="TextOverlay"/>.
    /// Defaults to <see cref="Keys.F1"/>. Must be set before the script starts.
    /// </summary>
    [DataMemberIgnore]
    public Keys HelpToggleKey { get; set; } = Keys.F1;

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

        // The key list, collapsed to its title, which doubles as the place a chord is acknowledged
        _overlay = TextOverlay.GetOrCreate(Game);
        _help = _overlay.AddCollapsibleSection("Profiler", HelpTitle, HelpToggleKey, HelpLines, collapsed: true);

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

            // Every key below is chorded with Ctrl: the plain F2 belongs to the camera help, F3 and F4 to the text
            // overlay, and both are on screen in the same scenes as the profiler
            if (Enabled && Input.IsCtrlDown())
            {
                // Ctrl+F1 and Ctrl+F2 cycle the filter and the sort order
                if (Input.IsKeyPressed(Keys.F1)) Acknowledge("Ctrl+F1", $"filter {FilteringMode = Next(FilteringMode)}");
                if (Input.IsKeyPressed(Keys.F2)) Acknowledge("Ctrl+F2", $"sort {SortingMode = Next(SortingMode)}");

                // Ctrl+F3 and Ctrl+F4 page back and forward; Ctrl+1 to Ctrl+5 jump to that page
                if (Input.IsKeyPressed(Keys.F3)) Acknowledge("Ctrl+F3", $"page {ResultPage = Math.Max(1u, ResultPage - 1)}");
                if (Input.IsKeyPressed(Keys.F4)) Acknowledge("Ctrl+F4", $"page {++ResultPage}");

                for (var page = 1; page <= 5; page++)
                {
                    if (Input.IsKeyPressed(Keys.D0 + page)) Acknowledge($"Ctrl+{page}", $"page {ResultPage = (uint)page}");
                }

                // Ctrl+minus refreshes half as often, Ctrl+plus twice as often
                if (Input.IsKeyPressed(Keys.Subtract) || Input.IsKeyPressed(Keys.OemMinus)) Acknowledge("Ctrl+-", $"refresh every {RefreshTime = Math.Min(RefreshTime * 2, 10000):0} ms");
                if (Input.IsKeyPressed(Keys.Add) || Input.IsKeyPressed(Keys.OemPlus)) Acknowledge("Ctrl++", $"refresh every {RefreshTime = Math.Max(RefreshTime / 2, 100):0} ms");
            }

            _help.Enabled = Enabled;

            if (_feedbackUntil > 0 && Game.UpdateTime.Total.TotalSeconds >= _feedbackUntil)
            {
                _help.Title = HelpTitle;
                _feedbackUntil = 0;
            }

            await Script.NextFrame();
        }
    }

    /// <inheritdoc />
    public override void Cancel()
    {
        if (_help is not null) _overlay?.RemoveSection(_help);

        _help = null;
    }

    // The title says what the chord did, for a moment: the profiler's own text has no line of ours to say it on
    private void Acknowledge(string chord, string effect)
    {
        _help!.Title = $"{HelpTitle} - {chord}: {effect}";
        _feedbackUntil = Game.UpdateTime.Total.TotalSeconds + FeedbackSeconds;
    }

    private IReadOnlyList<TextElement> HelpLines() =>
    [
        new("Ctrl+Shift+P", "Profiler on and off", Color.Gold),
        new("Ctrl+F1", $"Filter: {FilteringMode}", Color.Gold),
        new("Ctrl+F2", $"Sort: {SortingMode}", Color.Gold),
        new(["Ctrl+F3", "Ctrl+F4"], $"Page back and forward: page {ResultPage}", Color.Gold),
        new("Ctrl+1 to 5", "Jump to a page", Color.Gold),
        new(["Ctrl+-", "Ctrl++"], $"Refresh slower and faster: {RefreshTime:0} ms", Color.Gold),
    ];

    /// <summary>The value after <paramref name="value"/> in its enum, wrapping round to the first.</summary>
    private static T Next<T>(T value) where T : struct, Enum
    {
        var values = Enum.GetValues<T>();

        return values[(Array.IndexOf(values, value) + 1) % values.Length];
    }
}
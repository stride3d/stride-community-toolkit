using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Stride.CommunityToolkit.Examples.Core;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Stride.CommunityToolkit.Examples.Launcher;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<ExampleListItem> _visible = [];
    private readonly List<ExampleEntry> _all = [];
    private Process? _running;
    private CancellationTokenSource? _cts;

    private static readonly Regex GenericWarning = new(@"\bwarning\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ShaderWarning = new(@"\b(effect|shader|hlsl|fx|mixin|compiler)\b.*\bwarning\b|\bwarning\b.*\b(effect|shader|hlsl|fx|mixin|compiler)\b",
      RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public MainWindow()
    {
        InitializeComponent();

        ExamplesList.ItemsSource = _visible;

        LoadExamples();

        SearchBox.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == nameof(TextBox.Text))
                Filter(SearchBox.Text);
        };

        ExamplesList.SelectionChanged += (_, __) => ShowDetails(Current);

        BtnRun.Click += async (_, __) => await RunSelectedAsync();
        BtnStop.Click += (_, __) => StopRunning();
        BtnOpenFolder.Click += (_, __) => OpenFolder();
        BtnCopyCmd.Click += (_, __) => CopyCommand();
        BtnClearLog.Click += (_, __) => LogPanel.Text = string.Empty;
    }

    /// <summary>
    /// Fills the list from the generated manifest.
    /// </summary>
    /// <remarks>
    /// The manifest arrives already ordered by language, level and order, and already filtered by the
    /// generator to published examples, so nothing here re-sorts or re-filters it. A missing manifest is
    /// reported in the log rather than thrown, so the window still opens and says what is wrong.
    /// </remarks>
    private void LoadExamples()
    {
        try
        {
            _all.AddRange(ManifestLoader.Load());
        }
        catch (InvalidOperationException ex)
        {
            AppendLine($"❌ Could not load the examples manifest: {ex.Message}");

            return;
        }

        Filter(null);
    }

    private void Filter(string? text)
    {
        text = text?.Trim() ?? string.Empty;

        _visible.Clear();

        foreach (var entry in _all.Where(entry => Matches(entry, text)))
        {
            _visible.Add(new ExampleListItem(entry));
        }

        var notRunnable = _all.Count(entry => !entry.IsRunnable);

        CountLabel.Text = notRunnable > 0
            ? $"{_visible.Count} of {_all.Count} examples · {notRunnable} not found on disk"
            : $"{_visible.Count} of {_all.Count} examples";
    }

    private static bool Matches(ExampleEntry entry, string text)
    {
        if (text.Length == 0) return true;

        return entry.Title.Contains(text, StringComparison.OrdinalIgnoreCase)
            || entry.Slug.Contains(text, StringComparison.OrdinalIgnoreCase)
            || entry.ProjectName.Contains(text, StringComparison.OrdinalIgnoreCase)
            || entry.Level.Contains(text, StringComparison.OrdinalIgnoreCase)
            || (entry.Category?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false)
            || entry.Tags.Any(tag => tag.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    private ExampleEntry? Current => (ExamplesList.SelectedItem as ExampleListItem)?.Entry;

    private void ShowDetails(ExampleEntry? entry)
    {
        if (entry is null)
        {
            DetailTitle.Text = "Select an example";
            DetailMeta.Text = string.Empty;
            DetailDescription.Text = string.Empty;
            DetailTags.Text = string.Empty;

            return;
        }

        var meta = new List<string> { entry.Level };

        if (entry.Category is { Length: > 0 } category) meta.Add(category);
        if (entry.Complexity is { } complexity) meta.Add($"complexity {complexity}/5");
        if (entry.LanguageLabel.Length > 0) meta.Add(entry.LanguageLabel);
        if (entry.IsFileBased) meta.Add("file-based app");

        meta.Add(entry.ProjectName);

        DetailTitle.Text = entry.Title;
        DetailMeta.Text = string.Join("  ·  ", meta);
        DetailDescription.Text = entry.Description ?? string.Empty;
        DetailTags.Text = entry.Tags.Count > 0 ? string.Join("  ", entry.Tags.Select(tag => $"#{tag}")) : string.Empty;
    }

    private async Task RunSelectedAsync()
    {
        var entry = Current;

        if (entry is null)
        {
            AppendLine("⚠️ Please select an example to run.");
            return;
        }

        if (!entry.IsRunnable)
        {
            AppendLine($"❌ Nothing to run for {entry.ProjectName}: no project or source file found on disk.");
            return;
        }

        StopRunning();

        LogPanel.Text = string.Empty;
        AppendLine($"▶️ Starting: {entry.Title}");
        AppendLine($"📁 {entry.RunTarget}");
        AppendLine(new string('-', 80));

        _cts = new CancellationTokenSource();

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = entry.ProcessArguments,
            WorkingDirectory = entry.Directory ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        _running = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var process = _running;

        try
        {
            process.Start();
            var readOut = Task.Run(() => ReadLinesAsync(process.StandardOutput, isError: false, _cts.Token));
            var readErr = Task.Run(() => ReadLinesAsync(process.StandardError, isError: true, _cts.Token));
            await Task.WhenAll(readOut, readErr);

            process.WaitForExit();
            var exitCode = process.ExitCode;
            AppendLine($"✅ Process exited with code: {exitCode}");
        }
        catch (Exception ex)
        {
            AppendLine($"❌ Error: {ex.Message}");
            StopRunning();
        }
    }

    private void StopRunning()
    {
        try
        {
            _cts?.Cancel();
            if (_running is { HasExited: false })
            {
                AppendLine("⏹️ Stopping process...");
                _running.Kill(entireProcessTree: true);
                _running.WaitForExit(2000);
                AppendLine("✅ Process stopped.");
            }
        }
        catch (Exception ex)
        {
            AppendLine($"⚠️ Error stopping process: {ex.Message}");
        }
        finally
        {
            _running?.Dispose();
            _running = null;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task ReadLinesAsync(StreamReader reader, bool isError, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            string? line;
            try { line = await reader.ReadLineAsync(ct); }
            catch { break; }
            if (line is null) break;

            if (ShouldSuppress(line)) continue;

            AppendLine(line, isError);
        }
    }

    private static bool ShouldSuppress(string line)
    {
        var showAll = string.Equals(Environment.GetEnvironmentVariable("SHOW_WARNINGS"), "1", StringComparison.OrdinalIgnoreCase)
|| string.Equals(Environment.GetEnvironmentVariable("SHOW_WARNINGS"), "true", StringComparison.OrdinalIgnoreCase);
        if (showAll) return false;

        if (!GenericWarning.IsMatch(line)) return false;
        return ShaderWarning.IsMatch(line);
    }

    private void AppendLine(string text, bool isError = false)
    {
        Dispatcher.UIThread.Post(() =>
       {
           var sb = new StringBuilder(LogPanel.Text ?? string.Empty);
           if (sb.Length > 0) sb.AppendLine();
           if (isError) sb.Append("❌ ");
           sb.Append(text);
           LogPanel.Text = sb.ToString();
       });
    }

    private void OpenFolder()
    {
        var entry = Current;
        if (entry is null) return;

        var dir = entry.Directory;

        if (dir is null || !Directory.Exists(dir))
        {
            AppendLine("⚠️ Folder not found.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            AppendLine($"❌ Error opening folder: {ex.Message}");
        }
    }

    private void CopyCommand()
    {
        var entry = Current;
        if (entry is null) return;

        var cmd = entry.CommandLine;

        try
        {
            var item = new DataTransferItem();
            item.SetText(cmd);

            var data = new DataTransfer();
            data.Add(item);

            Clipboard?.SetDataAsync(data);
            AppendLine($"📋 Copied to clipboard: {cmd}");
        }
        catch (Exception ex)
        {
            AppendLine($"❌ Error copying to clipboard: {ex.Message}");
        }
    }

    /// <summary>
    /// One row in the list.
    /// </summary>
    /// <remarks>
    /// The entry is stored in a property rather than captured from the primary constructor parameter,
    /// which is what the previous version did and what CS9124 warned about.
    /// </remarks>
    private sealed class ExampleListItem(ExampleEntry entry)
    {
        public ExampleEntry Entry { get; } = entry;

        public override string ToString()
        {
            var language = Entry.LanguageLabel.Length > 0 ? $" [{Entry.LanguageLabel}]" : string.Empty;
            var warning = Entry.IsRunnable ? string.Empty : "  ⚠ not found";

            return $"{Entry.Level,-16}{Entry.Title}{language}  ({Entry.ProjectName}){warning}";
        }
    }
}

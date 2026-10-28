using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Stride.CommunityToolkit.Examples.Core;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace Stride.CommunityToolkit.Examples.Launcher;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<ExampleListItem> _visible = [];
    private readonly List<ExampleEntry> _all = [];
    private Process? _running;
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        InitializeComponent();

        ExamplesList.ItemsSource = _visible;

        LoadExamples();

        SearchBox.PropertyChanged += (_, e) =>
        {
            if (e.Property.Name == nameof(TextBox.Text)) Filter(SearchBox.Text);
        };

        ExamplesList.SelectionChanged += (_, _) => ShowDetails(Current);

        BtnRun.Click += async (_, _) => await RunSelectedAsync();
        BtnStop.Click += (_, _) => StopRunning();
        BtnOpenFolder.Click += (_, _) => OpenFolder();
        BtnCopyCmd.Click += (_, _) => CopyCommand();
        BtnClearLog.Click += (_, _) => LogPanel.Text = string.Empty;
    }

    private ExampleEntry? Current => (ExamplesList.SelectedItem as ExampleListItem)?.Entry;

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

        foreach (var entry in _all)
        {
            if (Matches(entry, text)) _visible.Add(new ExampleListItem(entry));
        }

        var notRunnable = _all.Count(entry => !entry.IsRunnable);

        CountLabel.Text = notRunnable > 0
            ? $"{_visible.Count} of {_all.Count} examples · {notRunnable} not found on disk"
            : $"{_visible.Count} of {_all.Count} examples";
    }

    private static bool Matches(ExampleEntry entry, string text)
        => text.Length == 0
            || entry.Title.Contains(text, StringComparison.OrdinalIgnoreCase)
            || entry.Slug.Contains(text, StringComparison.OrdinalIgnoreCase)
            || entry.ProjectName.Contains(text, StringComparison.OrdinalIgnoreCase)
            || entry.Level.Contains(text, StringComparison.OrdinalIgnoreCase)
            || entry.Category?.Contains(text, StringComparison.OrdinalIgnoreCase) == true
            || entry.Tags.Any(tag => tag.Contains(text, StringComparison.OrdinalIgnoreCase));

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
        if (Current is not { } entry)
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

        var token = _cts.Token;
        var process = _running = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = entry.ProcessArguments,
                WorkingDirectory = entry.Directory ?? Environment.CurrentDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        try
        {
            process.Start();

            await Task.WhenAll(
                Task.Run(() => ReadLinesAsync(process.StandardOutput, isError: false, token)),
                Task.Run(() => ReadLinesAsync(process.StandardError, isError: true, token)));

            process.WaitForExit();
            AppendLine($"✅ Process exited with code: {process.ExitCode}");
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

            try { line = await reader.ReadLineAsync(ct); } catch { break; }

            if (line is null) break;

            if (!OutputFilter.IsSuppressed(line)) AppendLine(line, isError);
        }
    }

    private void AppendLine(string text, bool isError = false)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var log = new StringBuilder(LogPanel.Text ?? string.Empty);

            if (log.Length > 0) log.AppendLine();
            if (isError) log.Append("❌ ");

            log.Append(text);
            LogPanel.Text = log.ToString();
        });
    }

    private void OpenFolder()
    {
        if (Current is not { } entry) return;

        if (entry.Directory is not { } dir || !Directory.Exists(dir))
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
        if (Current is not { } entry) return;

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
    private sealed class ExampleListItem(ExampleEntry entry)
    {
        internal ExampleEntry Entry { get; } = entry;

        public override string ToString()
        {
            var language = Entry.LanguageLabel.Length > 0 ? $" [{Entry.LanguageLabel}]" : string.Empty;
            var warning = Entry.IsRunnable ? string.Empty : "  ⚠ not found";

            return $"{Entry.Level,-16}{Entry.Title}{language}  ({Entry.ProjectName}){warning}";
        }
    }
}
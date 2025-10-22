using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Stride.CommunityToolkit.Examples.Core;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Stride.CommunityToolkit.Examples.Launcher;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<ExampleListItem> _examples = [];
    private readonly List<ExampleProjectMeta> _all = [];
    private Process? _running;
    private CancellationTokenSource? _cts;

    private static readonly Regex GenericWarning = new(@"\bwarning\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ShaderWarning = new(@"\b(effect|shader|hlsl|fx|mixin|compiler)\b.*\bwarning\b|\bwarning\b.*\b(effect|shader|hlsl|fx|mixin|compiler)\b",
      RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public MainWindow()
    {
 InitializeComponent();
  
        ExamplesList.ItemsSource = _examples;

 LoadExamples();

        SearchBox.PropertyChanged += (s, e) =>
        {
     if (e.Property.Name == nameof(TextBox.Text))
     Filter(SearchBox.Text);
 };
        
     BtnRun.Click += async (_, __) => await RunSelectedAsync();
        BtnStop.Click += (_, __) => StopRunning();
      BtnOpenFolder.Click += (_, __) => OpenFolder();
      BtnCopyCmd.Click += (_, __) => CopyCommand();
        BtnClearLog.Click += (_, __) => LogPanel.Text = string.Empty;
    }

    private void LoadExamples()
    {
var provider = new ExampleProvider();
        var examples = provider.GetExamples()
      .Where(e => e.Title != Constants.Quit && e.Title != Constants.Clear)
       .Select(e => new ExampleProjectMeta(e.Id, e.Title, GetProjectPath(e), GetOrder(e), e.Category))
 .ToList();

        _all.AddRange(examples);
        foreach (var e in _all)
       _examples.Add(new ExampleListItem(e));
    }

    private static string GetProjectPath(Example example)
    {
  var baseDir = AppDomain.CurrentDomain.BaseDirectory;
  var examplesRoot = FindExamplesRoot(baseDir) ?? Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "examples", "code-only"));
     
        var projectName = example.ProjectName ?? example.Title.Replace(" ", "_");
        var patterns = new[] { "*.csproj", "*.fsproj", "*.vbproj" };
        
  foreach (var pattern in patterns)
   {
 var files = Directory.EnumerateFiles(examplesRoot, pattern, SearchOption.AllDirectories)
       .Where(f => Path.GetFileNameWithoutExtension(f).Contains(projectName, StringComparison.OrdinalIgnoreCase))
     .ToList();
        
   if (files.Count > 0) return files[0];
        }

        return string.Empty;
    }

    private static string? FindExamplesRoot(string baseDir)
    {
  var dir = baseDir;
    for (int i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
     {
          var candidate = Path.Combine(dir, "examples", "code-only");
            if (Directory.Exists(candidate))
       return candidate;

  dir = Path.GetDirectoryName(dir?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }
   return null;
    }

    private static int? GetOrder(Example example)
    {
        if (example.Category == Constants.BasicExample) return 1;
        if (example.Category == Constants.AdvanceExample) return 2;
    return 3;
    }

    private void Filter(string? text)
    {
  text ??= string.Empty;
        text = text.Trim();

    _examples.Clear();
 foreach (var e in _all)
     {
        if (text.Length == 0 ||
      e.Title.Contains(text, StringComparison.OrdinalIgnoreCase) ||
      e.Id.Contains(text, StringComparison.OrdinalIgnoreCase) ||
          (e.Category?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false))
{
       _examples.Add(new ExampleListItem(e));
  }
   }
    }

    private ExampleProjectMeta? Current
    {
        get
 {
    var item = ExamplesList.SelectedItem as ExampleListItem;
 return item?.Meta;
  }
    }

    private async Task RunSelectedAsync()
    {
    var meta = Current;
        if (meta is null)
        {
  AppendLine("⚠️ Please select an example to run.");
   return;
        }

        if (string.IsNullOrEmpty(meta.ProjectFile) || !File.Exists(meta.ProjectFile))
        {
            AppendLine($"❌ Project file not found: {meta.ProjectFile}");
      return;
        }

        StopRunning();

 LogPanel.Text = string.Empty;
  AppendLine($"▶️ Starting: {meta.Title}");
      AppendLine($"📁 Project: {meta.ProjectFile}");
AppendLine(new string('-', 80));

        _cts = new CancellationTokenSource();

        var psi = new ProcessStartInfo
        {
      FileName = "dotnet",
  Arguments = $"run --project \"{meta.ProjectFile}\"",
        WorkingDirectory = Path.GetDirectoryName(meta.ProjectFile) ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
     RedirectStandardError = true,
            CreateNoWindow = true
        };

        _running = new Process { StartInfo = psi, EnableRaisingEvents = true };

      try
        {
            _running.Start();
     var readOut = Task.Run(() => ReadLinesAsync(_running.StandardOutput, isError: false, _cts.Token));
            var readErr = Task.Run(() => ReadLinesAsync(_running.StandardError, isError: true, _cts.Token));
            await Task.WhenAll(readOut, readErr);

         _running.WaitForExit();
            var exitCode = _running.ExitCode;
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
   try { line = await reader.ReadLineAsync(); }
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
        var meta = Current;
 if (meta is null) return;
    var dir = Path.GetDirectoryName(meta.ProjectFile);
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
        var meta = Current;
    if (meta is null) return;
   var cmd = $"dotnet run --project \"{meta.ProjectFile}\"";
        
try
    {
         Clipboard?.SetTextAsync(cmd);
    AppendLine($"📋 Copied to clipboard: {cmd}");
}
   catch (Exception ex)
  {
       AppendLine($"❌ Error copying to clipboard: {ex.Message}");
  }
    }

    private class ExampleListItem(ExampleProjectMeta meta)
    {
        public ExampleProjectMeta Meta { get; } = meta;

   public override string ToString()
{
   var cat = !string.IsNullOrEmpty(meta.Category) ? $"[{meta.Category}] " : "";
       return $"{cat}{meta.Title} ({meta.Id})";
        }
    }
}

using System.Diagnostics;

namespace Stride.CommunityToolkit.Examples.Core;

/// <summary>
/// Builds the console menu from the manifest and runs the selected example.
/// </summary>
public class ExampleProvider
{
    private int _index;

    // Console state: consecutive blank lines are collapsed, and one directly after hidden warnings goes with them
    private readonly Lock _consoleLock = new();
    private bool _lastPrintedWasBlank;
    private bool _justSuppressed;

    /// <summary>
    /// Gets the menu: every launcher-visible example, then Quit and Clear.
    /// </summary>
    /// <returns>The menu entries, in manifest order.</returns>
    public List<Example> GetExamples()
    {
        var entries = ManifestLoader.Load();
        var list = new List<Example>(entries.Count + 2);

        foreach (var entry in entries) list.Add(new Example(GetIndex(), entry.Title, entry, () => Launch(entry)));

        list.Add(new Example("Q", Constants.Quit, null, () => Environment.Exit(0)));
        list.Add(new Example("C", Constants.Clear, null, Constants.SafeClear));

        return list;
    }

    /// <summary>
    /// Starts an example and streams its output into this console.
    /// </summary>
    private void Launch(ExampleEntry entry)
    {
        if (!entry.IsRunnable)
        {
            Console.WriteLine($"Cannot run {entry.ProjectName}: no project or source file found on disk.");

            return;
        }

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = entry.ProcessArguments,
            WorkingDirectory = entry.Directory ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        });

        if (process is null) return;

        // stdout and stderr are read concurrently, each on its own thread, and the process is disposed once
        // both have drained, so a fast example's last lines are not lost to an early exit
        var drained = Task.WhenAll(
            Task.Run(() => ReadStreamLines(process.StandardOutput, isError: false)),
            Task.Run(() => ReadStreamLines(process.StandardError, isError: true)));

        _ = drained.ContinueWith(_ => process.Dispose(), TaskScheduler.Default);
    }

    private void ReadStreamLines(StreamReader reader, bool isError)
    {
        while (reader.ReadLine() is { } line)
        {
            if (OutputFilter.IsSuppressed(line))
            {
                _justSuppressed = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(line)) WriteBlankLine();
            else WriteLine(line, isError);
        }
    }

    private void WriteBlankLine()
    {
        // A blank line directly after hidden warnings belongs to them
        if (_justSuppressed) return;

        lock (_consoleLock)
        {
            if (_lastPrintedWasBlank) return;

            Console.WriteLine();
            _lastPrintedWasBlank = true;
        }
    }

    private void WriteLine(string line, bool isError)
    {
        lock (_consoleLock)
        {
            _lastPrintedWasBlank = false;
            _justSuppressed = false;

            if (OutputFilter.IsWarning(line)) WriteColoured(Console.Out, ConsoleColor.Yellow, line);
            else if (isError) WriteColoured(Console.Error, ConsoleColor.Red, line);
            else Console.WriteLine(line);
        }
    }

    private static void WriteColoured(TextWriter writer, ConsoleColor color, string line)
    {
        var previous = Console.ForegroundColor;

        Console.ForegroundColor = color;
        writer.WriteLine(line);
        Console.ForegroundColor = previous;
    }

    private string GetIndex() => (++_index).ToString();
}
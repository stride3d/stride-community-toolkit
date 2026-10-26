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

        process.EnableRaisingEvents = true;
        process.Exited += (_, _) =>
        {
            try { process.Dispose(); } catch { /* ignore */ }
        };

        // stdout and stderr are read concurrently, each on its own thread
        _ = Task.Run(() => ReadStreamLines(process.StandardOutput, isError: false));
        _ = Task.Run(() => ReadStreamLines(process.StandardError, isError: true));
    }

    private void ReadStreamLines(StreamReader reader, bool isError)
    {
        while (true)
        {
            string? line;

            // The process may be disposed from its Exited handler while a read is pending
            try { line = reader.ReadLine(); } catch { break; }

            if (line is null) break;

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

    private string GetIndex() => Interlocked.Increment(ref _index).ToString();
}
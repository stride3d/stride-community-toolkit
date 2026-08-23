using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Stride.CommunityToolkit.Examples.Core;

/// <summary>
/// Builds the console menu from the manifest and runs the selected example.
/// </summary>
public partial class ExampleProvider
{
    private int _index;

    // Warning filtering configuration
    private const bool FilterWarnings = true;

    [GeneratedRegex(@"\bwarning\b", RegexOptions.IgnoreCase)]
    private static partial Regex GenericWarningPattern();

    [GeneratedRegex(@"\b(effect|shader|hlsl|fx|mixin|compiler)\b.*\bwarning\b|\bwarning\b.*\b(effect|shader|hlsl|fx|mixin|compiler)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ShaderWarningPattern();

    // Blank line handling
    private const bool CollapseConsecutiveBlankLines = true;
    private const bool RemoveBlankLinesAfterSuppressedBlock = true;

    private static bool BypassFiltering =>
        Environment.GetEnvironmentVariable("SHOW_WARNINGS") is string v &&
        (v.Equals("1") || v.Equals("true", StringComparison.OrdinalIgnoreCase));

    // Console state
    private readonly Lock _consoleLock = new();
    private bool _lastPrintedWasBlank;
    private bool _justSuppressed; // set when we suppressed at least one line since last printed real line

    /// <summary>
    /// Gets the menu: every launcher-visible example, then Quit and Clear.
    /// </summary>
    /// <returns>The menu entries, in manifest order.</returns>
    public List<Example> GetExamples()
    {
        var entries = ManifestLoader.Load();
        var list = new List<Example>(entries.Count + 2);

        foreach (var entry in entries)
        {
            list.Add(new Example(GetIndex(), entry.Title, entry, () => Launch(entry)));
        }

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

        var process = Process.Start(psi);

        if (process is null) return;

        process.EnableRaisingEvents = true;
        process.Exited += (_, __) =>
        {
            try { process.Dispose(); } catch { /* ignore */ }
        };

        _ = Task.Run(async () => await StreamProcessOutput(process));
    }

    private async Task StreamProcessOutput(Process process)
    {
        // Process stdout & stderr concurrently
        var stdout = Task.Run(() => ReadStreamLines(process.StandardOutput, isError: false));
        var stderr = Task.Run(() => ReadStreamLines(process.StandardError, isError: true));
        await Task.WhenAll(stdout, stderr);
    }

    private void ReadStreamLines(StreamReader reader, bool isError)
    {
        while (true)
        {
            string? line;
            try { line = reader.ReadLine(); }
            catch { break; }
            if (line is null) break;

            if (ShouldSuppress(line))
            {
                _justSuppressed = true;
                continue;
            }

            // Handle blank lines intelligently
            if (string.IsNullOrWhiteSpace(line))
            {
                // Remove blank line if it directly follows a suppressed block
                if (RemoveBlankLinesAfterSuppressedBlock && _justSuppressed)
                    continue;

                lock (_consoleLock)
                {
                    if (CollapseConsecutiveBlankLines && _lastPrintedWasBlank)
                        // skip additional blank line
                        continue;

                    Console.WriteLine();
                    _lastPrintedWasBlank = true;
                    _justSuppressed = false;
                }

                continue;
            }

            WriteLine(line, isError);
        }
    }

    private static bool ShouldSuppress(string line)
    {
        if (!FilterWarnings || BypassFiltering)
            return false;

        if (!GenericWarningPattern().IsMatch(line))
            return false; // Not a warning

        // Only suppress if it looks shader/effect related
        return ShaderWarningPattern().IsMatch(line);
    }

    private void WriteLine(string line, bool isError)
    {
        lock (_consoleLock)
        {
            _lastPrintedWasBlank = false;
            _justSuppressed = false;

            if (GenericWarningPattern().IsMatch(line))
            {
                var prev = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(line);
                Console.ForegroundColor = prev;
                return;
            }

            if (isError)
            {
                var prev = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(line);
                Console.ForegroundColor = prev;
                return;
            }

            Console.WriteLine(line);
        }
    }

    private string GetIndex() => Interlocked.Increment(ref _index).ToString();
}

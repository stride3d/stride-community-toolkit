using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Stride.CommunityToolkit.Examples.Core;

public partial class ExampleProvider
{
    private int _index;
    private readonly string _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

    // Configuration (adjust as desired)
    private const string ExamplesRootRelative = "..\\..\\..\\..\\..\\examples\\code-only";
    private const string ExampleTitleElement = "ExampleTitle";
    private const string ExampleOrderElement = "ExampleOrder";

    private static readonly string[] _projectPatterns = ["*.csproj", "*.fsproj", "*.vbproj"];
    private static readonly Regex _commentTitleRegex = new("//\\s*ExampleTitle\\s*:\\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Warning filtering configuration
    private const bool FilterWarnings = true;
    private static readonly Regex _genericWarningRegex = new(@"\bwarning\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex _shaderWarningRegex = new(@"\b(effect|shader|hlsl|fx|mixin|compiler)\b.*\bwarning\b|\bwarning\b.*\b(effect|shader|hlsl|fx|mixin|compiler)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Blank line handling
    private const bool CollapseConsecutiveBlankLines = true;
    private const bool RemoveBlankLinesAfterSuppressedBlock = true;

    private static bool BypassFiltering =>
        Environment.GetEnvironmentVariable("SHOW_WARNINGS") is string v &&
        (v.Equals("1") || v.Equals("true", StringComparison.OrdinalIgnoreCase));

    // Console state
    private readonly object _consoleLock = new();
    private bool _lastPrintedWasBlank;
    private bool _justSuppressed; // set when we suppressed at least one line since last printed real line

    public List<Example> GetExamples()
    {
        var exampleProjects = DiscoverExamples();

        var ordered = exampleProjects
            .OrderBy(m => m.Category)
            .OrderBy(m => m.Order.HasValue ? 0 : 1)
            .ThenBy(m => m.Order)
            .ThenBy(m => m.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var list = new List<Example>(ordered.Count + 1);

        foreach (var meta in ordered)
            list.Add(new Example(GetIndex(), meta.Title, () => LaunchWithDotNet(meta.ProjectFile)));

        list.Add(new Example("Q", "Quit", () => Environment.Exit(0)));

        return list;
    }

    private IEnumerable<ExampleProjectMeta> DiscoverExamples()
    {
        var root = Path.GetFullPath(Path.Combine(_baseDirectory, ExamplesRootRelative));
        if (!Directory.Exists(root)) yield break;

        foreach (var pattern in _projectPatterns)
            foreach (var project in Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories))
            {
                ExampleProjectMeta? meta = null;

                try { meta = CreateMetaFromProject(project); }
                catch { /* ignore */ }

                if (meta is not null)
                    yield return meta;
            }
    }

    private ExampleProjectMeta? CreateMetaFromProject(string projectFile)
    {
        var doc = XDocument.Load(projectFile, LoadOptions.None);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid project XML.");
        var ns = root.Name.Namespace;

        var explicitTitle = GetProp(ExampleTitleElement) ?? GetProp("Title");
        var assemblyName = GetProp("AssemblyName");
        var category = GetProp("ExampleCategory");
        var orderRaw = GetProp(ExampleOrderElement);
        var enabledRaw = GetProp("ExampleEnabled");

        bool? enabled = bool.TryParse(enabledRaw, out var e) && e;

        if (enabled == false && enabledRaw != null) return null;

        int? order = int.TryParse(orderRaw, out var o) ? o : null;

        string title = explicitTitle
                       ?? TryParseProgramCommentTitle(projectFile)
                       ?? assemblyName
                       ?? Path.GetFileNameWithoutExtension(projectFile);

        var id = assemblyName ?? Path.GetFileNameWithoutExtension(projectFile);

        return new ExampleProjectMeta(id, title, projectFile, order, category);

        string? GetProp(string name) =>
            root.Elements(ns + "PropertyGroup")
                .Elements()
                .FirstOrDefault(e => e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase))
                ?.Value?.Trim();
    }

    private string? TryParseProgramCommentTitle(string projectFile)
    {
        var dir = Path.GetDirectoryName(projectFile);
        if (dir is null) return null;

        var programFile = Directory.EnumerateFiles(dir, "Program.*", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (programFile is null) return null;

        try
        {
            foreach (var line in File.ReadLines(programFile))
            {
                var m = _commentTitleRegex.Match(line);
                if (m.Success)
                    return m.Groups[1].Value.Trim();
            }
        }
        catch { /* ignore */ }

        return null;
    }

    private void LaunchWithDotNet(string projectFile)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectFile}\"",
            WorkingDirectory = Path.GetDirectoryName(projectFile) ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var process = Process.Start(psi);

        if (process == null) return;

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
                        return;

                    Console.WriteLine();
                    _lastPrintedWasBlank = true;
                    _justSuppressed = false;
                }

                continue;
            }

            WriteLine(line, isError);
        }
    }

    private bool ShouldSuppress(string line)
    {
        if (!FilterWarnings || BypassFiltering)
            return false;

        if (!_genericWarningRegex.IsMatch(line))
            return false; // Not a warning

        // Only suppress if it looks shader/effect related
        return _shaderWarningRegex.IsMatch(line);
    }

    private void WriteLine(string line, bool isError)
    {
        lock (_consoleLock)
        {
            _lastPrintedWasBlank = false;
            _justSuppressed = false;

            if (_genericWarningRegex.IsMatch(line))
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
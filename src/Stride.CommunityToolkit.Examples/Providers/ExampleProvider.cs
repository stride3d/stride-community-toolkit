using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Stride.CommunityToolkit.Examples.Providers;

public class ExampleProvider
{
    private int _index;
    private readonly string _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

    // Configuration (adjust as desired)
    private const string ExamplesRootRelative = "..\\..\\..\\..\\..\\examples\\code-only";
    private static readonly string[] ProjectPatterns = ["*.csproj", "*.fsproj", "*.vbproj"];
    private const string ExampleTitleElement = "ExampleTitle";
    private const string ExampleOrderElement = "ExampleOrder";
    private static readonly Regex CommentTitleRegex = new("//\\s*ExampleTitle\\s*:\\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Warning filtering configuration
    private const bool FilterWarnings = true;                 // Master switch
    private const bool FilterOnlyShaderWarnings = true;       // If false, all warnings filtered
    private static readonly Regex GenericWarningRegex = new(@"\bwarning\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // Heuristic: shader/effect/compiler lines
    private static readonly Regex ShaderWarningRegex = new(@"\b(effect|shader|hlsl|fx|mixin|compiler)\b.*\bwarning\b|\bwarning\b.*\b(effect|shader|hlsl|fx|mixin|compiler)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static bool BypassFiltering =>
        Environment.GetEnvironmentVariable("SHOW_WARNINGS") is string v &&
        (v.Equals("1") || v.Equals("true", StringComparison.OrdinalIgnoreCase));

    private sealed record ExampleProjectMeta(
        string Id,
        string Title,
        string ProjectFile,
        int? Order,
        string? Category
    );

    public List<Example> GetExamples()
    {
        var metas = DiscoverExamples();

        var ordered = metas
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

        foreach (var pattern in ProjectPatterns)
        {
            foreach (var proj in Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories))
            {
                ExampleProjectMeta? meta = null;
                try { meta = CreateMetaFromProject(proj); }
                catch { /* ignore */ }
                if (meta is not null)
                    yield return meta;
            }
        }
    }

    private ExampleProjectMeta CreateMetaFromProject(string projectFile)
    {
        var doc = XDocument.Load(projectFile, LoadOptions.None);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid project XML.");
        var ns = root.Name.Namespace;

        string? GetProp(string name) =>
            root.Elements(ns + "PropertyGroup")
                .Elements()
                .FirstOrDefault(e => e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase))
                ?.Value?.Trim();

        var explicitTitle = GetProp(ExampleTitleElement) ?? GetProp("Title");
        var assemblyName = GetProp("AssemblyName");
        var category = GetProp("ExampleCategory");
        var orderRaw = GetProp(ExampleOrderElement);
        int? order = int.TryParse(orderRaw, out var o) ? o : null;

        string title = explicitTitle
                       ?? TryParseProgramCommentTitle(projectFile)
                       ?? assemblyName
                       ?? Path.GetFileNameWithoutExtension(projectFile);

        var id = assemblyName ?? Path.GetFileNameWithoutExtension(projectFile);
        return new ExampleProjectMeta(id, title, projectFile, order, category);
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
                var m = CommentTitleRegex.Match(line);
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
            try
            {
                line = reader.ReadLine();
            }
            catch
            {
                break;
            }

            if (line is null) break;

            if (ShouldSuppress(line))
                continue;

            WriteLine(line, isError);
        }
    }

    private bool ShouldSuppress(string line)
    {
        if (!FilterWarnings || BypassFiltering)
            return false;

        if (!GenericWarningRegex.IsMatch(line))
            return false; // Not a warning

        if (!FilterOnlyShaderWarnings)
            return true;

        // Only suppress if it looks shader/effect related
        return ShaderWarningRegex.IsMatch(line);
    }

    private static void WriteLine(string line, bool isError)
    {
        // Optional: colorize remaining warnings/errors
        if (GenericWarningRegex.IsMatch(line))
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

    private string GetIndex() => Interlocked.Increment(ref _index).ToString();
}
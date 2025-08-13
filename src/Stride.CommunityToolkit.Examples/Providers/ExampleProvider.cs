using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Stride.CommunityToolkit.Examples.Providers;

public class ExampleProvider
{
    private int _index;
    private readonly string _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

    // Configuration (adjust as desired)
    private const string ExamplesRootRelative = "..\\..\\..\\..\\..\\examples\\code-only"; // from launcher bin folder during dev
    private static readonly string[] ProjectPatterns = ["*.csproj", "*.fsproj", "*.vbproj"];
    private const string ExampleTitleElement = "ExampleTitle";
    private const string ExampleOrderElement = "ExampleOrder";
    private static readonly Regex CommentTitleRegex = new("//\\s*ExampleTitle\\s*:\\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

        // Sort: explicit order first, then title
        var ordered = metas
            .OrderBy(m => m.Order.HasValue ? 0 : 1)
            .ThenBy(m => m.Order)
            .ThenBy(m => m.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var list = new List<Example>(ordered.Count + 1);

        foreach (var meta in ordered)
        {
            list.Add(new Example(GetIndex(), meta.Title, () => LaunchWithDotNet(meta.ProjectFile)));
        }

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
                try
                {
                    meta = CreateMetaFromProject(proj);
                }
                catch
                {
                    // Swallow and continue; optionally log.
                }

                if (meta is not null)
                    yield return meta;
            }
        }
    }

    private ExampleProjectMeta CreateMetaFromProject(string projectFile)
    {
        var doc = XDocument.Load(projectFile, LoadOptions.None);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid project XML.");
        var ns = root.Name.Namespace; // Usually empty for SDK-style

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

        // ID: use folder or assembly base (stable)
        var id = assemblyName ?? Path.GetFileNameWithoutExtension(projectFile);

        return new ExampleProjectMeta(id, title, projectFile, order, category);
    }

    private string? TryParseProgramCommentTitle(string projectFile)
    {
        // Look for Program.* alongside project
        var dir = Path.GetDirectoryName(projectFile);

        if (dir is null) return null;

        var programFile = Directory.EnumerateFiles(dir, "Program.*", SearchOption.TopDirectoryOnly)
                                   .FirstOrDefault();
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
        // For faster subsequent runs you can add: --no-build
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

        _ = Task.Run(async () =>
        {
            try
            {
                // Stream output (optional: aggregate, colorize, etc.)
                while (!process.HasExited)
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    if (line is null) break;
                    Console.WriteLine(line);
                }

                while (!process.StandardError.EndOfStream)
                {
                    var err = await process.StandardError.ReadLineAsync();
                    if (err is null) break;
                    Console.Error.WriteLine(err);
                }
            }
            catch { /* ignore */ }
        });
    }

    private string GetIndex() => Interlocked.Increment(ref _index).ToString();
}
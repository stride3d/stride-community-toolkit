using Microsoft.Extensions.Logging;
using Stride.CommunityToolkit.Examples.MetadataGenerator.Core;
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace Stride.CommunityToolkit.Examples.MetadataGenerator.Services;

/// <summary>
/// Finds the source files that carry an example metadata block.
/// </summary>
/// <remarks>
/// <para>
/// Every source file is considered, not just <c>Program.cs</c>. The metadata block itself is what marks
/// a file as an example, which frees file-based apps from the <c>Program.cs</c> name - <c>dotnet run
/// Basic3DScene.cs</c> says far more than <c>dotnet run Program.cs</c> - and allows one folder to hold
/// several examples. Uniqueness of <c>slug</c> is what keeps that honest.
/// </para>
/// <para>
/// Because the search is no longer restricted to a single filename, build output has to be excluded
/// explicitly: <c>obj</c> holds generated <c>.cs</c> files, and <c>bin</c> holds copies of anything.
/// </para>
/// </remarks>
public partial class ExampleScanner(ILogger<ExampleScanner> logger)
{
    /// <summary>Directory names that are skipped wherever they appear in the tree.</summary>
    private static readonly string[] ExcludedDirectories = ["bin", "obj", ".vs", ".git", "node_modules"];

    private static readonly string[] ProjectPatterns = ["*.csproj", "*.fsproj", "*.vbproj"];

    [GeneratedRegex(@"<ProjectReference\s+Include\s*=\s*""(?<path>[^""]+)""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ProjectReferencePattern();

    /// <summary>
    /// Finds every source file under <paramref name="examplesRootPath"/> that contains a metadata block.
    /// </summary>
    /// <param name="examplesRootPath">The root directory to scan.</param>
    /// <returns>The matching file paths, in a stable alphabetical order.</returns>
    public IReadOnlyList<string> FindExampleFiles(DirectoryInfo examplesRootPath)
    {
        ArgumentNullException.ThrowIfNull(examplesRootPath);

        logger.LogInformation("Scanning for example metadata blocks in: {Path}", examplesRootPath.FullName);

        if (!examplesRootPath.Exists)
        {
            logger.LogError("Examples directory does not exist: {Path}", examplesRootPath.FullName);

            return [];
        }

        var candidates = YamlMetadataExtractor.LanguageByExtension.Keys
            .SelectMany(extension => Directory.EnumerateFiles(examplesRootPath.FullName, $"*{extension}", SearchOption.AllDirectories))
            .Where(path => !IsExcluded(path, examplesRootPath.FullName))
            .Order(StringComparer.Ordinal)
            .ToList();

        logger.LogInformation("Examined {Count} source file(s)", candidates.Count);

        return candidates;
    }

    /// <summary>
    /// Lists every example project folder, whether or not it carries metadata yet.
    /// </summary>
    /// <param name="examplesRootPath">The root directory to scan.</param>
    /// <returns>The folder names, for resolving <c>related:</c> entries.</returns>
    /// <remarks>
    /// <c>related:</c> has to be checked against this rather than against the examples that were
    /// actually parsed. During the backfill most examples still have no metadata block, and treating a
    /// perfectly correct project name as a typo purely because that example has not been reached yet
    /// would bury the real typos in noise.
    /// </remarks>
    public static IReadOnlySet<string> FindProjectNames(DirectoryInfo examplesRootPath)
    {
        ArgumentNullException.ThrowIfNull(examplesRootPath);

        if (!examplesRootPath.Exists) return FrozenSet<string>.Empty;

        return examplesRootPath
            .EnumerateDirectories()
            .Select(directory => directory.Name)
            .Where(name => !ExcludedDirectories.Contains(name, StringComparer.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the project name for an example source file - the name of the folder that contains it.
    /// </summary>
    /// <param name="exampleFilePath">The full path to the source file.</param>
    /// <returns>The project directory name.</returns>
    public static string GetProjectName(string exampleFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exampleFilePath);

        return Path.GetFileName(Path.GetDirectoryName(exampleFilePath)) ?? string.Empty;
    }

    /// <summary>
    /// Finds the toolkit packages an example references that are not on NuGet: the
    /// <c>ProjectReference</c>s of the project file beside the entry file, or the <c>#:project</c>
    /// directives of a file-based app, filtered by <see cref="DocPaths.PackagesNotOnNuGet"/>.
    /// </summary>
    /// <param name="exampleFilePath">The full path to the entry source file.</param>
    /// <returns>The package names, in a stable order; empty when the example needs none.</returns>
    public static IReadOnlyList<string> FindRepositoryOnlyPackages(string exampleFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exampleFilePath);

        var references = new List<string>();

        if (Path.GetDirectoryName(exampleFilePath) is { } directory)
        {
            foreach (var projectFile in ProjectPatterns.SelectMany(pattern => Directory.EnumerateFiles(directory, pattern)))
            {
                references.AddRange(ProjectReferenceNames(File.ReadAllText(projectFile)));
            }
        }

        // A file-based app carries its references as directives in the entry file itself
        references.AddRange(File.ReadLines(exampleFilePath)
            .TakeWhile(line => line.StartsWith("#:", StringComparison.Ordinal) || line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
            .Where(line => line.StartsWith("#:project", StringComparison.Ordinal))
            .Select(line => Path.GetFileNameWithoutExtension(line["#:project".Length..].Trim())));

        return references
            .Where(DocPaths.PackagesNotOnNuGet.Contains)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private static IEnumerable<string> ProjectReferenceNames(string projectXml)
        => ProjectReferencePattern().Matches(projectXml).Select(match => Path.GetFileNameWithoutExtension(match.Groups["path"].Value.Replace('\\', '/')));

    // The last segment is the file itself; any directory on the way may exclude it
    private static bool IsExcluded(string filePath, string rootPath)
        => Path.GetRelativePath(rootPath, filePath)
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[..^1]
            .Any(segment => ExcludedDirectories.Contains(segment, StringComparer.OrdinalIgnoreCase));
}
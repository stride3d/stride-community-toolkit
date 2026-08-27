using Microsoft.Extensions.Logging;
using Stride.CommunityToolkit.Examples.MetadataGenerator.Core;

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
public class ExampleScanner(ILogger<ExampleScanner> logger)
{
    /// <summary>Directory names that are skipped wherever they appear in the tree.</summary>
    private static readonly string[] ExcludedDirectories = ["bin", "obj", ".vs", ".git", "node_modules"];

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
    public IReadOnlySet<string> FindProjectNames(DirectoryInfo examplesRootPath)
    {
        ArgumentNullException.ThrowIfNull(examplesRootPath);

        if (!examplesRootPath.Exists)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var names = examplesRootPath
            .EnumerateDirectories()
            .Select(directory => directory.Name)
            .Where(name => !ExcludedDirectories.Contains(name, StringComparer.OrdinalIgnoreCase));

        return new HashSet<string>(names, StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the project name for an example source file - the name of the folder that contains it.
    /// </summary>
    /// <param name="exampleFilePath">The full path to the source file.</param>
    /// <returns>The project directory name.</returns>
    public string GetProjectName(string exampleFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exampleFilePath);

        var projectDirectory = Path.GetDirectoryName(exampleFilePath);

        return Path.GetFileName(projectDirectory) ?? string.Empty;
    }

    private static bool IsExcluded(string filePath, string rootPath)
    {
        var relative = Path.GetRelativePath(rootPath, filePath);
        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // The last segment is the file itself.
        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (ExcludedDirectories.Contains(segments[i], StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

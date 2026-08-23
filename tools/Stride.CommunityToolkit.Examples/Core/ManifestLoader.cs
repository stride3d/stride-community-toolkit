using System.Globalization;
using System.Text.Json;

namespace Stride.CommunityToolkit.Examples.Core;

/// <summary>
/// Loads <c>examples-manifest.json</c> and turns it into runnable entries.
/// </summary>
/// <remarks>
/// This replaces the previous approach of reading <c>&lt;ExampleTitle&gt;</c> and friends out of every
/// <c>.csproj</c>. That scheme required a new example to be registered in two unrelated places, and it
/// silently omitted any example whose author edited only one of them - nine of them, by the time it was
/// replaced.
/// </remarks>
public static class ManifestLoader
{
    /// <summary>The manifest filename, copied next to the launcher executable at build time.</summary>
    public const string ManifestFileName = "examples-manifest.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Loads every example that should appear in a launcher.
    /// </summary>
    /// <returns>
    /// The entries, in manifest order - already sorted by language, level and order - filtered to those
    /// with <c>launcher</c> not set to <see langword="false"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">The manifest is missing, unreadable, or a newer schema.</exception>
    public static IReadOnlyList<ExampleEntry> Load()
    {
        var manifestPath = FindManifest()
            ?? throw new InvalidOperationException(
                $"{ManifestFileName} was not found next to the executable. It is generated before the build " +
                "by tools/ExamplesManifest.targets - build the launcher rather than running a stale binary.");

        var manifest = Read(manifestPath);
        var examplesRoot = FindExamplesRoot();
        var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        return [.. manifest.Examples
            .Where(example => example.Launcher != false)
            .Select(example => ToEntry(example, examplesRoot, language))];
    }

    /// <summary>
    /// Reads and validates the manifest document.
    /// </summary>
    private static ExampleManifest Read(string manifestPath)
    {
        ExampleManifest? manifest;

        try
        {
            manifest = JsonSerializer.Deserialize<ExampleManifest>(File.ReadAllText(manifestPath), JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"{manifestPath} is not valid JSON: {ex.Message}", ex);
        }

        if (manifest is null)
        {
            throw new InvalidOperationException($"{manifestPath} is empty.");
        }

        // A newer manifest may contain fields or semantics this build does not know about. Saying so is
        // better than silently showing a wrong list.
        if (manifest.SchemaVersion > ExampleManifest.SupportedSchemaVersion)
        {
            throw new InvalidOperationException(
                $"{manifestPath} is schema v{manifest.SchemaVersion}; this launcher understands v{ExampleManifest.SupportedSchemaVersion}. Rebuild the launcher.");
        }

        return manifest;
    }

    /// <summary>
    /// Turns a manifest entry into something runnable, resolving what <c>dotnet run</c> should be given.
    /// </summary>
    private static ExampleEntry ToEntry(ManifestExample example, string? examplesRoot, string language)
    {
        var projectName = example.ProjectName ?? example.Slug ?? "(unknown)";
        var (runTarget, isFileBased) = ResolveRunTarget(example, examplesRoot);

        return new ExampleEntry(
            Slug: example.Slug ?? projectName,
            ProjectName: projectName,
            Title: example.TitleFor(language) ?? projectName,
            Description: example.DescriptionFor(language),
            Level: example.Level ?? Levels.Other,
            Category: example.Category,
            Complexity: example.Complexity,
            Language: example.Language ?? "csharp",
            Tags: example.Tags ?? [],
            RunTarget: runTarget,
            IsFileBased: isFileBased);
    }

    /// <summary>
    /// Finds what to run for an example: its project file, or its source file if it has none.
    /// </summary>
    /// <remarks>
    /// The manifest records the entry <em>source</em> file, because that is where the metadata block
    /// lives. Most examples sit next to a project file and are run through it. A file-based app
    /// deliberately has no project file, and is run by naming the source file - so it is launchable
    /// here for the first time, where the old csproj-scanning approach could not see it at all.
    /// </remarks>
    private static (string RunTarget, bool IsFileBased) ResolveRunTarget(ManifestExample example, string? examplesRoot)
    {
        if (examplesRoot is null || example.ProjectPath is not { Length: > 0 } relativePath)
        {
            return (string.Empty, false);
        }

        var sourceFile = Path.GetFullPath(Path.Combine(examplesRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var directory = Path.GetDirectoryName(sourceFile);

        if (directory is null || !Directory.Exists(directory))
        {
            return (string.Empty, false);
        }

        string[] projectPatterns = ["*.csproj", "*.fsproj", "*.vbproj"];

        foreach (var pattern in projectPatterns)
        {
            var project = Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly).FirstOrDefault();

            if (project is not null)
            {
                return (project, false);
            }
        }

        return File.Exists(sourceFile) ? (sourceFile, true) : (string.Empty, false);
    }

    /// <summary>
    /// Locates the manifest next to the executable.
    /// </summary>
    private static string? FindManifest()
    {
        var candidate = Path.Combine(AppContext.BaseDirectory, ManifestFileName);

        return File.Exists(candidate) ? candidate : null;
    }

    /// <summary>
    /// Walks up from the executable to find <c>examples/code-only</c>.
    /// </summary>
    /// <remarks>
    /// The manifest stores paths relative to the examples root rather than absolute ones, so that it is
    /// identical on every machine. That means the root has to be rediscovered here.
    /// </remarks>
    private static string? FindExamplesRoot()
    {
        var directory = AppContext.BaseDirectory;

        for (var depth = 0; depth < 8 && !string.IsNullOrEmpty(directory); depth++)
        {
            var candidate = Path.Combine(directory, "examples", "code-only");

            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        return null;
    }
}

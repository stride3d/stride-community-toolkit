using System.Drawing;

namespace Stride.CommunityToolkit.Examples.Core;

/// <summary>
/// A runnable example, resolved from the manifest and ready for a launcher to display and start.
/// </summary>
/// <param name="Slug">The short, unique identifier.</param>
/// <param name="ProjectName">The project directory name.</param>
/// <param name="Title">The title in the current UI language, already falling back to English.</param>
/// <param name="Description">The description in the current UI language, or <see langword="null"/>.</param>
/// <param name="Level">The teaching level, used for grouping and colour.</param>
/// <param name="Category">The topic.</param>
/// <param name="Complexity">The relative difficulty, 1 to 5.</param>
/// <param name="Language">The source language.</param>
/// <param name="Tags">The free-form topic tags.</param>
/// <param name="RunTarget">
/// What to hand to <c>dotnet run</c>: a project file, or a single <c>.cs</c> file for a file-based app.
/// Empty when neither could be found on disk.
/// </param>
/// <param name="IsFileBased">
/// Whether <paramref name="RunTarget"/> is a source file rather than a project, which decides whether
/// the command needs <c>--project</c>.
/// </param>
public sealed record ExampleEntry(
    string Slug,
    string ProjectName,
    string Title,
    string? Description,
    string Level,
    string? Category,
    int? Complexity,
    string Language,
    IReadOnlyList<string> Tags,
    string RunTarget,
    bool IsFileBased)
{
    /// <summary>Gets whether this example can actually be started.</summary>
    public bool IsRunnable => RunTarget.Length > 0 && File.Exists(RunTarget);

    /// <summary>Gets the folder holding the example.</summary>
    public string? Directory => RunTarget.Length > 0 ? Path.GetDirectoryName(RunTarget) : null;

    /// <summary>
    /// Gets the command line that starts this example, as a user would type it.
    /// </summary>
    /// <remarks>
    /// A file-based app has no project file, so it is run by naming the source file directly - which is
    /// the whole point of that format and the reason the two cases are distinguished.
    /// </remarks>
    public string CommandLine => IsFileBased
        ? $"dotnet run \"{RunTarget}\""
        : $"dotnet run --project \"{RunTarget}\"";

    /// <summary>Gets the arguments to pass to <c>dotnet</c>.</summary>
    public string ProcessArguments => IsFileBased
        ? $"run \"{RunTarget}\""
        : $"run --project \"{RunTarget}\"";

    /// <summary>
    /// Gets the colour used for this example's level.
    /// </summary>
    /// <returns>A colour, warming as the level rises.</returns>
    public Color GetColor() => Level switch
    {
        Levels.GettingStarted => Color.MediumSeaGreen,
        Levels.Beginner => Color.CornflowerBlue,
        Levels.Intermediate => Color.MediumPurple,
        Levels.Advanced => Color.Orange,
        _ => Color.LightGray
    };

    /// <summary>Gets a short language tag for display, or empty for C#.</summary>
    public string LanguageLabel => Language switch
    {
        "fsharp" => "F#",
        "vb" => "VB",
        _ => string.Empty
    };
}

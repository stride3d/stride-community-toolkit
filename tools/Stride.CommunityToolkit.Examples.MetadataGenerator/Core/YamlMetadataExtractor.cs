using System.Text;
using System.Text.RegularExpressions;

namespace Stride.CommunityToolkit.Examples.MetadataGenerator.Core;

/// <summary>
/// Extracts the raw YAML of an <c>---example-metadata</c> block from an example's source file.
/// </summary>
/// <remarks>
/// <para>
/// The block is written as a comment, so its delimiters depend on the language: C# and F# use their
/// block-comment forms, and Visual Basic - which has no block comment - uses a run of line comments.
/// </para>
/// <code>
/// C#            F#            Visual Basic
/// /* ---…       (* ---…       ' ---…
///    …             …          ' …
///    --- */        --- *)     ' ---
/// </code>
/// <para>
/// <c>RegexOptions.Compiled</c> is deliberately absent: <c>[GeneratedRegex]</c> already emits a
/// compiled implementation, so the flag only adds startup cost.
/// </para>
/// </remarks>
public static partial class YamlMetadataExtractor
{
    /// <summary>The source file extensions that may carry a metadata block, mapped to their language.</summary>
    public static readonly IReadOnlyDictionary<string, string> LanguageByExtension =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".cs"] = "csharp",
            [".fs"] = "fsharp",
            [".vb"] = "vb"
        };

    [GeneratedRegex(@"/\*\s*---example-metadata\s*(.*?)\s*---\s*\*/", RegexOptions.Singleline)]
    private static partial Regex CSharpBlockPattern();

    [GeneratedRegex(@"\(\*\s*---example-metadata\s*(.*?)\s*---\s*\*\)", RegexOptions.Singleline)]
    private static partial Regex FSharpBlockPattern();

    // Both delimiters tolerate a trailing CR: in multiline mode '$' matches before the LF, so on a
    // CRLF file the CR is still sitting in front of it and an unguarded '$' silently fails to match.
    [GeneratedRegex(@"^[ \t]*'[ \t]*---example-metadata[ \t]*\r?$(.*?)^[ \t]*'[ \t]*---[ \t]*\r?$",
        RegexOptions.Singleline | RegexOptions.Multiline)]
    private static partial Regex VisualBasicBlockPattern();

    /// <summary>
    /// Attempts to extract the metadata block from a source file's contents.
    /// </summary>
    /// <param name="filePath">The source file path; its extension selects the comment syntax.</param>
    /// <param name="content">The full contents of the file.</param>
    /// <param name="yaml">The raw YAML between the delimiters, comment prefixes removed.</param>
    /// <returns><see langword="true"/> if a block was found.</returns>
    public static bool TryExtract(string filePath, string content, out string yaml)
        => TryExtract(filePath, content, out yaml, out _);

    /// <summary>
    /// Attempts to extract the metadata block, also reporting where it sits in the file.
    /// </summary>
    /// <param name="filePath">The source file path; its extension selects the comment syntax.</param>
    /// <param name="content">The full contents of the file.</param>
    /// <param name="yaml">The raw YAML between the delimiters, comment prefixes removed.</param>
    /// <param name="location">Where the block starts and ends, for excluding it from a docs code include.</param>
    /// <returns><see langword="true"/> if a block was found.</returns>
    public static bool TryExtract(string filePath, string content, out string yaml, out MetadataBlockLocation location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(content);

        yaml = string.Empty;
        location = default;

        var extension = Path.GetExtension(filePath);

        if (!LanguageByExtension.TryGetValue(extension, out var language))
        {
            return false;
        }

        var match = language switch
        {
            "fsharp" => FSharpBlockPattern().Match(content),
            "vb" => VisualBasicBlockPattern().Match(content),
            _ => CSharpBlockPattern().Match(content)
        };

        if (!match.Success)
        {
            return false;
        }

        yaml = language == "vb"
            ? StripLineCommentPrefixes(match.Groups[1].Value)
            : match.Groups[1].Value.Trim();

        location = MetadataBlockLocation.Measure(content, match.Index, match.Index + match.Length);

        return yaml.Length > 0;
    }

    /// <summary>
    /// Gets the language implied by a source file's extension, or <see langword="null"/> if the
    /// extension is not one the scanner reads.
    /// </summary>
    /// <param name="filePath">The source file path.</param>
    /// <returns>The language identifier, matching <see cref="MetadataVocabulary.Languages"/>.</returns>
    public static string? GetLanguage(string filePath)
        => LanguageByExtension.TryGetValue(Path.GetExtension(filePath), out var language) ? language : null;

    /// <summary>
    /// Removes the leading <c>'</c> (and the single space that conventionally follows it) from every
    /// line of a Visual Basic comment block, leaving the YAML indentation intact.
    /// </summary>
    private static string StripLineCommentPrefixes(string block)
    {
        var builder = new StringBuilder();

        foreach (var line in block.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');
            var apostrophe = trimmed.IndexOf('\'');

            if (apostrophe < 0)
            {
                // A blank separator line inside the block.
                if (trimmed.Trim().Length == 0)
                {
                    builder.AppendLine();
                }

                continue;
            }

            var payload = trimmed[(apostrophe + 1)..];

            if (payload.StartsWith(' '))
            {
                payload = payload[1..];
            }

            builder.AppendLine(payload);
        }

        return builder.ToString().Trim('\n', '\r');
    }
}

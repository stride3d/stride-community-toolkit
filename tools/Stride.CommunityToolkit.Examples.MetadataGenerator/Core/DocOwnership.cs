using System.Text.RegularExpressions;

namespace Stride.CommunityToolkit.Examples.MetadataGenerator.Core;

/// <summary>
/// Who owns a documentation file's content.
/// </summary>
public enum DocOwnership
{
    /// <summary>Written by a person. The generator never touches it.</summary>
    HandOwned,

    /// <summary>Entirely tool-owned and overwritten freely.</summary>
    Generated,

    /// <summary>Only the delimited region is tool-owned; everything else is preserved verbatim.</summary>
    Partial
}

/// <summary>
/// Reads the ownership marker out of an existing documentation file.
/// </summary>
/// <remarks>
/// <para>
/// Adoption is opt-in per file, which is the whole safety story: none of the documentation written by
/// hand carries frontmatter, so the first run of the generator cannot destroy any of it. A file becomes
/// tool-owned only when someone adds <c>generated: true</c> or <c>generated: partial</c> to it.
/// </para>
/// <para>
/// This is not a YAML parser. The frontmatter of a documentation page is a handful of flat
/// <c>key: value</c> lines, and reading them with a regex avoids making the generator's docs command
/// depend on how DocFX happens to parse a block it only ever writes itself.
/// </para>
/// </remarks>
public static partial class DocFrontmatter
{
    /// <summary>Opens the tool-owned region in a <see cref="DocOwnership.Partial"/> file.</summary>
    public const string RegionStart = "<!-- #region generated -->";

    /// <summary>Closes the tool-owned region in a <see cref="DocOwnership.Partial"/> file.</summary>
    public const string RegionEnd = "<!-- #endregion generated -->";

    [GeneratedRegex(@"\A---\r?\n(.*?)\r?\n---\r?\n", RegexOptions.Singleline)]
    private static partial Regex FrontmatterBlockPattern();

    [GeneratedRegex(@"^generated:\s*(\S+)\s*$", RegexOptions.Multiline)]
    private static partial Regex GeneratedKeyPattern();

    /// <summary>
    /// Determines who owns a file's content.
    /// </summary>
    /// <param name="content">The full text of the documentation file.</param>
    /// <returns>The ownership; <see cref="DocOwnership.HandOwned"/> when there is no marker.</returns>
    public static DocOwnership ReadOwnership(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var frontmatter = FrontmatterBlockPattern().Match(content);

        if (!frontmatter.Success)
        {
            return DocOwnership.HandOwned;
        }

        var generated = GeneratedKeyPattern().Match(frontmatter.Groups[1].Value);

        if (!generated.Success)
        {
            return DocOwnership.HandOwned;
        }

        return generated.Groups[1].Value.Trim().ToLowerInvariant() switch
        {
            "true" => DocOwnership.Generated,
            "partial" => DocOwnership.Partial,
            _ => DocOwnership.HandOwned
        };
    }

    /// <summary>
    /// Replaces the tool-owned region of a partial file, leaving both markers and everything outside
    /// them exactly as they were.
    /// </summary>
    /// <param name="content">The existing file text.</param>
    /// <param name="replacement">The new content for the region.</param>
    /// <param name="result">The rewritten file.</param>
    /// <returns><see langword="false"/> if either marker is missing, in which case nothing is changed.</returns>
    /// <remarks>
    /// A string replace between two delimiters, not a three-way merge: deterministic, and a contributor
    /// editing the file can see which part they must not touch. If a marker has been deleted the
    /// boundary is genuinely unknown, so the caller warns and skips rather than guessing one.
    /// </remarks>
    public static bool TryReplaceRegion(string content, string replacement, out string result)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(replacement);

        result = content;

        var start = content.IndexOf(RegionStart, StringComparison.Ordinal);
        var end = content.IndexOf(RegionEnd, StringComparison.Ordinal);

        if (start < 0 || end < 0 || end < start)
        {
            return false;
        }

        var afterStart = start + RegionStart.Length;

        result = content[..afterStart]
            + Environment.NewLine
            + replacement.TrimEnd()
            + Environment.NewLine
            + content[end..];

        return true;
    }
}
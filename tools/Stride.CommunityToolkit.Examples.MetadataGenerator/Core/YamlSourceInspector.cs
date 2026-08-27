using System.Text.RegularExpressions;

namespace Stride.CommunityToolkit.Examples.MetadataGenerator.Core;

/// <summary>
/// Inspects the raw text of a metadata block for mistakes that are invisible once it is deserialized.
/// </summary>
/// <remarks>
/// <para>
/// Two authoring mistakes account for most lost metadata, and neither can be diagnosed from the parsed
/// object. An unquoted <c>#</c> silently truncates its value, so the object simply holds a shorter
/// string than the author wrote. An unquoted <c>": "</c> inside a sequence item turns the item into a
/// mapping, which does not truncate anything - it makes deserialization fail several frames deep in
/// YamlDotNet with "Uninitialized Strings cannot be created", a message that names neither the line nor
/// the cause.
/// </para>
/// <para>
/// Because the second case aborts parsing, this runs on both paths: as part of validation when the
/// block parsed, and as the diagnosis attached to the error when it did not.
/// </para>
/// </remarks>
public static partial class YamlSourceInspector
{
    /// <summary>Matches a key that opens a block scalar, capturing the chomping indicator if present.</summary>
    [GeneratedRegex(@"^\s*[A-Za-z0-9_-]+:\s*[|>]([-+]?)\d*\s*$")]
    private static partial Regex BlockScalarHeaderPattern();

    /// <summary>
    /// Scans a metadata block's source text.
    /// </summary>
    /// <param name="projectName">The example the block belongs to, used to attribute findings.</param>
    /// <param name="rawYaml">The raw YAML between the block delimiters.</param>
    /// <returns>Every finding, in source order.</returns>
    public static IReadOnlyList<ValidationMessage> Inspect(string projectName, string rawYaml)
    {
        ArgumentNullException.ThrowIfNull(rawYaml);

        var messages = new List<ValidationMessage>();

        int? blockScalarIndent = null;

        foreach (var rawLine in rawYaml.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var indent = line.Length - line.TrimStart().Length;

            if (blockScalarIndent is { } openIndent)
            {
                if (line.Trim().Length == 0 || indent > openIndent)
                {
                    // Literal block content: '#' and ':' are ordinary characters here.
                    continue;
                }

                blockScalarIndent = null;
            }

            var trimmed = line.TrimStart();

            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var blockScalar = BlockScalarHeaderPattern().Match(line);

            if (blockScalar.Success)
            {
                if (blockScalar.Groups[1].Value != "-")
                {
                    messages.Add(ValidationMessage.Warning(projectName, trimmed.Split(':')[0],
                        "Block scalar opened with '|' rather than '|-', so the value keeps a trailing newline. The parser strips it, but write '|-' so the source matches the manifest."));
                }

                blockScalarIndent = indent;

                continue;
            }

            InspectInlineComment(projectName, line, messages);
            InspectSequenceItemColon(projectName, line, messages);
        }

        return messages;
    }

    /// <summary>
    /// Flags a <c>#</c> that starts a comment in the middle of a value, which silently truncates it.
    /// </summary>
    private static void InspectInlineComment(string projectName, string line, List<ValidationMessage> messages)
    {
        var (index, key) = FindUnquoted(line, '#');

        if (index <= 0 || !char.IsWhiteSpace(line[index - 1]))
        {
            return;
        }

        if (line[..index].Trim().Length == 0)
        {
            return;
        }

        messages.Add(ValidationMessage.Error(projectName, key,
            $"An unquoted '#' starts a YAML comment, so this value is truncated to '{line[..index].Trim()}'. Quote the value. Line: {line.Trim()}"));
    }

    /// <summary>
    /// Flags a sequence item containing an unquoted <c>": "</c>, which YAML reads as a mapping rather
    /// than as the string it looks like.
    /// </summary>
    private static void InspectSequenceItemColon(string projectName, string line, List<ValidationMessage> messages)
    {
        var trimmed = line.TrimStart();

        if (!trimmed.StartsWith("- ", StringComparison.Ordinal))
        {
            return;
        }

        var value = trimmed[2..].Trim();

        if (value.StartsWith('"') || value.StartsWith('\''))
        {
            return;
        }

        if (!value.Contains(": ", StringComparison.Ordinal))
        {
            return;
        }

        messages.Add(ValidationMessage.Error(projectName, "(sequence item)",
            $"A sequence item containing ': ' is read as a mapping, not a string, and aborts parsing. Quote it. Line: {trimmed}"));
    }

    /// <summary>
    /// Finds the first occurrence of <paramref name="target"/> outside quotes, and the key of the line
    /// it appears on.
    /// </summary>
    private static (int Index, string Key) FindUnquoted(string line, char target)
    {
        var inSingle = false;
        var inDouble = false;
        var found = -1;
        var colon = -1;

        for (var i = 0; i < line.Length; i++)
        {
            var current = line[i];

            if (current == '\'' && !inDouble)
            {
                inSingle = !inSingle;
            }
            else if (current == '"' && !inSingle)
            {
                inDouble = !inDouble;
            }
            else if (!inSingle && !inDouble)
            {
                if (current == ':' && colon < 0)
                {
                    colon = i;
                }
                else if (current == target && found < 0)
                {
                    found = i;
                }
            }
        }

        var key = colon > 0 ? line[..colon].Trim().TrimStart('-', ' ') : line.Trim().TrimStart('-', ' ');

        return (found, key.Length == 0 ? "(value)" : key);
    }
}

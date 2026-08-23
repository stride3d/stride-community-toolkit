namespace Stride.CommunityToolkit.Examples.MetadataGenerator.Core;

/// <summary>
/// Where an example's metadata block sits in its source file.
/// </summary>
/// <remarks>
/// <para>
/// The documentation embeds each example's source with a DocFX code include. Left alone that renders
/// the metadata block too, which is both noise and a duplicate - the description and concepts are
/// already on the page as prose, immediately above the listing.
/// </para>
/// <para>
/// DocFX takes a line range on a code include, so the block can simply be left out of the range. That
/// keeps the fix at generation time: no DocFX plugin, no post-processing pass over generated HTML, and
/// it works in <c>docfx serve</c> like anything else.
/// </para>
/// </remarks>
/// <param name="CodeLineCount">
/// How many lines of real code precede the block, with blank lines immediately before it not counted.
/// Zero when the block is at the very top of the file.
/// </param>
/// <param name="IsLastInFile">
/// Whether nothing but whitespace follows the block. Only then can the source be expressed as a single
/// leading range.
/// </param>
public readonly record struct MetadataBlockLocation(int CodeLineCount, bool IsLastInFile)
{
    /// <summary>
    /// Gets whether the code can be included as one range ending before the block.
    /// </summary>
    public bool CanTrimBlock => IsLastInFile && CodeLineCount > 0;

    /// <summary>
    /// Measures a block's position within its file.
    /// </summary>
    /// <param name="content">The full file contents.</param>
    /// <param name="startIndex">Where the block's opening delimiter begins.</param>
    /// <param name="endIndex">Just past the block's closing delimiter.</param>
    /// <returns>The measurement.</returns>
    public static MetadataBlockLocation Measure(string content, int startIndex, int endIndex)
    {
        ArgumentNullException.ThrowIfNull(content);

        var lines = content[..startIndex].Split('\n');

        // The block begins its own line, so every earlier element is a complete line of source.
        var count = lines.Length - 1;

        // Trailing blank lines belong to the separation before the block, not to the code.
        while (count > 0 && string.IsNullOrWhiteSpace(lines[count - 1]))
        {
            count--;
        }

        return new MetadataBlockLocation(count, content[endIndex..].AsSpan().IsWhiteSpace());
    }
}

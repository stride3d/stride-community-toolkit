using Stride.CommunityToolkit.Rendering.Text;

namespace Example.Common;

/// <summary>
/// Overlay lines from a sentence that may be too long for one: the toolkit's help block keeps every
/// line to about fifty characters, and a long line widens the whole block and pushes it across the
/// screen. Text is split at spaces, never inside a word.
/// </summary>
public static class OverlayText
{
    /// <summary>The line length the examples' overlay help keeps to.</summary>
    public const int DefaultWidth = 50;

    /// <summary>Wraps a sentence into overlay lines of at most <paramref name="width"/> characters, all in one colour.</summary>
    /// <param name="text">The sentence.</param>
    /// <param name="color">The colour of every line.</param>
    /// <param name="width">The longest line, in characters; a single word longer than it stands alone.</param>
    public static IEnumerable<TextElement> Wrap(string text, Color color, int width = DefaultWidth)
    {
        ArgumentNullException.ThrowIfNull(text);

        var line = new System.Text.StringBuilder();

        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                yield return new(line.ToString(), color);
                line.Clear();
            }

            if (line.Length > 0) line.Append(' ');
            line.Append(word);
        }

        if (line.Length > 0) yield return new(line.ToString(), color);
    }
}
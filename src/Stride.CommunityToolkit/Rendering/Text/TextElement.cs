using Stride.CommunityToolkit.Rendering.Overlay;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// One line of on-screen text: what it says, its colour, and, for a help line, the keys it is about.
/// </summary>
/// <param name="Text">The text content to be displayed.</param>
/// <param name="Color">The optional color for the text. If null, a default color will be used.</param>
/// <remarks>
/// A help line names its keys as data rather than in the text - <c>new("H", "Reset camera")</c>, not
/// <c>new("[H] Reset camera")</c> - and the <see cref="TextOverlay"/> decorates them when it draws, with
/// <see cref="TextOverlay.KeyFormat"/> and in <see cref="TextOverlay.KeyColor"/>. Changing the whole
/// toolkit from <c>[H]</c> to <c>H:</c> is then one property, not an edit to every example.
/// </remarks>
public record TextElement(string Text, Color? Color = null)
{
    /// <summary>
    /// The keys the line is about, drawn before the text and decorated by the overlay: <c>"H"</c>,
    /// <c>"Space"</c>, <c>"Arrow keys"</c>; several when they do the same thing, drawn as <c>[Q] [E]</c>.
    /// </summary>
    public IReadOnlyList<string>? Keys { get; init; }

    /// <summary>
    /// A state marker drawn before the keys, in the key colour and as given: a collapsible section's
    /// <c>[+]</c> or <c>[-]</c>. Not decorated like a key, since it is not one.
    /// </summary>
    public string? Marker { get; init; }

    /// <summary>
    /// Whether the line is drawn one marker in, so its keys sit under the key of a title with a marker.
    /// The overlay sets it for the body of a collapsible section; a key menu sets it on its items.
    /// </summary>
    public bool Indented { get; init; }

    /// <summary>A help line about one key: <c>new("H", "Reset camera")</c>.</summary>
    /// <param name="key">The key, as a reader would say it: <c>"H"</c>, <c>"Space"</c>, <c>"Mouse wheel"</c>.</param>
    /// <param name="text">What the key does, capitalised like a sentence.</param>
    /// <param name="color">The optional color for the text.</param>
    public TextElement(string key, string text, Color? color = null) : this(text, color)
    {
        Keys = [key];
    }

    /// <summary>A help line about several keys that do the same thing: <c>new(["Q", "E"], "Ascend / descend")</c>.</summary>
    /// <param name="keys">The keys, in the order they are drawn.</param>
    /// <param name="text">What they do, capitalised like a sentence.</param>
    /// <param name="color">The optional color for the text.</param>
    public TextElement(IReadOnlyList<string> keys, string text, Color? color = null) : this(text, color)
    {
        Keys = keys;
    }
}
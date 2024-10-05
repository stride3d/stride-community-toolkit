namespace Stride.CommunityToolkit.Rendering.Compositing;

/// <summary>
/// Options for rendering entity debug information.
/// </summary>
public class EntityDebugRendererOptions
{
    /// <summary>
    /// Gets or sets the font size for the debug text.
    /// </summary>
    public int FontSize { get; set; } = 12;

    /// <summary>
    /// Gets or sets the font color for the debug text.
    /// </summary>
    public Color FontColor { get; set; } = Color.Black;

    /// <summary>
    /// Gets or sets a value indicating whether to show the entity name.
    /// </summary>
    public bool ShowEntityName { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show the entity position.
    /// </summary>
    public bool ShowEntityPosition { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show the background for the font.
    /// </summary>
    public bool ShowFontBackground { get; set; }

    /// <summary>
    /// Gets or sets the offset for the debug text.
    /// </summary>
    public Vector2 Offset { get; set; } = new Vector2(0, -25);
}
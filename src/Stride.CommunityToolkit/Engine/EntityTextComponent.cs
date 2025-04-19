using Stride.Engine;
using Stride.Graphics;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Represents a text rendering component that can be attached to an entity to display screen-space text.
/// </summary>
/// <remarks>
/// This component automatically positions text relative to the entity's position in screen space.
/// All color and positional values are managed in RGBA color space and screen pixels respectively.
/// </remarks>
public class EntityTextComponent : EntityComponent
{
    /// <summary>
    /// Gets or sets the text content to be displayed.
    /// </summary>
    /// <value>A non-null string containing the text to render.</value>
    public required string Text { get; set; }

    /// <summary>
    /// Gets or sets the size of the font in points.
    /// </summary>
    /// <value>
    /// A positive float value representing font size. Default is 18px.
    /// </value>
    public float FontSize { get; set; } = 18;

    /// <summary>
    /// Gets or sets the screen-space offset from the entity's position.
    /// </summary>
    /// <value>
    /// A <see cref="Vector2"/> representing X/Y offset in pixels.
    /// Positive values move text right/up, negative left/down.
    /// </value>
    public Vector2 Offset { get; set; }

    /// <summary>
    /// Gets or sets the color of the rendered text.
    /// </summary>
    /// <value>
    /// A <see cref="Color"/> structure defining the text color.
    /// Default is <see cref="Color.White"/> for better visibility against dark backgrounds.
    /// </value>
    public Color TextColor { get; set; } = Color.Black;

    /// <summary>
    /// Gets or sets the text alignment relative to the entity's position.
    /// </summary>
    /// <value>
    /// A <see cref="TextAlignment"/> enum value. Default is <see cref="TextAlignment.Left"/>.
    /// </value>
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;

    /// <summary>
    /// Gets or sets the color of the background behind the text.
    /// </summary>
    public Color4? BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the padding around the text.
    /// </summary>
    public float Padding { get; set; } = 2;

    /// <summary>
    /// Gets or sets a value indicating whether to use a background behind the text.
    /// </summary>
    public bool EnableBackground { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityTextComponent"/> class.
    /// </summary>
    public EntityTextComponent() { }

    /// <summary>
    /// Validates that the component has valid configuration.
    /// </summary>
    /// <returns>True if valid; otherwise, false.</returns>
    public bool Validate() => !string.IsNullOrEmpty(Text);
}
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Graphics;

namespace Stride.CommunityToolkit.Renderers;

/// <summary>
/// Everything needed to draw one piece of screen-space text, independent of where the text or the
/// position came from.
/// </summary>
/// <remarks>
/// This is what lets <see cref="EntityTextRenderer"/> and <see cref="EntityDebugSceneRenderer"/> share
/// a drawing path while keeping their own answers to <em>which</em> entities to label and
/// <em>what</em> to write. One builds a style from a component the caller configured, the other from
/// a single set of options applied to every entity; past that point the two are the same problem.
/// </remarks>
internal readonly struct ScreenTextStyle
{
    /// <summary>The font to draw with.</summary>
    public required SpriteFont Font { get; init; }

    /// <summary>Size the glyphs are rasterised at.</summary>
    public required float FontSize { get; init; }

    /// <summary>Colour of the text, before <see cref="Opacity"/>.</summary>
    public required Color Color { get; init; }

    /// <summary>Which point of the text sits on the drawn position.</summary>
    public TextAnchor Anchor { get; init; }

    /// <summary>How lines of a multi-line string sit relative to one another.</summary>
    public TextAlignment Alignment { get; init; }

    /// <summary>Multiplier on the drawn size. Scaling happens about <see cref="Anchor"/>.</summary>
    public float Scale { get; init; }

    /// <summary>Clockwise rotation in radians.</summary>
    public float Rotation { get; init; }

    /// <summary>Overall opacity from 0 to 1, applied to text, shadow and background alike.</summary>
    public float Opacity { get; init; }

    /// <summary>Draw order. Higher is drawn on top.</summary>
    public float LayerDepth { get; init; }

    /// <summary>Whether to draw the string a second time behind itself.</summary>
    public bool EnableShadow { get; init; }

    /// <summary>Colour of the shadow.</summary>
    public Color ShadowColor { get; init; }

    /// <summary>How far the shadow sits from the text, in pixels.</summary>
    public Vector2 ShadowOffset { get; init; }

    /// <summary>Whether to fill a rectangle behind the text.</summary>
    public bool EnableBackground { get; init; }

    /// <summary>Colour of that rectangle.</summary>
    public Color4 BackgroundColor { get; init; }

    /// <summary>Space between the text and the edge of its background, in pixels.</summary>
    public Vector2 Padding { get; init; }
}
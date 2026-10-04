using Stride.Engine;
using Stride.Engine.Design;
using Stride.Graphics;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// Draws text that lives in the scene: positioned by the entity's transform, scaled by perspective,
/// and occluded by geometry standing in front of it.
/// </summary>
/// <remarks>
/// <para>
/// This is the counterpart to <see cref="EntityTextComponent"/>, which is screen-space: that one
/// projects an anchor point and draws flat pixels on top of everything, while this one draws the text
/// as part of the world. Use that one for HUDs and labels that must always be readable, and this one
/// when the text should look like it belongs in the scene.
/// </para>
/// <para>
/// <see cref="WorldTextProcessor"/> adds a <see cref="WorldTextRenderer"/> to the scene's compositor when none is there.
/// </para>
/// <example>
/// A label standing on the ground, facing the camera:
/// <code>
/// entity.Add(new WorldTextComponent
/// {
///     Text = "Spawn",
///     Height = 0.4f,
///     Anchor = TextAnchor.BottomCenter,
///     Billboard = true
/// });
/// </code>
/// </example>
/// </remarks>
[DefaultEntityComponentProcessor(typeof(WorldTextProcessor), ExecutionMode = ExecutionMode.All)]
[AllowMultipleComponents]
[DataContract("WorldTextComponent")]
[Display("World Text", Expand = ExpandRule.Once)]
[ComponentCategory("Text")]
public class WorldTextComponent : EntityComponent
{
    /// <summary>
    /// Gets or sets the text to draw.
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// Gets or sets the font. Leave <see langword="null"/> to use Stride's default font.
    /// </summary>
    public SpriteFont? Font { get; set; }

    /// <summary>
    /// Gets or sets the size the glyphs are rasterised at, in pixels. Defaults to 32.
    /// </summary>
    /// <remarks>
    /// This is sharpness, not size on screen - <see cref="Height"/> decides how big the text is in the
    /// world. Raise it when text is viewed close up and looks soft; it costs glyph cache space. On a
    /// scaled display the glyphs are rasterised that much larger while <see cref="AutoScale"/> is on,
    /// because the same world height covers that many more pixels there.
    /// </remarks>
    public float FontSize { get; set; } = 32;

    /// <summary>
    /// Gets or sets whether the rasterisation size follows the display's scale, so text on a 150%
    /// display is drawn from glyphs 1.5x the size and stays as sharp as it is on a 100% monitor.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Sharpness only: the text is <see cref="Height"/> tall in the world whatever the display, so
    /// nothing moves or resizes. The factor comes from <c>DisplayScale</c>, shared with everything else
    /// in the toolkit that follows the display, and only applies when the process is DPI aware.
    /// <see cref="GlowSize"/> is in font pixels and scales along, so the glow keeps its reach.
    /// </remarks>
    public bool AutoScale { get; set; } = true;

    /// <summary>
    /// Gets or sets the height of the text in world units. Defaults to 1.
    /// </summary>
    /// <remarks>
    /// The text is scaled so the whole block - every line of it - is this tall, then scaled again by
    /// the entity's transform. Expressing it this way means changing <see cref="FontSize"/> for
    /// sharpness does not also change how big the text appears.
    /// </remarks>
    public float Height { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the text colour. Defaults to <see cref="Color.White"/>.
    /// </summary>
    /// <remarks>
    /// Its alpha counts: it is how transparent this text is by nature, and it multiplies with
    /// <see cref="Opacity"/> and any distance fade. Set it when a label is meant to sit faintly in the
    /// scene; use <see cref="Opacity"/> to fade the whole thing - glow included - from code.
    /// </remarks>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets an overall opacity from 0 to 1. Defaults to 1.
    /// </summary>
    /// <remarks>
    /// A dimmer over everything the component draws: the letters, at <see cref="TextColor"/>'s own
    /// alpha, and the <see cref="GlowColor"/> behind them. Distance fading drives this same dimmer.
    /// </remarks>
    public float Opacity { get; set; } = 1f;

    /// <summary>
    /// Gets or sets which point of the text sits on the entity's position. Defaults to
    /// <see cref="TextAnchor.MiddleCenter"/>.
    /// </summary>
    /// <remarks>
    /// Centred by default, unlike the screen-space component: world text is usually placed <em>at</em>
    /// a thing rather than hung off the corner of one.
    /// </remarks>
    public TextAnchor Anchor { get; set; } = TextAnchor.MiddleCenter;

    /// <summary>
    /// Gets or sets how the lines of a multi-line text sit relative to one another.
    /// </summary>
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;

    /// <summary>
    /// Gets or sets a local-space offset applied before the entity's rotation.
    /// </summary>
    public Vector3 Offset { get; set; }

    /// <summary>
    /// Gets or sets whether the text turns to face the camera. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// When <see langword="false"/> the text keeps the entity's own orientation, so it can be laid flat
    /// on a floor or fixed to a wall and will foreshorten and disappear edge-on like any other surface.
    /// </remarks>
    public bool Billboard { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the text stays upright when billboarding. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Upright text turns about the world Y axis only, so it never rolls when the camera tilts - which
    /// is what a label standing in a scene should do. Set to <see langword="false"/> to face the camera
    /// squarely from any angle, including from directly above.
    /// </remarks>
    public bool KeepUpright { get; set; } = true;

    /// <summary>
    /// Gets or sets whether scene geometry in front of the text hides it. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// This is the whole point of world text as opposed to the screen-space kind. Turning it off makes
    /// the text draw over everything while still being positioned and scaled in the world.
    /// </remarks>
    public bool DepthTest { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the text is drawn at all. Defaults to <see langword="true"/>.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the colour of a soft glow drawn behind the letters. Leave it fully transparent -
    /// an alpha of zero, the default - for no glow. Its alpha is the glow's strength.
    /// </summary>
    /// <remarks>
    /// A HUD or a neon sign: light text on a glow of a deeper hue reads as lit rather than painted.
    /// The glow is the text itself drawn again in this colour, offset in a ring around the letters,
    /// so it scales with the text and follows every glyph exactly.
    /// </remarks>
    public Color GlowColor { get; set; }

    /// <summary>
    /// Gets or sets how far the glow reaches from the letters, in font pixels at <see cref="FontSize"/>.
    /// Defaults to 0. A tenth of the font size is a crisp halo; a quarter is a bloom.
    /// </summary>
    public float GlowSize { get; set; }

    /// <summary>
    /// Gets or sets the distance from the camera at which the text starts fading out, in world units.
    /// 0, the default, is no fade. Needs <see cref="MaxDistance"/> as well: the text fades from fully
    /// opaque at this distance to invisible at that one.
    /// </summary>
    public float FadeStartDistance { get; set; }

    /// <summary>
    /// Gets or sets the distance beyond which the text is not drawn, in world units. 0, the default,
    /// is no limit.
    /// </summary>
    public float MaxDistance { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldTextComponent"/> class.
    /// </summary>
    public WorldTextComponent() { }
}
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Graphics;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// Draws a line of screen-space text for the entity it is attached to, without using Stride's UI
/// system.
/// </summary>
/// <remarks>
/// <para>
/// Add <see cref="EntityTextRenderer"/> to the graphics compositor
/// for anything to appear - the component records what to draw, the renderer draws it.
/// </para>
/// <para>
/// The text can follow its entity through the world, sit at a fixed pixel position, or anchor to a
/// corner of the window; see <see cref="PositionMode"/>. Where the text sits relative to that point
/// is <see cref="Anchor"/>.
/// </para>
/// <example>
/// A label floating above an object:
/// <code>
/// entity.Add(new EntityTextComponent
/// {
///     Text = "Player",
///     Anchor = TextAnchor.BottomCenter,
///     Offset = new Vector2(0, -12),
///     EnableShadow = true
/// });
/// </code>
/// A score in the top-left that survives the window being resized:
/// <code>
/// entity.Add(new EntityTextComponent
/// {
///     Text = "Score: 0",
///     PositionMode = TextPositionMode.Anchored,
///     ScreenAnchor = DisplayPosition.TopLeft,
///     Offset = new Vector2(16, 16),
///     FontSize = 20
/// });
/// </code>
/// </example>
/// </remarks>
[DefaultEntityComponentProcessor(typeof(EntityTextProcessor), ExecutionMode = ExecutionMode.All)]
// Several labels on one entity is a normal thing to want - a name and a subtitle, a score and its
// multiplier - and text is presentation rather than identity, so nothing is ambiguous about having
// two. Without this Stride rejects the second with "Cannot add a component of type ... multiple
// times". LightComponent and ScriptComponent opt in the same way.
[AllowMultipleComponents]
// DataContract is what makes the component usable from Game Studio at all: without it the editor
// cannot clone the component to the game side and reports "No serializer available for type".
[DataContract("EntityTextComponent")]
// Editor as well as runtime: the processor puts the renderer on the scene's compositor itself, so a
// label placed in Game Studio is drawn in the viewport and in a game that never called
// AddEntityTextRenderer.
[Display("Entity Text", Expand = ExpandRule.Once)]
[ComponentCategory("Text")]
public class EntityTextComponent : EntityComponent
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
    /// Gets or sets the size the glyphs are rasterised at, in pixels on a 100% display. Defaults to 18.
    /// </summary>
    /// <remarks>
    /// Animate <see cref="Scale"/> rather than this. Changing the font size re-rasterises the glyphs
    /// and re-measures the text every frame it changes; scaling does neither. On a scaled display
    /// the glyphs are rasterised that much larger while <see cref="AutoScale"/> is on, so they stay
    /// sharp rather than being stretched.
    /// </remarks>
    public float FontSize { get; set; } = 18;

    /// <summary>
    /// Gets or sets whether everything measured in pixels - <see cref="FontSize"/>, <see cref="Offset"/>,
    /// <see cref="ScreenPosition"/>, <see cref="Padding"/> and <see cref="ShadowOffset"/> - follows
    /// the display's scale, so the text is the same size to the eye on a 150% laptop as on a 100%
    /// monitor. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// The factor comes from <c>DisplayScale</c>, shared with everything else in the toolkit that
    /// draws in pixels, and is re-read when the window moves to another monitor. Turn it off to get
    /// exactly the pixels asked for - a screenshot at a known size, or a game applying its own
    /// UI-scale setting through <c>DisplayScale.Override</c> and nothing else should compound it.
    /// </remarks>
    public bool AutoScale { get; set; } = true;

    /// <summary>
    /// Gets or sets a multiplier applied to the drawn size. Defaults to 1.
    /// </summary>
    /// <remarks>
    /// This is the cheap way to make text pop or shrink over time, because it does not touch the
    /// glyph cache. Scaling happens about <see cref="Anchor"/>, so a centred text grows evenly rather
    /// than drifting to one side.
    /// </remarks>
    public float Scale { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the clockwise rotation of the text, in radians. Defaults to 0.
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// Gets or sets the text colour. Defaults to <see cref="Color.White"/>.
    /// </summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets an overall opacity from 0 to 1, applied on top of the text, shadow and background
    /// colours. Defaults to 1.
    /// </summary>
    /// <remarks>
    /// Fading with this leaves the configured colours alone, so a fade can be restarted without
    /// having to remember what the colours were before it began.
    /// </remarks>
    public float Opacity { get; set; } = 1f;

    /// <summary>
    /// Gets or sets how the position to draw at is decided. Defaults to
    /// <see cref="TextPositionMode.World"/>, which follows the entity.
    /// </summary>
    public TextPositionMode PositionMode { get; set; } = TextPositionMode.World;

    /// <summary>
    /// Gets or sets the pixel position used when <see cref="PositionMode"/> is
    /// <see cref="TextPositionMode.Screen"/>, measured from the top-left of the window.
    /// </summary>
    public Vector2 ScreenPosition { get; set; }

    /// <summary>
    /// Gets or sets the window corner used when <see cref="PositionMode"/> is
    /// <see cref="TextPositionMode.Anchored"/>. Defaults to the top-left.
    /// </summary>
    /// <remarks>
    /// <see cref="DisplayPosition.Custom"/> and
    /// <see cref="DisplayPosition.None"/> are treated as the top-left; use
    /// <see cref="TextPositionMode.Screen"/> for an explicit position and <see cref="IsVisible"/> to
    /// hide the text.
    /// </remarks>
    public DisplayPosition ScreenAnchor { get; set; } = DisplayPosition.TopLeft;

    /// <summary>
    /// Gets or sets a pixel offset applied after the position is resolved, in every position mode.
    /// </summary>
    /// <remarks>
    /// In <see cref="TextPositionMode.Anchored"/> this is the margin from the chosen corner, and it
    /// always points inwards - a positive offset moves the text away from its corner rather than off
    /// the screen.
    /// </remarks>
    public Vector2 Offset { get; set; }

    /// <summary>
    /// Gets or sets which point of the text is placed on the resolved position. Defaults to
    /// <see cref="TextAnchor.TopLeft"/>.
    /// </summary>
    public TextAnchor Anchor { get; set; } = TextAnchor.TopLeft;

    /// <summary>
    /// Gets or sets how the lines of a multi-line text sit relative to one another. Defaults to
    /// <see cref="TextAlignment.Left"/>.
    /// </summary>
    /// <remarks>
    /// This has no effect on single-line text, where every value produces the same result. To centre
    /// a label on its position use <see cref="Anchor"/>.
    /// </remarks>
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;

    /// <summary>
    /// Gets or sets the draw order. Higher values are drawn on top. Defaults to 0.
    /// </summary>
    public float LayerDepth { get; set; }

    /// <summary>
    /// Gets or sets whether a drop shadow is drawn behind the text. Defaults to <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// Worth turning on for anything drawn over a scene rather than over a flat background: it costs
    /// one extra draw of the same string and is usually the difference between text that is readable
    /// against any colour underneath it and text that disappears against some of them.
    /// </remarks>
    public bool EnableShadow { get; set; }

    /// <summary>
    /// Gets or sets the shadow colour. Defaults to half-transparent black.
    /// </summary>
    public Color ShadowColor { get; set; } = new(0, 0, 0, 128);

    /// <summary>
    /// Gets or sets how far the shadow is offset from the text, in pixels. Defaults to one pixel
    /// right and down.
    /// </summary>
    public Vector2 ShadowOffset { get; set; } = new(1, 1);

    /// <summary>
    /// Gets or sets whether the text is drawn with an outline around every glyph. Defaults to
    /// <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// An outline in a contrasting colour keeps text readable over anything, including a
    /// background of its own colour, where a shadow on one side is not enough. It costs eight extra
    /// draws of the same string.
    /// </remarks>
    public bool EnableOutline { get; set; }

    /// <summary>
    /// Gets or sets the outline colour. Defaults to white.
    /// </summary>
    public Color OutlineColor { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets how far the outline reaches beyond the glyphs, in pixels. Defaults to 1.
    /// </summary>
    public float OutlineWidth { get; set; } = 1f;

    /// <summary>
    /// Gets or sets whether a filled rectangle is drawn behind the text. Defaults to <see langword="false"/>.
    /// </summary>
    public bool EnableBackground { get; set; }

    /// <summary>
    /// Gets or sets the background colour. Leave <see langword="null"/> for the renderer's default.
    /// </summary>
    public Color4? BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the padding between the text and the edge of its background, in pixels.
    /// </summary>
    public Vector2 Padding { get; set; } = new(2, 2);

    /// <summary>
    /// Gets or sets whether the text is drawn at all. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Prefer this over removing and re-adding the component, which throws away the cached text
    /// measurement along with it.
    /// </remarks>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the distance from the camera at which the text starts fading out, in world units.
    /// 0, the default, is no fade.
    /// </summary>
    /// <remarks>
    /// Only applies in <see cref="TextPositionMode.World"/>. Requires <see cref="MaxDistance"/> to be
    /// set as well; the text fades from fully opaque at this distance to invisible at that one.
    /// </remarks>
    public float FadeStartDistance { get; set; }

    /// <summary>
    /// Gets or sets the distance from the camera beyond which the text is not drawn, in world units.
    /// 0, the default, is no limit.
    /// </summary>
    /// <remarks>
    /// Only applies in <see cref="TextPositionMode.World"/>. Useful on its own as a cutoff, without
    /// <see cref="FadeStartDistance"/>, when labels should simply stop rather than fade.
    /// </remarks>
    public float MaxDistance { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityTextComponent"/> class.
    /// </summary>
    public EntityTextComponent() { }
}
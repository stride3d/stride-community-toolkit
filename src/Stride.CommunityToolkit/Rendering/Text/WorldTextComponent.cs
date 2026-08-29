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
/// Add <see cref="Stride.CommunityToolkit.Renderers.WorldTextRenderer"/> to the graphics compositor
/// for anything to appear.
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
[DefaultEntityComponentProcessor(typeof(WorldTextProcessor), ExecutionMode = ExecutionMode.Runtime)]
[AllowMultipleComponents]
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
    /// world. Raise it when text is viewed close up and looks soft; it costs glyph cache space.
    /// </remarks>
    public float FontSize { get; set; } = 32;

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
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets an overall opacity from 0 to 1. Defaults to 1.
    /// </summary>
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
    /// Gets or sets the distance from the camera at which the text starts fading out, in world units.
    /// </summary>
    public float? FadeStartDistance { get; set; }

    /// <summary>
    /// Gets or sets the distance beyond which the text is not drawn, in world units.
    /// </summary>
    public float? MaxDistance { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldTextComponent"/> class.
    /// </summary>
    public WorldTextComponent() { }
}
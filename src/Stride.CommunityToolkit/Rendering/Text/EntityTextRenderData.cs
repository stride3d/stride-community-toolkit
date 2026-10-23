using Stride.Engine;
using Stride.Graphics;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// Per-component state the text renderer keeps between frames.
/// </summary>
/// <remarks>
/// This exists to hold the measured size of the text, which is needed to anchor it and to size its
/// background, and which is far too expensive to recompute every frame for every label. Holding it
/// here rather than in a dictionary inside the renderer is what gives it a correct lifetime: the
/// processor creates one of these when the component is added and drops it when the component goes
/// away, so the measurement cannot outlive what it describes.
/// </remarks>
/// <param name="component">The component being drawn.</param>
public class EntityTextRenderData(EntityTextComponent component)
{
    private readonly TextMeasurement _measurement = new();

    /// <summary>Gets the component being drawn.</summary>
    public EntityTextComponent Component { get; } = component;

    /// <summary>
    /// Gets the entity the component is attached to.
    /// </summary>
    /// <remarks>
    /// Read from the component rather than stored alongside it. A component knows its own owner for
    /// as long as it is attached, and this data only exists while it is - the processor creates it
    /// when the component arrives and drops it when it leaves - so keeping a second copy would add a
    /// way for the two to disagree without adding anything.
    /// </remarks>
    public Entity Entity => Component.Entity;

    /// <summary>
    /// Returns the size of the component's text in pixels at <see cref="EntityTextComponent.FontSize"/>,
    /// measuring it only when the text, size or font has actually changed since the last measurement.
    /// </summary>
    /// <param name="spriteBatch">The batch used to measure.</param>
    /// <param name="font">The font the text will be drawn with.</param>
    /// <returns>The width and height of the text, in pixels, before <see cref="EntityTextComponent.Scale"/>.</returns>
    public Vector2 GetMeasuredSize(SpriteBatch spriteBatch, SpriteFont font) => GetMeasuredSize(spriteBatch, font, Component.FontSize);

    /// <summary>
    /// Returns the size of the component's text in pixels at a given font size - the component's
    /// own size times the display's scale, when the renderer is following it - measuring it only
    /// when the text, size or font has actually changed since the last measurement.
    /// </summary>
    /// <param name="spriteBatch">The batch used to measure.</param>
    /// <param name="font">The font the text will be drawn with.</param>
    /// <param name="fontSize">The size the glyphs are rasterised at, in pixels.</param>
    /// <returns>The width and height of the text, in pixels, before <see cref="EntityTextComponent.Scale"/>.</returns>
    public Vector2 GetMeasuredSize(SpriteBatch spriteBatch, SpriteFont font, float fontSize)
        => _measurement.Measure(spriteBatch, font, Component.Text, fontSize);
}
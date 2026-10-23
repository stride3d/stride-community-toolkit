using Stride.Engine;
using Stride.Graphics;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// Per-component state the world text renderer keeps between frames.
/// </summary>
/// <remarks>
/// Holds the measured size of the text, which is needed to anchor it and to work out the scale that
/// makes it <see cref="WorldTextComponent.Height"/> tall. Measuring is far too expensive to repeat
/// every frame for every label, and holding the result here rather than in a dictionary inside the
/// renderer gives it a correct lifetime - the processor creates it with the component and drops it
/// when the component goes away.
/// </remarks>
/// <param name="component">The component being drawn.</param>
public class WorldTextRenderData(WorldTextComponent component)
{
    private readonly TextMeasurement _measurement = new();

    /// <summary>Gets the component being drawn.</summary>
    public WorldTextComponent Component { get; } = component;

    /// <summary>Gets the entity the component is attached to.</summary>
    public Entity Entity => Component.Entity;

    /// <summary>
    /// Returns the size of the text in font pixels at <see cref="WorldTextComponent.FontSize"/>,
    /// measuring only when the text, size or font changed.
    /// </summary>
    /// <param name="spriteBatch">The batch used to measure.</param>
    /// <param name="font">The font the text will be drawn with.</param>
    /// <returns>The width and height of the text, in font pixels.</returns>
    public Vector2 GetMeasuredSize(SpriteBatch spriteBatch, SpriteFont font) => GetMeasuredSize(spriteBatch, font, Component.FontSize);

    /// <summary>
    /// Returns the size of the text in font pixels at a given font size - the component's own size
    /// times the display's scale, when the renderer is following it - measuring only when the text,
    /// size or font changed.
    /// </summary>
    /// <param name="spriteBatch">The batch used to measure.</param>
    /// <param name="font">The font the text will be drawn with.</param>
    /// <param name="fontSize">The size the glyphs are rasterised at, in pixels.</param>
    /// <returns>The width and height of the text, in font pixels.</returns>
    public Vector2 GetMeasuredSize(SpriteBatch spriteBatch, SpriteFont font, float fontSize)
        => _measurement.Measure(spriteBatch, font, Component.Text, fontSize);
}
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
    private string? _measuredText;
    private float _measuredFontSize;
    private SpriteFont? _measuredFont;
    private Vector2 _measuredSize;

    /// <summary>Gets the component being drawn.</summary>
    public WorldTextComponent Component { get; } = component;

    /// <summary>Gets the entity the component is attached to.</summary>
    public Entity Entity => Component.Entity;

    /// <summary>
    /// Returns the size of the text in font pixels, measuring only when the text, size or font changed.
    /// </summary>
    /// <param name="spriteBatch">The batch used to measure.</param>
    /// <param name="font">The font the text will be drawn with.</param>
    /// <returns>The width and height of the text, in font pixels.</returns>
    public Vector2 GetMeasuredSize(SpriteBatch spriteBatch, SpriteFont font)
    {
        if (_measuredText == Component.Text && _measuredFontSize == Component.FontSize && _measuredFont == font)
        {
            return _measuredSize;
        }

        _measuredSize = spriteBatch.MeasureString(font, Component.Text, Component.FontSize);
        _measuredText = Component.Text;
        _measuredFontSize = Component.FontSize;
        _measuredFont = font;

        return _measuredSize;
    }
}
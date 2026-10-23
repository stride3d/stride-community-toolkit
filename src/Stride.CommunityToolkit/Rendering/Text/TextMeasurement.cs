using Stride.Graphics;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// The measured size of one string at one font and size, measured again only when any of the three
/// changes: what the render data of both text components keeps between frames.
/// </summary>
internal sealed class TextMeasurement
{
    private string? _text;
    private float _fontSize;
    private SpriteFont? _font;
    private Vector2 _size;

    /// <summary>
    /// The size of <paramref name="text"/> in pixels at <paramref name="fontSize"/>, from the cache when
    /// nothing has changed since the last call.
    /// </summary>
    internal Vector2 Measure(SpriteBatch spriteBatch, SpriteFont font, string text, float fontSize)
    {
        if (_text == text && _fontSize == fontSize && _font == font) return _size;

        _size = spriteBatch.MeasureString(font, text, fontSize);
        _text = text;
        _fontSize = fontSize;
        _font = font;

        return _size;
    }
}
using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// A rectangle in the HUD's world units with Y up: the space a widget is given to draw in. The
/// methods cut it into smaller rectangles, which is all the layout a widget needs.
/// </summary>
public readonly record struct HudRect(float Left, float Bottom, float Width, float Height)
{
    public float Right => Left + Width;

    public float Top => Bottom + Height;

    public Vector2 Center => new(Left + Width / 2f, Bottom + Height / 2f);

    public Vector2 Size => new(Width, Height);

    public static HudRect Centered(Vector2 center, Vector2 size) => new(center.X - size.X / 2f, center.Y - size.Y / 2f, size.X, size.Y);

    /// <summary>The rectangle shrunk by the same amount on every side.</summary>
    public HudRect Inset(float amount) => Inset(amount, amount);

    /// <summary>The rectangle shrunk by one amount left and right and another top and bottom.</summary>
    public HudRect Inset(float horizontal, float vertical) => new(Left + horizontal, Bottom + vertical, Width - 2f * horizontal, Height - 2f * vertical);

    /// <summary>A strip of the given height along the top.</summary>
    public HudRect TakeTop(float height) => new(Left, Top - height, Width, height);

    /// <summary>What is left under a strip of the given height.</summary>
    public HudRect DropTop(float height) => new(Left, Bottom, Width, Height - height);

    /// <summary>A strip of the given width along the left.</summary>
    public HudRect TakeLeft(float width) => new(Left, Bottom, width, Height);

    /// <summary>What is left beside a strip of the given width.</summary>
    public HudRect DropLeft(float width) => new(Left + width, Bottom, Width - width, Height);

    /// <summary>One of <paramref name="count"/> equal columns, counted from the left.</summary>
    public HudRect Column(int index, int count, float gap = 0f)
    {
        var width = (Width - gap * (count - 1)) / count;

        return new(Left + index * (width + gap), Bottom, width, Height);
    }

    /// <summary>One of <paramref name="count"/> equal rows, counted from the top.</summary>
    public HudRect Row(int index, int count, float gap = 0f)
    {
        var height = (Height - gap * (count - 1)) / count;

        return new(Left, Top - height - index * (height + gap), Width, height);
    }

    /// <summary>A point inside the rectangle, as fractions from the bottom left.</summary>
    public Vector2 At(float x, float y) => new(Left + Width * x, Bottom + Height * y);
}
namespace E03_2D_HUD;

/// <summary>Something that draws itself inside the rectangle it is given, so it fits any column.</summary>
public abstract class HudWidget
{
    public abstract void Draw(HudCanvas canvas, HudRect bounds);

    /// <summary>The widget with a fixed width in a row, or a fixed height in a column.</summary>
    public HudSlot Sized(float size) => new(this, size);
}

/// <summary>A widget and the room it takes in a row or a column.</summary>
/// <param name="Widget">The widget.</param>
/// <param name="Size">Its width in a row or its height in a column. 0 shares what the sized ones leave.</param>
public readonly record struct HudSlot(HudWidget Widget, float Size = 0f)
{
    public static implicit operator HudSlot(HudWidget widget) => new(widget);
}

/// <summary>Widgets side by side, left to right, with the same gap between them.</summary>
public sealed class HudRow(float gap, params HudSlot[] slots) : HudWidget
{
    public override void Draw(HudCanvas canvas, HudRect bounds)
    {
        var left = bounds.Left;
        var shared = HudStack.Shared(slots, bounds.Width, gap);

        foreach (var slot in slots)
        {
            var width = slot.Size > 0f ? slot.Size : shared;

            HudStack.Draw(canvas, slot.Widget, new HudRect(left, bounds.Bottom, width, bounds.Height));

            left += width + gap;
        }
    }
}

/// <summary>Widgets stacked top down, with the same gap between them.</summary>
public sealed class HudColumn(float gap, params HudSlot[] slots) : HudWidget
{
    public override void Draw(HudCanvas canvas, HudRect bounds)
    {
        var top = bounds.Top;
        var shared = HudStack.Shared(slots, bounds.Height, gap);

        foreach (var slot in slots)
        {
            var height = slot.Size > 0f ? slot.Size : shared;

            HudStack.Draw(canvas, slot.Widget, new HudRect(bounds.Left, top - height, bounds.Width, height));

            top -= height + gap;
        }
    }
}

/// <summary>What a row and a column have in common.</summary>
internal static class HudStack
{
    /// <summary>The size each unsized slot gets: what the sized slots and the gaps leave, shared equally.</summary>
    public static float Shared(HudSlot[] slots, float available, float gap)
    {
        var unsized = slots.Count(slot => slot.Size <= 0f);

        if (unsized == 0) return 0f;

        return (available - slots.Sum(slot => slot.Size) - gap * (slots.Length - 1)) / unsized;
    }

    /// <summary>Draws a widget. A row or a column passes through; anything else is next in the fade-in.</summary>
    public static void Draw(HudCanvas canvas, HudWidget widget, HudRect bounds)
    {
        if (widget is not (HudRow or HudColumn)) canvas.NextWidget();

        widget.Draw(canvas, bounds);
    }
}
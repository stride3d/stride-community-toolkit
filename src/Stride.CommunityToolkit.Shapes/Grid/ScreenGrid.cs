using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// The grid of the window: pixels counted from the top left corner, with Y pointing down. These
/// are the coordinates a <see cref="ShapeBatch"/> draws in with <see cref="ShapeBatch.Screen"/> on.
/// </summary>
internal static class ScreenGrid
{
    internal static void Draw(GridCanvas canvas)
    {
        var settings = canvas.Settings;
        var size = canvas.Overlay.ScreenSize;
        var major = MathF.Max(settings.ScreenStep, 1f);
        var divisions = Math.Max(settings.ScreenMinorDivisions, 1);
        var minor = major / divisions;

        // The last line that fits in the window, across and down. The first is always line 0, the edge
        var lastX = GridSteps.Range(0f, size.X, minor).Last;
        var lastY = GridSteps.Range(0f, size.Y, minor).Last;

        DrawLines(canvas, size, minor, divisions, new Int2(lastX, lastY));

        if (settings.ShowNumbers) DrawNumbers(canvas, size, minor, divisions, new Int2(lastX, lastY));
    }

    private static void DrawLines(GridCanvas canvas, Vector2 size, float minor, int divisions, Int2 last)
    {
        var batch = canvas.Overlay;

        // Line 0 is the axis, so the lines start at 1
        for (var i = 1; i <= last.X; i++) canvas.Line(batch, new Vector3(i * minor, 0f, 0f), new Vector3(i * minor, size.Y, 0f), i % divisions == 0);
        for (var i = 1; i <= last.Y; i++) canvas.Line(batch, new Vector3(0f, i * minor, 0f), new Vector3(size.X, i * minor, 0f), i % divisions == 0);

        // The axes run along the top and the left edge. A pixel in, so their whole width shows
        GridCanvas.Axis(batch, new Vector3(0f, 1f, 0f), new Vector3(size.X, 1f, 0f), ReferenceGrid.AxisXColor);
        GridCanvas.Axis(batch, new Vector3(1f, 0f, 0f), new Vector3(1f, size.Y, 0f), ReferenceGrid.AxisYColor);
    }

    private static void DrawNumbers(GridCanvas canvas, Vector2 size, float minor, int divisions, Int2 last)
    {
        const float margin = GridCanvas.EdgeMargin;

        var major = minor * divisions;
        var fontSize = canvas.Settings.FontSize;

        // Along the top edge, each number just right of its line
        for (var i = divisions; i <= last.X; i += divisions)
        {
            canvas.Label(GridSteps.Format(i * minor, major), new Vector2(i * minor + 3f, margin), TextAnchor.TopLeft, ReferenceGrid.AxisXColor);
        }

        // Down the left edge, each number just above its line
        for (var i = divisions; i <= last.Y; i += divisions)
        {
            canvas.Label(GridSteps.Format(i * minor, major), new Vector2(margin, i * minor - 2f), TextAnchor.BottomLeft, ReferenceGrid.AxisYColor);
        }

        // The origin in its corner, and each letter at the far end of its axis
        canvas.Label("0", new Vector2(margin, margin), TextAnchor.TopLeft, Color.White);
        canvas.Label("X", new Vector2(size.X - margin, margin + fontSize + 4f), TextAnchor.TopRight, ReferenceGrid.AxisXColor);
        canvas.Label("Y", new Vector2(margin + fontSize * 2.5f, size.Y - margin), TextAnchor.BottomLeft, ReferenceGrid.AxisYColor);
    }
}
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// The world grid of a 2D scene: lines in the XY plane, over whatever the camera sees.
/// </summary>
internal static class FlatGrid
{
    // A little behind the plane Z = 0, so flat shapes drawn there hide the grid
    private const float Depth = -0.01f;

    internal static void Draw(GridCanvas canvas, in GridView view)
    {
        // What the window shows of the plane
        if (!view.OnPlaneZ(Vector2.Zero, out var a) || !view.OnPlaneZ(Vector2.One, out var b)) return;

        var settings = canvas.Settings;
        var min = Vector2.Min(a, b);
        var max = Vector2.Max(a, b);
        var centre = new Vector3((min + max) * 0.5f, 0f);
        var major = settings.MajorStep > 0f ? settings.MajorStep : GridSteps.Nice(GridSteps.TargetMajorPixels * view.WorldPerPixel(centre));
        var divisions = settings.MinorDivisions > 0 ? settings.MinorDivisions : GridSteps.Divisions(major);
        var layout = new Layout(min, max, major, divisions);

        DrawLines(canvas, layout);

        if (settings.ShowNumbers) DrawNumbers(canvas, view, layout);
    }

    private static void DrawLines(GridCanvas canvas, in Layout layout)
    {
        var batch = canvas.World;
        var (min, max, minor, divisions) = (layout.Min, layout.Max, layout.Minor, layout.Divisions);

        for (var i = layout.FirstX; i <= layout.LastX; i++)
        {
            if (layout.Skips(i)) continue;

            canvas.Line(batch, new Vector3(i * minor, min.Y, Depth), new Vector3(i * minor, max.Y, Depth), i % divisions == 0);
        }

        for (var i = layout.FirstY; i <= layout.LastY; i++)
        {
            if (layout.Skips(i)) continue;

            canvas.Line(batch, new Vector3(min.X, i * minor, Depth), new Vector3(max.X, i * minor, Depth), i % divisions == 0);
        }

        // The axes last, so they lie over the lines
        GridCanvas.Axis(batch, new Vector3(min.X, 0f, Depth), new Vector3(max.X, 0f, Depth), ReferenceGrid.AxisXColor);
        GridCanvas.Axis(batch, new Vector3(0f, min.Y, Depth), new Vector3(0f, max.Y, Depth), ReferenceGrid.AxisYColor);
    }

    private static void DrawNumbers(GridCanvas canvas, in GridView view, in Layout layout)
    {
        var fontSize = canvas.Settings.FontSize;

        // Where the axes are on the screen, held inside the window: an axis that has been panned
        // out of view leaves its numbers along the nearest edge
        view.Project(Vector3.Zero, out var origin);

        var axisY = MathUtil.Clamp(origin.Y, GridCanvas.EdgeMargin, view.Size.Y - fontSize - GridCanvas.EdgeMargin * 2f);
        var axisX = MathUtil.Clamp(origin.X, GridCanvas.EdgeMargin, view.Size.X - fontSize * 4f);

        for (var i = layout.FirstX; i <= layout.LastX; i++)
        {
            if (i == 0 || i % layout.Divisions != 0 || !view.Project(new Vector3(i * layout.Minor, 0f, 0f), out var at)) continue;

            canvas.Label(GridSteps.Format(i * layout.Minor, layout.Major), new Vector2(at.X + 3f, axisY + 2f), TextAnchor.TopLeft, ReferenceGrid.AxisXColor);
        }

        for (var i = layout.FirstY; i <= layout.LastY; i++)
        {
            if (i == 0 || i % layout.Divisions != 0 || !view.Project(new Vector3(0f, i * layout.Minor, 0f), out var at)) continue;

            canvas.Label(GridSteps.Format(i * layout.Minor, layout.Major), new Vector2(axisX + 4f, at.Y - 2f), TextAnchor.BottomLeft, ReferenceGrid.AxisYColor);
        }

        canvas.Label("0", new Vector2(axisX + 4f, axisY + 2f), TextAnchor.TopLeft, Color.White);
        canvas.Label("X", new Vector2(view.Size.X - GridCanvas.EdgeMargin, axisY - 2f), TextAnchor.BottomRight, ReferenceGrid.AxisXColor);
        canvas.Label("Y", new Vector2(axisX - 4f, GridCanvas.EdgeMargin), TextAnchor.TopRight, ReferenceGrid.AxisYColor);
    }

    /// <summary>Which lines a frame draws: the part of the plane in view, cut into steps.</summary>
    private readonly struct Layout
    {
        // Whether there are too many lines in view to draw the minor ones
        private readonly bool _majorOnly;

        internal Layout(Vector2 min, Vector2 max, float major, int divisions)
        {
            Min = min;
            Max = max;
            Major = major;
            Divisions = divisions;
            Minor = major / divisions;

            (FirstX, LastX) = GridSteps.Range(min.X, max.X, Minor);
            (FirstY, LastY) = GridSteps.Range(min.Y, max.Y, Minor);
            _majorOnly = LastX - FirstX > GridSteps.MaximumLines || LastY - FirstY > GridSteps.MaximumLines;
        }

        internal Vector2 Min { get; }

        internal Vector2 Max { get; }

        internal float Major { get; }

        internal float Minor { get; }

        internal int Divisions { get; }

        internal int FirstX { get; }

        internal int LastX { get; }

        internal int FirstY { get; }

        internal int LastY { get; }

        /// <summary>Whether line <paramref name="index"/> is left out: the axis, drawn separately, or a minor line among too many.</summary>
        internal bool Skips(int index) => index == 0 || (_majorOnly && index % Divisions != 0);
    }
}
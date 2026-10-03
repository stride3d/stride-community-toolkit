using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// The world grid of a 3D scene: a square on the ground plane XZ around the origin, with a Y axis
/// standing in the middle.
/// </summary>
internal static class GroundGrid
{
    // A little above the plane Y = 0, so the grid does not fight a floor at that height
    private const float Lift = 0.004f;

    internal static void Draw(GridCanvas canvas, in GridView view)
    {
        var settings = canvas.Settings;
        var half = MathF.Max(settings.Extent, 0.01f) * 0.5f;
        var major = settings.MajorStep > 0f ? settings.MajorStep : AutomaticStep(view, half);
        var layout = new Layout(half, major, settings.MinorDivisions > 0 ? settings.MinorDivisions : GridSteps.Divisions(major));

        DrawLines(canvas, layout);

        if (settings.ShowNumbers) DrawNumbers(canvas, view, layout);
    }

    /// <summary>The step for where the camera looks at the ground, held to steps that suit the square.</summary>
    private static float AutomaticStep(in GridView view, float half)
    {
        var focus = view.OnPlaneY(new Vector2(0.5f, 0.5f), out var looked)
            ? new Vector3(MathUtil.Clamp(looked.X, -half, half), 0f, MathUtil.Clamp(looked.Y, -half, half))
            : Vector3.Zero;

        var step = GridSteps.Nice(GridSteps.TargetMajorPixels * view.WorldPerPixel(focus));

        return MathUtil.Clamp(step, GridSteps.Nice(half / 50f), GridSteps.Nice(half));
    }

    private static void DrawLines(GridCanvas canvas, in Layout layout)
    {
        var batch = canvas.World;
        var half = layout.Half;
        var majorOnly = layout.Last - layout.First > GridSteps.MaximumLines;

        for (var i = layout.First; i <= layout.Last; i++)
        {
            // The axes are drawn separately, and the minor lines are left out when there are too many
            if (i == 0 || (majorOnly && !layout.IsMajor(i))) continue;

            var at = i * layout.Minor;

            canvas.Line(batch, new Vector3(at, Lift, -half), new Vector3(at, Lift, half), layout.IsMajor(i));
            canvas.Line(batch, new Vector3(-half, Lift, at), new Vector3(half, Lift, at), layout.IsMajor(i));
        }

        GridCanvas.Axis(batch, new Vector3(-half, Lift, 0f), new Vector3(half, Lift, 0f), ReferenceGrid.AxisXColor);
        GridCanvas.Axis(batch, new Vector3(0f, Lift, -half), new Vector3(0f, Lift, half), ReferenceGrid.AxisZColor);
        GridCanvas.Axis(batch, new Vector3(0f, Lift, 0f), new Vector3(0f, layout.Height, 0f), ReferenceGrid.AxisYColor);
    }

    private static void DrawNumbers(GridCanvas canvas, in GridView view, in Layout layout)
    {
        for (var i = layout.First; i <= layout.Last; i++)
        {
            if (i == 0 || !layout.IsMajor(i)) continue;

            var at = i * layout.Minor;
            var text = GridSteps.Format(at, layout.Major);

            canvas.WorldLabel(view, text, new Vector3(at, 0f, 0f), ReferenceGrid.AxisXColor);
            canvas.WorldLabel(view, text, new Vector3(0f, 0f, at), ReferenceGrid.AxisZColor);

            if (at > 0f && at <= layout.Height) canvas.WorldLabel(view, text, new Vector3(0f, at, 0f), ReferenceGrid.AxisYColor);
        }

        canvas.WorldLabel(view, "0", Vector3.Zero, Color.White);

        // The letters stand halfway to the first number on the positive side of each axis. The far
        // ends of the axes are usually outside the window; the origin, when it shows, never is
        var letter = layout.Major * 0.5f;

        canvas.WorldLabel(view, "X", new Vector3(letter, 0f, 0f), ReferenceGrid.AxisXColor);
        canvas.WorldLabel(view, "Y", new Vector3(0f, letter, 0f), ReferenceGrid.AxisYColor);
        canvas.WorldLabel(view, "Z", new Vector3(0f, 0f, letter), ReferenceGrid.AxisZColor);
    }

    /// <summary>Which lines a frame draws: the square around the origin, cut into steps.</summary>
    private readonly struct Layout
    {
        private readonly int _divisions;

        internal Layout(float half, float major, int divisions)
        {
            Half = half;
            Major = major;
            _divisions = divisions;
            Minor = major / divisions;

            (First, Last) = GridSteps.Range(-half, half, Minor);
        }

        /// <summary>Half the side of the square.</summary>
        internal float Half { get; }

        /// <summary>How high the Y axis stands.</summary>
        internal float Height => Half * 0.5f;

        internal float Major { get; }

        internal float Minor { get; }

        internal int First { get; }

        internal int Last { get; }

        internal bool IsMajor(int index) => index % _divisions == 0;
    }
}
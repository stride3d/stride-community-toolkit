using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using System.Globalization;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// A coordinate readout that follows the mouse: a small ring on the chart plane under the cursor, drawn
/// every frame with a glow, and a label with the chart-space coordinates next to it, hidden while the
/// cursor is off the chart. Owned by the chart once <see cref="ChartCursorOptions.Visible"/> is first
/// turned on, and pumped by <see cref="Chart.Update(CameraComponent)"/>.
/// </summary>
/// <remarks>
/// The mouse ray is intersected with the chart's plane by hand rather than through <see cref="Plane"/>,
/// whose point-and-normal constructor stores the wrong sign for a plane that does not pass through the
/// origin. It works with the orthographic 2D camera and a free 3D camera alike, and respects the chart
/// root's position, rotation and scale.
/// </remarks>
internal sealed class ChartCursor : IDisposable
{
    private const float RingWidth = 2f;

    private readonly Chart _chart;
    private readonly ChartText _label;
    private bool _isDisposed;

    /// <summary>The point under the mouse in chart units, or <see langword="null"/> while the cursor is off the chart.</summary>
    internal Vector3? Position { get; private set; }

    internal ChartCursor(IGame game, Chart chart)
    {
        _chart = chart;
        var o = chart.Options;

        // The readout draws text even when tick labels are off
        ChartText.EnsureRenderer(game, o.Labels.Mode);

        // Up and to the right of the ring, clear of the mouse pointer
        _label = new ChartText(o.Labels, "Cursor readout");
        _label.Set(string.Empty, Anchor.BottomLeft, new Vector2(10f, -10f));
        _label.Visible = false;
        chart.Root.AddChild(_label.Entity);
    }

    /// <summary>
    /// Moves the readout to the point of the chart plane under <paramref name="screenPosition"/>, or hides it
    /// when that point is outside the chart's ranges or behind the camera.
    /// </summary>
    /// <param name="camera">The camera the mouse position is relative to.</param>
    /// <param name="screenPosition">The mouse position in normalised screen coordinates - <c>Input.MousePosition</c> as it comes.</param>
    /// <param name="view">This frame's view of the chart.</param>
    internal void Update(CameraComponent camera, Vector2 screenPosition, in ChartView view)
    {
        var world = view.World;
        var normal = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitZ, world));

        var ray = camera.ScreenToWorldRay(screenPosition);
        var facing = Vector3.Dot(normal, ray.Direction);

        // Parallel to the plane, or the plane is behind the camera
        if (MathUtil.IsZero(facing))
        {
            Hide();
            return;
        }

        var distance = Vector3.Dot(normal, world.TranslationVector - ray.Position) / facing;

        if (distance < 0f)
        {
            Hide();
            return;
        }

        var hit = ray.Position + ray.Direction * distance;

        Matrix.Invert(ref world, out var toChart);
        var local = Vector3.TransformCoordinate(hit, toChart);

        var o = _chart.Options;

        if (local.X < o.Range.XMin || local.X > o.Range.XMax || local.Y < o.Range.YMin || local.Y > o.Range.YMax)
        {
            Hide();
            return;
        }

        Position = local;
        _label.Position = new Vector3(local.X, local.Y, 3f * Chart.LayerStep);
        _label.Text = $"x = {local.X.ToString(o.Cursor.Format, CultureInfo.InvariantCulture)}  y = {local.Y.ToString(o.Cursor.Format, CultureInfo.InvariantCulture)}";
        _label.Visible = true;
    }

    /// <summary>Submits the ring for this frame, in the label colour, with the halo the options ask for.</summary>
    internal void Draw(ShapeBatch batch, in ChartView view)
    {
        if (Position is not { } local)
            return;

        var o = _chart.Options;

        // The batch may be the caller's, so its state is put back the way it was found
        var stroke = batch.StrokeWidth;
        var glowWidth = batch.Glow.Width;
        var glowColor = batch.Glow.Color;

        batch.StrokeWidth = RingWidth;
        batch.Glow.Set(o.Cursor.Glow);
        batch.DrawPixelRing(view.ToWorld(new Vector3(local.X, local.Y, 3f * Chart.LayerStep)), o.Cursor.Radius, o.Labels.Color);

        batch.StrokeWidth = stroke;
        batch.Glow.Set(glowWidth, glowColor);
    }

    /// <summary>
    /// Hides the marker and the label until the next <see cref="Update"/> that lands on the chart.
    /// </summary>
    internal void Hide()
    {
        Position = null;
        _label.Visible = false;
    }

    /// <summary>Removes the label from the chart. Called by <see cref="Chart.Dispose"/>; safe to call twice.</summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _label.Dispose();
    }
}
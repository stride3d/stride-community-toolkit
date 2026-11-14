using Stride.Core.Mathematics;
using Stride.Engine.Processors;
using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// Keeps a chart's ranges in step with what an orthographic camera sees, turning it into a Desmos-style
/// infinite chart: pan and the axes follow, zoom out and the grid still covers the whole screen with a
/// coarser tick step, zoom in and it refines. Owned by the chart while
/// <see cref="ChartRangeOptions.FollowCamera"/> is on and pumped by <see cref="Chart.Update(CameraComponent)"/>.
/// </summary>
/// <remarks>
/// The follower only reads the camera - position and orthographic size - and never writes to it, so it
/// works with <c>Basic2DCameraController</c>, any other controller, or a camera animated by hand. On each
/// meaningful view change it picks a 1-2-5 tick step for the zoom level (<see cref="ChartFraming.NiceTickStep"/>)
/// and writes the new range into the chart's options, which the chart's own range diff then applies.
/// Changes smaller than half a percent of the view are ignored, so an idle camera costs nothing.
/// </remarks>
/// <param name="game">The game whose window gives the view its aspect ratio.</param>
/// <param name="chart">The chart whose range follows the camera.</param>
internal sealed class ChartViewFollower(IGame game, Chart chart)
{
    private ViewRect? _lastRange;
    private float _lastPixelHeight;

    private readonly record struct ViewRect(float Left, float Right, float Bottom, float Top);

    /// <summary>
    /// Writes the camera's visible rectangle into the chart's range if the view has meaningfully changed.
    /// Does nothing for a perspective camera - the view-driven chart is a 2D, orthographic idea.
    /// </summary>
    /// <param name="camera">The camera whose view the chart should cover.</param>
    internal void Update(CameraComponent camera)
    {
        if (camera.Projection != CameraProjectionMode.Orthographic)
            return;

        var backBuffer = game.GraphicsDevice.Presenter.BackBuffer;
        var aspect = (float)backBuffer.Width / backBuffer.Height;
        var pixelHeight = (float)backBuffer.Height;

        // The camera's position in chart space; the chart root may be moved
        var world = chart.Root.Transform.WorldMatrix;
        Matrix.Invert(ref world, out var toChart);
        var centre = Vector3.TransformCoordinate(camera.Entity.Transform.WorldMatrix.TranslationVector, toChart);

        var height = camera.OrthographicSize;
        var width = height * aspect;

        var range = new ViewRect(
            centre.X - width * 0.5f,
            centre.X + width * 0.5f,
            centre.Y - height * 0.5f,
            centre.Y + height * 0.5f);

        // Rebuilding the scaffolding is not free; ignore camera jitter below half a percent of the view
        if (_lastRange is { } last && pixelHeight == _lastPixelHeight)
        {
            var threshold = height * 0.005f;

            if (MathF.Abs(range.Left - last.Left) < threshold && MathF.Abs(range.Right - last.Right) < threshold
                && MathF.Abs(range.Bottom - last.Bottom) < threshold && MathF.Abs(range.Top - last.Top) < threshold)
            {
                return;
            }
        }

        _lastRange = range;
        _lastPixelHeight = pixelHeight;

        // One square grid step for both axes, refined or coarsened with the zoom; minors split a step of
        // 2 into quarters and steps of 1 and 5 into fifths, so minor lines land on readable values
        var step = ChartFraming.NiceTickStep(height);
        var magnitude = MathF.Pow(10f, MathF.Floor(MathF.Log10(step)));
        var mantissa = step / magnitude;

        var r = chart.Options.Range;
        r.TickStep = step;
        r.MinorDivisions = mantissa is > 1.5f and < 3f ? 4 : 5;
        r.XMin = range.Left;
        r.XMax = range.Right;
        r.YMin = range.Bottom;
        r.YMax = range.Top;
    }
}
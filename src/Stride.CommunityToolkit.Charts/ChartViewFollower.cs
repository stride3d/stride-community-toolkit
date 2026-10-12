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
internal sealed class ChartViewFollower
{
    private readonly IGame _game;
    private readonly Chart _chart;
    private Vector4? _lastRange;
    private float _lastPixelHeight;

    internal ChartViewFollower(IGame game, Chart chart)
    {
        _game = game;
        _chart = chart;
    }

    /// <summary>
    /// Writes the camera's visible rectangle into the chart's range if the view has meaningfully changed.
    /// Does nothing for a perspective camera - the view-driven chart is a 2D, orthographic idea.
    /// </summary>
    /// <param name="camera">The camera whose view the chart should cover.</param>
    internal void Update(CameraComponent camera)
    {
        if (camera.Projection != CameraProjectionMode.Orthographic)
            return;

        var backBuffer = _game.GraphicsDevice.Presenter.BackBuffer;
        var aspect = (float)backBuffer.Width / backBuffer.Height;
        var pixelHeight = (float)backBuffer.Height;

        // The camera's position in chart space; the chart root may be moved
        var world = _chart.Root.Transform.WorldMatrix;
        Matrix.Invert(ref world, out var toChart);
        var centre = Vector3.TransformCoordinate(camera.Entity.Transform.WorldMatrix.TranslationVector, toChart);

        var height = camera.OrthographicSize;
        var width = height * aspect;

        var range = new Vector4(
            centre.X - width * 0.5f,
            centre.X + width * 0.5f,
            centre.Y - height * 0.5f,
            centre.Y + height * 0.5f);

        // Rebuilding the scaffolding is not free; ignore camera jitter below half a percent of the view
        if (_lastRange is { } last && pixelHeight == _lastPixelHeight)
        {
            var threshold = height * 0.005f;

            if (MathF.Abs(range.X - last.X) < threshold && MathF.Abs(range.Y - last.Y) < threshold
                && MathF.Abs(range.Z - last.Z) < threshold && MathF.Abs(range.W - last.W) < threshold)
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

        var r = _chart.Options.Range;
        r.TickStep = step;
        r.MinorDivisions = mantissa is > 1.5f and < 3f ? 4 : 5;
        r.XMin = range.X;
        r.XMax = range.Y;
        r.YMin = range.Z;
        r.YMax = range.W;
    }
}
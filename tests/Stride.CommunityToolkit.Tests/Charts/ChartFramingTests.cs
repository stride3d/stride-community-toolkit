using Stride.CommunityToolkit.Charts;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Charts;

/// <summary>
/// Pins <see cref="ChartFraming"/>: the fit-to-window mathematics behind <c>Chart.FrameCamera</c>.
/// </summary>
public class ChartFramingTests
{
    private const float RightAngleFov = MathF.PI / 2f;

    [Fact]
    public void OrthographicSize_IsTheHeight_WhenHeightIsTheLimit()
    {
        // A 10 x 8 rectangle in a 2:1 window: 10 wide fits in 16, so the height of 8 decides
        Assert.Equal(8f, ChartFraming.OrthographicSize(10f, 8f, aspectRatio: 2f, padding: 0f), Tolerances.Geometry);
    }

    [Fact]
    public void OrthographicSize_GrowsToFitTheWidth_WhenWidthIsTheLimit()
    {
        // 30 wide in a 2:1 window needs a visible height of 15
        Assert.Equal(15f, ChartFraming.OrthographicSize(30f, 8f, aspectRatio: 2f, padding: 0f), Tolerances.Geometry);
    }

    [Fact]
    public void OrthographicSize_AddsPaddingOnBothSides()
    {
        // 10 % padding per side scales the deciding extent by 1.2
        Assert.Equal(9.6f, ChartFraming.OrthographicSize(10f, 8f, aspectRatio: 2f, padding: 0.1f), Tolerances.Geometry);
    }

    [Fact]
    public void PerspectiveDistance_MatchesTheTangent_ForAHeadOnSquare()
    {
        // At a 90° vertical FOV the tangent of the half angle is 1: an 8-high flat box needs distance 4
        var distance = ChartFraming.PerspectiveDistance(Box(8f, 8f, 0f), Vector3.UnitX, Vector3.UnitY, -Vector3.UnitZ, aspectRatio: 1f, RightAngleFov, padding: 0f);

        Assert.Equal(4f, distance, Tolerances.Geometry);
    }

    [Fact]
    public void PerspectiveDistance_StepsBackPastTheNearFace()
    {
        // The corner on the near face at z = +3 demands its lateral fit plus its depth towards the camera
        var distance = ChartFraming.PerspectiveDistance(Box(8f, 8f, 6f), Vector3.UnitX, Vector3.UnitY, -Vector3.UnitZ, aspectRatio: 1f, RightAngleFov, padding: 0f);

        Assert.Equal(7f, distance, Tolerances.Geometry);
    }

    [Fact]
    public void PerspectiveDistance_UsesTheWidth_WhenTheWindowIsNarrow()
    {
        // A 1:2 (portrait) window halves the horizontal tangent: 8 wide needs distance 8, not 4
        var distance = ChartFraming.PerspectiveDistance(Box(8f, 8f, 0f), Vector3.UnitX, Vector3.UnitY, -Vector3.UnitZ, aspectRatio: 0.5f, RightAngleFov, padding: 0f);

        Assert.Equal(8f, distance, Tolerances.Geometry);
    }

    [Fact]
    public void PerspectiveDistance_FitsTheProjectedCorners_ForAnObliqueView()
    {
        // Looking at the flat 8 x 8 box from 45°: the worst corner sits 2√2 towards the camera with a
        // lateral offset of 2√2, and its vertical offset of 4 decides - distance 4 + 2√2, not the
        // head-on 4 and not a loose overshoot
        var forward = Vector3.Normalize(new Vector3(-1f, 0f, -1f));
        var right = Vector3.Normalize(new Vector3(1f, 0f, -1f));

        var distance = ChartFraming.PerspectiveDistance(Box(8f, 8f, 0f), right, Vector3.UnitY, forward, aspectRatio: 1f, RightAngleFov, padding: 0f);

        Assert.Equal(4f + 2f * MathF.Sqrt(2f), distance, Tolerances.Geometry);
    }

    [Fact]
    public void RejectsOutOfRangeInputs()
        => Assert.Multiple(
            () => Assert.Throws<ArgumentOutOfRangeException>(() => ChartFraming.OrthographicSize(0f, 8f, 2f)),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => ChartFraming.OrthographicSize(10f, 8f, 0f)),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => ChartFraming.PerspectiveDistance(Box(8f, 8f, 0f), Vector3.UnitX, Vector3.UnitY, -Vector3.UnitZ, 1f, 0f)),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => ChartFraming.PerspectiveDistance(Box(8f, 8f, 0f), Vector3.UnitX, Vector3.UnitY, -Vector3.UnitZ, 1f, MathF.PI)));

    /// <summary>A box of the given extents centred on the origin.</summary>
    private static BoundingBox Box(float width, float height, float depth)
        => new(new Vector3(-width * 0.5f, -height * 0.5f, -depth * 0.5f), new Vector3(width * 0.5f, height * 0.5f, depth * 0.5f));
}

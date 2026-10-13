using Stride.CommunityToolkit.Charts;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// Pins what <see cref="PolylineClipping"/> does to the awkward curves a plotter meets: samples outside the
/// visible range, <c>NaN</c> from a function outside its domain, and the jump across an asymptote. All of it
/// is point arithmetic, so no graphics device is involved.
/// </summary>
public class PolylineClippingTests
{
    private const float Tolerance = 1e-5f;

    [Fact]
    public void Clip_KeepsALineThatIsEntirelyInside_AsOneUnchangedRun()
    {
        Vector3[] points = [new(-1, -1, 0), new(0, 0.5f, 0), new(1, 1, 0)];

        var runs = PolylineClipping.Clip(points, -5, 5, -5, 5);

        var run = Assert.Single(runs);
        Assert.Equal(points, run);
    }

    [Fact]
    public void Clip_EndsARunOnTheEdge_WhenTheLineLeaves()
    {
        // Rises out of the top of a [-1, 1] window between the second and third point
        Vector3[] points = [new(0, 0, 0), new(1, 0.5f, 0), new(2, 1.5f, 0)];

        var runs = PolylineClipping.Clip(points, -5, 5, -1, 1);

        var run = Assert.Single(runs);
        Assert.Equal(3, run.Length);
        Assert.Equal(1f, run[^1].Y, Tolerance);
        Assert.Equal(1.5f, run[^1].X, Tolerance); // halfway along the segment, where y crosses 1
    }

    [Fact]
    public void Clip_SplitsIntoTwoRuns_WhenTheLineLeavesAndComesBack()
    {
        // A "V" that dips below the window: out through the bottom and back in
        Vector3[] points = [new(-2, 0, 0), new(0, -2, 0), new(2, 0, 0)];

        var runs = PolylineClipping.Clip(points, -5, 5, -1, 1);

        Assert.Equal(2, runs.Count);
        Assert.Equal(-1f, runs[0][^1].Y, Tolerance);
        Assert.Equal(-1f, runs[1][0].Y, Tolerance);
        Assert.Equal(-1f, runs[0][^1].X, Tolerance);
        Assert.Equal(1f, runs[1][0].X, Tolerance);
    }

    [Fact]
    public void Clip_KeepsTheCrossingPiece_WhenBothEndsAreOutside()
    {
        Vector3[] points = [new(-10, 0, 0), new(10, 0, 0)];

        var runs = PolylineClipping.Clip(points, -5, 5, -5, 5);

        var run = Assert.Single(runs);
        Assert.Equal(new Vector3(-5, 0, 0), run[0]);
        Assert.Equal(new Vector3(5, 0, 0), run[1]);
    }

    [Fact]
    public void Clip_DropsALineThatMissesTheRectangle()
    {
        Vector3[] points = [new(-10, 10, 0), new(10, 10, 0)];

        var runs = PolylineClipping.Clip(points, -5, 5, -5, 5);

        Assert.Empty(runs);
    }

    [Fact]
    public void Clip_InterpolatesZ_AlongWithXAndY()
    {
        Vector3[] points = [new(-10, 0, 0), new(10, 0, 4)];

        var runs = PolylineClipping.Clip(points, -5, 5, -5, 5);

        var run = Assert.Single(runs);
        Assert.Equal(1f, run[0].Z, Tolerance);
        Assert.Equal(3f, run[1].Z, Tolerance);
    }

    [Fact]
    public void Clip_BreaksTheLine_AtANonFinitePoint()
    {
        Vector3[] points = [new(-1, 0, 0), new(0, float.NaN, 0), new(1, 0, 0), new(2, 0, 0)];

        var runs = PolylineClipping.Clip(points, -5, 5, -5, 5);

        var run = Assert.Single(runs);
        Assert.Equal(new Vector3(1, 0, 0), run[0]);
        Assert.Equal(new Vector3(2, 0, 0), run[1]);
    }

    [Fact]
    public void Clip_ReturnsNothing_ForFewerThanTwoPoints()
    {
        Assert.Empty(PolylineClipping.Clip([], -5, 5, -5, 5));
        Assert.Empty(PolylineClipping.Clip([new Vector3(0, 0, 0)], -5, 5, -5, 5));
    }

    [Fact]
    public void Clip_Throws_WhenTheRectangleIsInsideOut()
    {
        Assert.Throws<ArgumentException>(() => PolylineClipping.Clip([new Vector3(0, 0, 0)], 5, -5, -5, 5));
    }

    [Fact]
    public void SplitAtNonFinite_SeparatesTheDomainOfLog_FromTheNaNsBeforeIt()
    {
        var points = PolylineSampling.Function(x => MathF.Log(x), -2, 2, 9);

        var runs = PolylineClipping.SplitAtNonFinite(points);

        var run = Assert.Single(runs);
        Assert.All(run, p => Assert.True(p.X > 0f));
        Assert.All(run, p => Assert.True(float.IsFinite(p.Y)));
    }

    [Fact]
    public void SplitAtJumps_CutsTanAtEveryAsymptote()
    {
        // 4 asymptotes in [-5, 5] (at ±π/2 and ±3π/2) give 5 continuous branches
        var points = PolylineSampling.Function(MathF.Tan, -5, 5, 400);

        var runs = PolylineClipping.SplitAtJumps(points, maxJump: 8f);

        Assert.Equal(5, runs.Count);

        // Each break is a genuine pole crossing: the branch ends far on one side of zero and the next
        // begins far on the other. Steep same-sign segments inside a branch stay connected.
        for (var i = 0; i < runs.Count - 1; i++)
        {
            Assert.True(MathF.Sign(runs[i][^1].Y) != MathF.Sign(runs[i + 1][0].Y));
        }
    }

    [Fact]
    public void SplitAtJumps_LeavesASmoothCurveAlone()
    {
        var points = PolylineSampling.Function(MathF.Sin, -5, 5, 100);

        var runs = PolylineClipping.SplitAtJumps(points, maxJump: 8f);

        var run = Assert.Single(runs);
        Assert.Equal(points.Length, run.Length);
    }

    [Fact]
    public void Append_DropsAPointThatRepeatsThePreviousOne()
    {
        Vector3[] points = [new(0, 0, 0), new(0, 0, 0), new(1, 1, 0)];

        var runs = PolylineClipping.SplitAtNonFinite(points);

        var run = Assert.Single(runs);
        Assert.Equal(2, run.Length);
    }

    [Fact]
    public void ClipSegment_ReturnsTheInteriorParameterRange()
    {
        var hit = PolylineClipping.ClipSegment(new Vector3(-10, 0, 0), new Vector3(10, 0, 0), Rect(-5, 5, -5, 5), out var t0, out var t1);

        Assert.True(hit);
        Assert.Equal(0.25f, t0, Tolerance);
        Assert.Equal(0.75f, t1, Tolerance);
    }

    [Fact]
    public void ClipSegment_KeepsTheFullRange_WhenEntirelyInside()
    {
        var hit = PolylineClipping.ClipSegment(new Vector3(-1, 0, 0), new Vector3(1, 0, 0), Rect(-5, 5, -5, 5), out var t0, out var t1);

        Assert.True(hit);
        Assert.Equal(0f, t0, Tolerance);
        Assert.Equal(1f, t1, Tolerance);
    }

    [Fact]
    public void ClipSegment_ReturnsFalse_WhenTheSegmentMisses()
    {
        var hit = PolylineClipping.ClipSegment(new Vector3(-10, 6, 0), new Vector3(10, 6, 0), Rect(-5, 5, -5, 5), out _, out _);

        Assert.False(hit);
    }

    [Fact]
    public void SplitAtJumps_WithExtendEnds_MakesBranchesReachPastTheJump()
    {
        var points = PolylineSampling.Function(MathF.Tan, -5, 5, 400);

        var runs = PolylineClipping.SplitAtJumps(points, maxJump: 8f, extendEnds: true);

        Assert.Equal(5, runs.Count);

        // Every branch cut by an asymptote is extended well past the jump threshold, so clipping to a
        // chart cuts it at the edge instead of wherever the sampling gave up
        for (var i = 0; i < runs.Count - 1; i++)
        {
            Assert.True(MathF.Abs(runs[i][^1].Y) > 8f, $"run {i} ends at {runs[i][^1].Y}");
            Assert.True(MathF.Abs(runs[i + 1][0].Y) > 8f, $"run {i + 1} starts at {runs[i + 1][0].Y}");
        }
    }

    [Fact]
    public void Clip3D_KeepsALineInsideTheBox_Unchanged()
    {
        Vector3[] points = [new(-1, -1, -1), new(1, 1, 1)];

        var runs = PolylineClipping.Clip(points, -5, 5, -5, 5, -5, 5);

        var run = Assert.Single(runs);
        Assert.Equal(points, run);
    }

    [Fact]
    public void Clip3D_EndsARunOnTheZFace_WhenTheLineLeaves()
    {
        // Climbs out of the far face z = 1 halfway along
        Vector3[] points = [new(0, 0, 0), new(2, 0, 2)];

        var runs = PolylineClipping.Clip(points, -5, 5, -5, 5, -1, 1);

        var run = Assert.Single(runs);
        Assert.Equal(1f, run[^1].Z, Tolerance);
        Assert.Equal(1f, run[^1].X, Tolerance);
    }

    [Fact]
    public void Clip3D_DropsALineEntirelyOutsideTheZRange()
    {
        Vector3[] points = [new(-1, 0, 5), new(1, 0, 5)];

        var runs = PolylineClipping.Clip(points, -5, 5, -5, 5, -1, 1);

        Assert.Empty(runs);
    }

    [Fact]
    public void ClipSegment_IgnoresZ_WhenTheBoxIsUnboundedInZ()
    {
        var a = new Vector3(-10, 0, 42);
        var b = new Vector3(10, 0, -17);

        var hit = PolylineClipping.ClipSegment(a, b, Rect(-5, 5, -5, 5), out var t0, out var t1);

        Assert.True(hit);
        Assert.Equal(0.25f, t0, Tolerance);
        Assert.Equal(0.75f, t1, Tolerance);
    }

    /// <summary>A clip box bounded in X and Y and unbounded in Z - the 2D chart case.</summary>
    private static BoundingBox Rect(float xMin, float xMax, float yMin, float yMax)
        => new(new Vector3(xMin, yMin, -PolylineClipping.UnboundedZ), new Vector3(xMax, yMax, PolylineClipping.UnboundedZ));
}
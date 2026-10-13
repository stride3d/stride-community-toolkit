using Stride.CommunityToolkit.Charts;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// Pins <see cref="AreaColumns.Columns"/>: how a filled band is clamped to the visible range and broken
/// where it cannot be drawn. No graphics device is involved.
/// </summary>
public class AreaColumnsTests
{
    private const float Tolerance = 1e-5f;

    [Fact]
    public void Columns_KeepsABandInsideTheRange_Unchanged()
    {
        Vector3[] upper = [new(0, 1, 0), new(1, 2, 0), new(2, 1, 0)];
        Vector3[] lower = [new(0, 0, 0), new(1, 0, 0), new(2, 0, 0)];

        var runs = AreaColumns.Columns(upper, lower, -5, 5);

        var run = Assert.Single(runs);
        Assert.Equal(3, run.Count);
        Assert.Equal(2f, run[1].Upper.Y, Tolerance);
    }

    [Fact]
    public void Columns_ClampsToTheTop_WhenTheBandOverflows()
    {
        Vector3[] upper = [new(0, 1, 0), new(1, 9, 0)];
        Vector3[] lower = [new(0, 0, 0), new(1, 0, 0)];

        var runs = AreaColumns.Columns(upper, lower, -5, 5);

        var run = Assert.Single(runs);
        Assert.Equal(5f, run[1].Upper.Y, Tolerance);
        Assert.Equal(0f, run[1].Lower.Y, Tolerance);
    }

    [Fact]
    public void Columns_BreaksTheBand_WhereAColumnIsEntirelyOutside()
    {
        // The middle column sits far above the range: the fill must not slide along the top edge
        Vector3[] upper = [new(0, 1, 0), new(1, 20, 0), new(2, 1, 0), new(3, 1, 0)];
        Vector3[] lower = [new(0, 0, 0), new(1, 19, 0), new(2, 0, 0), new(3, 0, 0)];

        var runs = AreaColumns.Columns(upper, lower, -5, 5);

        var run = Assert.Single(runs);
        Assert.Equal(2, run.Count);
        Assert.Equal(2f, run[0].Upper.X, Tolerance);
    }

    [Fact]
    public void Columns_BreaksTheBand_AtANonFiniteEdge()
    {
        Vector3[] upper = [new(0, 1, 0), new(1, float.NaN, 0), new(2, 1, 0), new(3, 1, 0)];
        Vector3[] lower = [new(0, 0, 0), new(1, 0, 0), new(2, 0, 0), new(3, 0, 0)];

        var runs = AreaColumns.Columns(upper, lower, -5, 5);

        var run = Assert.Single(runs);
        Assert.Equal(2, run.Count);
    }

    [Fact]
    public void Columns_ReturnsNothing_ForABandOutsideTheRange()
    {
        Vector3[] upper = [new(0, 9, 0), new(1, 9, 0)];
        Vector3[] lower = [new(0, 8, 0), new(1, 8, 0)];

        Assert.Empty(AreaColumns.Columns(upper, lower, -5, 5));
    }

    [Fact]
    public void Columns_RejectsMismatchedEdges()
    {
        Vector3[] upper = [new(0, 1, 0), new(1, 1, 0)];
        Vector3[] lower = [new(0, 0, 0)];

        Assert.Throws<ArgumentException>(() => AreaColumns.Columns(upper, lower, -5, 5));
    }
}

using Stride.CommunityToolkit.Charts;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Charts;

/// <summary>
/// Pins <see cref="ChartFraming.TickValues"/>: where the ticks, labels and grid lines of an axis land, and
/// that float error at the edges of a range never drops the last one.
/// </summary>
public class TickValuesTests
{
    private const float Tolerance = 1e-5f;

    [Fact]
    public void ProducesEveryMultipleInsideTheRangeInclusive()
    {
        var ticks = ChartFraming.TickValues(-2f, 2f, 1f).ToArray();

        Assert.Equal([-2f, -1f, 0f, 1f, 2f], ticks);
    }

    [Fact]
    public void StartsAtTheFirstMultipleAboveTheMinimum()
    {
        var ticks = ChartFraming.TickValues(-1.5f, 1.5f, 1f).ToArray();

        Assert.Equal([-1f, 0f, 1f], ticks);
    }

    [Fact]
    public void KeepsAnEdgeThatFloatErrorPutsJustOutside()
    {
        // 0.1 * 30 is not exactly 3 in float; the last tick must still be there
        var ticks = ChartFraming.TickValues(0f, 3f, 0.1f).ToArray();

        Assert.Equal(31, ticks.Length);
        Assert.Equal(3f, ticks[^1], Tolerance);
    }

    [Fact]
    public void ProducesNothingForAStepThatIsNotPositive()
    {
        Assert.Empty(ChartFraming.TickValues(-5f, 5f, 0f));
        Assert.Empty(ChartFraming.TickValues(-5f, 5f, -1f));
    }

    [Fact]
    public void ProducesNothingWhenNoMultipleFits()
    {
        Assert.Empty(ChartFraming.TickValues(0.2f, 0.8f, 1f));
    }

    [Fact]
    public void IsAscending()
    {
        var ticks = ChartFraming.TickValues(-7.3f, 12.9f, 2.5f).ToArray();

        for (var i = 1; i < ticks.Length; i++)
        {
            Assert.True(ticks[i] > ticks[i - 1]);
        }
    }
}
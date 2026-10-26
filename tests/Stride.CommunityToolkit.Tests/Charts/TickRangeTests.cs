using Stride.CommunityToolkit.Charts;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Charts;

/// <summary>
/// Pins <see cref="TickRange"/>, the allocation-free walk behind <see cref="ChartFraming.TickValues"/> and
/// the grid: the same multiples, and the minor grid's skipping of the lines the major one draws.
/// </summary>
public class TickRangeTests
{
    private const float Tolerance = 1e-5f;

    [Fact]
    public void WalksTheSameMultiplesAsTickValues()
    {
        var walked = new List<float>();

        foreach (var value in TickRange.Of(-1.5f, 3f, 0.5f)) walked.Add(value);

        Assert.Equal(ChartFraming.TickValues(-1.5f, 3f, 0.5f), walked);
    }

    [Fact]
    public void LeavesOutTheMultiplesOfTheSkipSpacing()
    {
        var walked = new List<float>();

        // A minor grid of 0.5 under a major grid of 1: the whole numbers are the major lines
        foreach (var value in TickRange.Of(0f, 3f, 0.5f, skip: 1f)) walked.Add(value);

        Assert.Equal([0.5f, 1.5f, 2.5f], walked.Select(v => MathF.Round(v, 5)));
    }

    [Fact]
    public void WalksNothingForAStepThatIsNotPositive()
    {
        var count = 0;

        foreach (var _ in TickRange.Of(0f, 10f, 0f)) count++;

        Assert.Equal(0, count);
    }

    [Fact]
    public void KeepsAnEdgeThatFloatErrorPutsJustOutside()
    {
        var last = float.NaN;
        var count = 0;

        foreach (var value in TickRange.Of(0f, 3f, 0.1f))
        {
            last = value;
            count++;
        }

        Assert.Equal(31, count);
        Assert.Equal(3f, last, Tolerance);
    }
}
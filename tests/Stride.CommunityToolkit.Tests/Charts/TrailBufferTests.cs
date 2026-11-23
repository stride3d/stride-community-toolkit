using Stride.CommunityToolkit.Charts;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Charts;

/// <summary>
/// The buffer a chart trajectory records its points in, for the flat strokes and the 3D ribbon alike: breaks
/// stay between runs, the oldest point goes once the buffer is full, and a repeated point is not a new one.
/// </summary>
public class TrailBufferTests
{
    [Fact]
    public void ATrail_KeepsItsBreaks_AndDropsTheOldestPointOnceFull()
    {
        var trail = new TrailBuffer(capacity: 3) { RollOver = true };

        trail.Add(new Vector3(0, 0, 0));
        trail.Add(new Vector3(1, 0, 0));
        trail.Break();
        trail.Add(new Vector3(2, 0, 0));

        Assert.Equal(3, trail.Count);
        Assert.Equal(4, trail.Items.Length);
        Assert.True(TrailBuffer.IsBreak(trail.Items[2]));

        // Full: the oldest point goes, the break stays between the two runs
        trail.Add(new Vector3(3, 0, 0));

        Assert.Equal(3, trail.Count);
        Assert.Equal(new Vector3(1, 0, 0), trail.Items[0]);
        Assert.True(TrailBuffer.IsBreak(trail.Items[1]));
        Assert.Equal(new Vector3(3, 0, 0), trail.Items[^1]);

        // A point on top of the last one is not a new point
        trail.Add(new Vector3(3, 0, 0));

        Assert.Equal(3, trail.Count);
    }

    [Fact]
    public void AFullTrail_ThatDoesNotRollOver_TakesNoMorePoints()
    {
        var trail = new TrailBuffer(capacity: 2);

        trail.Add(new Vector3(0, 0, 0));
        trail.Add(new Vector3(1, 0, 0));
        trail.Add(new Vector3(2, 0, 0));

        Assert.Equal(2, trail.Count);
        Assert.Equal(new Vector3(1, 0, 0), trail.Items[^1]);
    }
}
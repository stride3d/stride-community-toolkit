using Stride.CommunityToolkit.Collections;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Collections;

/// <summary>
/// The list resize and the dictionary counter: a list grows with defaults, a list grown after a shrink does not
/// show its old items again, and a counter starts at one.
/// </summary>
public class CollectionExtensionsTests
{
    [Fact]
    public void EnsureSize_GrowsWithDefaults_AndLeavesALongerListAlone()
    {
        var list = new List<int> { 1, 2 };

        list.EnsureSize(5);

        Assert.Equal([1, 2, 0, 0, 0], list);

        list.EnsureSize(3);

        Assert.Equal(5, list.Count);
    }

    [Fact]
    public void SetCount_GrownAfterAShrink_DoesNotBringTheOldItemsBack()
    {
        var list = new List<string?> { "a", "b", "c", "d" };

        list.SetCount(1);

        Assert.Equal(["a"], list);

        // The backing array still held b, c and d; growing must show defaults, not them
        list.SetCount(4);

        Assert.Equal(["a", null, null, null], list);
    }

    [Fact]
    public void SetCount_OfValueTypes_GrownAfterAShrink_IsZeroed()
    {
        var list = new List<int> { 7, 8, 9 };

        list.SetCount(1);
        list.SetCount(3);

        Assert.Equal([7, 0, 0], list);
    }

    [Fact]
    public void Increment_StartsAtOne_AndCountsUp()
    {
        var counts = new Dictionary<string, int>();

        Assert.Equal(1, counts.Increment("a"));
        Assert.Equal(2, counts.Increment("a"));
        Assert.Equal(1, counts.Increment("b"));
        Assert.Equal(2, counts["a"]);
    }
}
using CubeCollapse.Components;
using CubeCollapse.Gameplay;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;

namespace Stride.CommunityToolkit.Tests.CubeCollapse;

/// <summary>
/// Covers the logical grid and the column collapse that follows a clear.
/// </summary>
/// <remarks>
/// These run with bare entities and no game, which is the point of holding the layout apart from the
/// physics: the collapse rule can be checked exactly, rather than by watching cubes fall.
/// </remarks>
public class CubeGridTests
{
    private static Entity MakeCube(Color colour) => new("Cube") { new CubeComponent(colour) };

    private static CubeGrid BuildColumn(params Color[] coloursFromBottom)
    {
        var grid = new CubeGrid();

        for (var layer = 0; layer < coloursFromBottom.Length; layer++)
        {
            grid.Add(new Int3(0, layer, 0), MakeCube(coloursFromBottom[layer]));
        }

        return grid;
    }

    [Fact]
    public void AddRecordsTheCoordinateOnTheCube()
    {
        var grid = new CubeGrid();
        var cube = MakeCube(Color.Red);

        grid.Add(new Int3(2, 3, 4), cube);

        Assert.Equal(new Int3(2, 3, 4), cube.Get<CubeComponent>()!.GridPosition);
        Assert.Same(cube, grid.Get(new Int3(2, 3, 4)));
    }

    [Fact]
    public void EmptySpaceReturnsNull()
        => Assert.Null(new CubeGrid().Get(new Int3(9, 9, 9)));

    [Fact]
    public void RemovingTheBottomDropsEverythingAbove()
    {
        var grid = BuildColumn(Color.Red, Color.Green, Color.Blue);
        var bottom = grid.Get(new Int3(0, 0, 0))!;
        var middle = grid.Get(new Int3(0, 1, 0))!;
        var top = grid.Get(new Int3(0, 2, 0))!;

        var moved = grid.RemoveAndCollapse([bottom]);

        Assert.Same(middle, grid.Get(new Int3(0, 0, 0)));
        Assert.Same(top, grid.Get(new Int3(0, 1, 0)));
        Assert.Null(grid.Get(new Int3(0, 2, 0)));

        // Each survivor reports how far it fell, so the caller can move the body to match
        Assert.Equal(2, moved.Count);
        Assert.All(moved, entry => Assert.Equal(1, entry.Dropped));
    }

    [Fact]
    public void CubesFallByTheNumberOfHolesBeneathThem()
    {
        var grid = BuildColumn(Color.Red, Color.Red, Color.Green, Color.Blue);
        var first = grid.Get(new Int3(0, 0, 0))!;
        var second = grid.Get(new Int3(0, 1, 0))!;
        var survivor = grid.Get(new Int3(0, 3, 0))!;

        var moved = grid.RemoveAndCollapse([first, second]);

        // The top cube had two cleared below it, so it drops two - not one
        var entry = Assert.Single(moved, m => m.Cube == survivor);

        Assert.Equal(2, entry.Dropped);
        Assert.Equal(new Int3(0, 1, 0), survivor.Get<CubeComponent>()!.GridPosition);
    }

    [Fact]
    public void RemovingTheTopMovesNothing()
    {
        var grid = BuildColumn(Color.Red, Color.Green, Color.Blue);
        var top = grid.Get(new Int3(0, 2, 0))!;

        var moved = grid.RemoveAndCollapse([top]);

        Assert.Empty(moved);
        Assert.Equal(2, grid.Count);
    }

    [Fact]
    public void ColumnsCollapseIndependently()
    {
        var grid = new CubeGrid();

        grid.Add(new Int3(0, 0, 0), MakeCube(Color.Red));
        grid.Add(new Int3(0, 1, 0), MakeCube(Color.Green));
        grid.Add(new Int3(5, 0, 5), MakeCube(Color.Blue));
        grid.Add(new Int3(5, 1, 5), MakeCube(Color.Blue));

        var untouchedBottom = grid.Get(new Int3(5, 0, 5))!;
        var untouchedTop = grid.Get(new Int3(5, 1, 5))!;

        grid.RemoveAndCollapse([grid.Get(new Int3(0, 0, 0))!]);

        Assert.Same(untouchedBottom, grid.Get(new Int3(5, 0, 5)));
        Assert.Same(untouchedTop, grid.Get(new Int3(5, 1, 5)));
    }

    [Fact]
    public void ClearingAWholeColumnLeavesItEmpty()
    {
        var grid = BuildColumn(Color.Red, Color.Red);

        grid.RemoveAndCollapse([grid.Get(new Int3(0, 0, 0))!, grid.Get(new Int3(0, 1, 0))!]);

        Assert.Equal(0, grid.Count);
        Assert.Null(grid.Get(new Int3(0, 0, 0)));
    }

    [Fact]
    public void AlreadyClearedCubeDoesNotEvictItsSuccessor()
    {
        // A stale entity must not remove whichever cube has since fallen into its old slot, or a
        // second clear touching the same space would delete an innocent cube
        var grid = BuildColumn(Color.Red, Color.Green);
        var bottom = grid.Get(new Int3(0, 0, 0))!;

        grid.RemoveAndCollapse([bottom]);

        var settled = grid.Get(new Int3(0, 0, 0));

        grid.RemoveAndCollapse([bottom]);

        Assert.Same(settled, grid.Get(new Int3(0, 0, 0)));
        Assert.Equal(1, grid.Count);
    }
}
using Example_CubicleCalamity.Components;
using Example_CubicleCalamity.Gameplay;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;

namespace Stride.CommunityToolkit.Tests.CubicleCalamity;

/// <summary>
/// Covers the connected-group search.
/// </summary>
public class MatchFinderTests
{
    private static Entity MakeCube(Color colour)
    {
        var entity = new Entity("Cube");

        entity.Add(new CubeComponent(colour));

        return entity;
    }

    private static Entity Place(CubeGrid grid, Int3 at, Color colour)
    {
        var cube = MakeCube(colour);

        grid.Add(at, cube);

        return cube;
    }

    [Fact]
    public void AloneCubeIsItsOwnGroup()
    {
        var grid = new CubeGrid();
        var cube = Place(grid, new Int3(0, 0, 0), Color.Red);

        var group = MatchFinder.FindGroup(grid, cube);

        Assert.Single(group);
        Assert.False(MatchFinder.IsClearable(group.Count));
    }

    [Fact]
    public void NeighboursOfTheSameColourJoinTheGroup()
    {
        var grid = new CubeGrid();
        var start = Place(grid, new Int3(0, 0, 0), Color.Red);

        Place(grid, new Int3(1, 0, 0), Color.Red);
        Place(grid, new Int3(2, 0, 0), Color.Red);

        var group = MatchFinder.FindGroup(grid, start);

        Assert.Equal(3, group.Count);
        Assert.True(MatchFinder.IsClearable(group.Count));
    }

    [Fact]
    public void DifferentColoursStopTheSearch()
    {
        var grid = new CubeGrid();
        var start = Place(grid, new Int3(0, 0, 0), Color.Red);

        Place(grid, new Int3(1, 0, 0), Color.Green);
        Place(grid, new Int3(2, 0, 0), Color.Red);

        var group = MatchFinder.FindGroup(grid, start);

        Assert.Single(group);
    }

    [Fact]
    public void MatchingIsThreeDimensional()
    {
        // Cubes stacked on top of each other count as neighbours, not only those side by side
        var grid = new CubeGrid();
        var start = Place(grid, new Int3(0, 0, 0), Color.Blue);

        Place(grid, new Int3(0, 1, 0), Color.Blue);
        Place(grid, new Int3(0, 0, 1), Color.Blue);

        var group = MatchFinder.FindGroup(grid, start);

        Assert.Equal(3, group.Count);
    }

    [Fact]
    public void DiagonalsAreNotNeighbours()
    {
        var grid = new CubeGrid();
        var start = Place(grid, new Int3(0, 0, 0), Color.Red);

        Place(grid, new Int3(1, 1, 0), Color.Red);

        Assert.Single(MatchFinder.FindGroup(grid, start));
    }

    [Fact]
    public void GroupIsFoundAroundCornersAndContainsNoDuplicates()
    {
        // An L shape, which a naive search that does not track visited coordinates would revisit
        var grid = new CubeGrid();
        var start = Place(grid, new Int3(0, 0, 0), Color.Red);

        Place(grid, new Int3(1, 0, 0), Color.Red);
        Place(grid, new Int3(1, 1, 0), Color.Red);
        Place(grid, new Int3(1, 2, 0), Color.Red);

        var group = MatchFinder.FindGroup(grid, start);

        Assert.Equal(4, group.Count);
        Assert.Equal(group.Count, group.Distinct().Count());
    }

    [Fact]
    public void SearchStartsFromWhicheverCubeWasClicked()
    {
        var grid = new CubeGrid();

        Place(grid, new Int3(0, 0, 0), Color.Red);

        var middle = Place(grid, new Int3(1, 0, 0), Color.Red);

        Place(grid, new Int3(2, 0, 0), Color.Red);

        Assert.Equal(3, MatchFinder.FindGroup(grid, middle).Count);
    }

    [Fact]
    public void CubeWithNoComponentYieldsNothing()
        => Assert.Empty(MatchFinder.FindGroup(new CubeGrid(), new Entity("NotACube")));

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(50, true)]
    public void ClearableNeedsTwoOrMore(int groupSize, bool expected)
        => Assert.Equal(expected, MatchFinder.IsClearable(groupSize));

    [Fact]
    public void EmptyBoardHasNoMoves()
        => Assert.False(MatchFinder.HasClearableGroup(new CubeGrid()));

    [Fact]
    public void BoardOfOrphansHasNoMoves()
    {
        // Every cube isolated by colour: nothing can ever be cleared again, which is the state the
        // game has to notice and announce rather than silently stop responding
        var grid = new CubeGrid();

        Place(grid, new Int3(0, 0, 0), Color.Red);
        Place(grid, new Int3(1, 0, 0), Color.Green);
        Place(grid, new Int3(2, 0, 0), Color.Blue);
        Place(grid, new Int3(3, 0, 0), Color.Red);

        Assert.False(MatchFinder.HasClearableGroup(grid));
    }

    [Fact]
    public void OneRemainingPairIsStillAMove()
    {
        var grid = new CubeGrid();

        Place(grid, new Int3(0, 0, 0), Color.Red);
        Place(grid, new Int3(1, 0, 0), Color.Green);
        Place(grid, new Int3(5, 0, 5), Color.Blue);
        Place(grid, new Int3(5, 1, 5), Color.Blue);

        Assert.True(MatchFinder.HasClearableGroup(grid));
    }

    [Fact]
    public void SingleCubeIsNotAMove()
    {
        var grid = new CubeGrid();

        Place(grid, new Int3(0, 0, 0), Color.Red);

        Assert.False(MatchFinder.HasClearableGroup(grid));
    }

    [Fact]
    public void EmptyBoardHasZeroMovesToCount()
        => Assert.Equal(0, MatchFinder.CountClearableGroups(new CubeGrid()));

    [Fact]
    public void MoveCountIgnoresStrandedSingles()
    {
        // Two clearable pairs and two stranded singles: two moves, not four groups
        var grid = new CubeGrid();

        Place(grid, new Int3(0, 0, 0), Color.Red);
        Place(grid, new Int3(1, 0, 0), Color.Red);
        Place(grid, new Int3(5, 0, 0), Color.Blue);
        Place(grid, new Int3(5, 1, 0), Color.Blue);
        Place(grid, new Int3(3, 0, 0), Color.Green);
        Place(grid, new Int3(8, 0, 8), Color.Red);

        Assert.Equal(2, MatchFinder.CountClearableGroups(grid));
    }

    [Fact]
    public void OneConnectedBoardIsOneMove()
    {
        var grid = new CubeGrid();

        for (var x = 0; x < 4; x++)
        {
            Place(grid, new Int3(x, 0, 0), Color.Red);
        }

        Assert.Equal(1, MatchFinder.CountClearableGroups(grid));
    }

    [Fact]
    public void TouchingGroupsOfDifferentColoursCountSeparately()
    {
        var grid = new CubeGrid();

        Place(grid, new Int3(0, 0, 0), Color.Red);
        Place(grid, new Int3(1, 0, 0), Color.Red);
        Place(grid, new Int3(2, 0, 0), Color.Green);
        Place(grid, new Int3(3, 0, 0), Color.Green);

        Assert.Equal(2, MatchFinder.CountClearableGroups(grid));
    }

    [Fact]
    public void EveryCubeIsVisitedOnlyOnceAcrossGroups()
    {
        // A large single-colour board must still terminate and report a move; this would spin or
        // recount if the visited set were not shared across the fills
        var grid = new CubeGrid();

        for (var x = 0; x < 8; x++)
        {
            for (var z = 0; z < 8; z++)
            {
                Place(grid, new Int3(x, 0, z), Color.Red);
            }
        }

        Assert.True(MatchFinder.HasClearableGroup(grid));
    }
}

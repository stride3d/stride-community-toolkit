using Example_CubicleCalamity.Components;
using Example_CubicleCalamity.Shared;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example_CubicleCalamity.Gameplay;

/// <summary>
/// Finds the group of same-coloured cubes connected to the one that was clicked.
/// </summary>
/// <remarks>
/// A breadth-first flood fill across the six face neighbours, over <see cref="CubeGrid"/> rather than
/// over transform positions. Working in integers means there is no tolerance to tune and no way for a
/// cube in mid-fall to be missed, and it means this can be tested without a running game.
/// </remarks>
public static class MatchFinder
{
    private static readonly Int3[] _neighbours =
    [
        new(1, 0, 0), new(-1, 0, 0),
        new(0, 1, 0), new(0, -1, 0),
        new(0, 0, 1), new(0, 0, -1),
    ];

    /// <summary>
    /// Returns every cube connected to <paramref name="start"/> through neighbours of the same colour,
    /// including the starting cube.
    /// </summary>
    /// <param name="grid">The grid to search.</param>
    /// <param name="start">The cube that was clicked.</param>
    /// <returns>The connected group. Empty if the cube is not a playable cube.</returns>
    public static List<Entity> FindGroup(CubeGrid grid, Entity start)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(start);

        var group = new List<Entity>();
        var startComponent = start.Get<CubeComponent>();

        if (startComponent is null) return group;

        var colour = startComponent.Color;
        var visited = new HashSet<Int3>();
        var pending = new Queue<Int3>();

        pending.Enqueue(startComponent.GridPosition);
        visited.Add(startComponent.GridPosition);

        while (pending.TryDequeue(out var coordinate))
        {
            var cube = grid.Get(coordinate);

            if (cube is null) continue;

            group.Add(cube);

            foreach (var offset in _neighbours)
            {
                var next = coordinate + offset;

                if (!visited.Add(next)) continue;

                var neighbour = grid.Get(next);

                if (neighbour?.Get<CubeComponent>()?.Color != colour)
                {
                    continue;
                }

                pending.Enqueue(next);
            }
        }

        return group;
    }

    /// <summary>
    /// Returns whether a group is large enough to clear.
    /// </summary>
    /// <param name="groupSize">How many cubes are connected.</param>
    /// <returns><see langword="true"/> when the group may be cleared.</returns>
    /// <remarks>
    /// A lone cube is not a match. Beyond following the genre, this removes a discontinuity from the
    /// scoring curve - the group bonus is zero at one cube, so a single click used to be worth a flat
    /// ten points while two were worth sixty.
    /// </remarks>
    public static bool IsClearable(int groupSize) => groupSize >= GameSettings.MinimumGroupSize;

    /// <summary>
    /// Returns whether any move remains, meaning at least one group is still large enough to clear.
    /// </summary>
    /// <param name="grid">The grid to check.</param>
    /// <returns><see langword="true"/> while the board can still be played.</returns>
    /// <remarks>
    /// <para>
    /// Requiring groups of two or more means single cubes with no matching neighbour can never be
    /// removed, so a board always ends with some left over and eventually reaches a state where
    /// nothing at all can be cleared. That is normal for the genre, but it needs saying out loud -
    /// without this check the game simply stops responding to clicks and never explains why.
    /// </para>
    /// <para>
    /// Every cube is visited at most once across all the fills, because each one is marked as it is
    /// reached and whole groups are skipped once seen - so this costs one pass over the board however
    /// many groups it is divided into. It deliberately measures real group sizes rather than looking
    /// for a matching neighbour, which would only be a valid shortcut while the minimum is exactly
    /// two.
    /// </para>
    /// </remarks>
    public static bool HasClearableGroup(CubeGrid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);

        var visited = new HashSet<Int3>();

        foreach (var (coordinate, cube) in grid.Cubes)
        {
            if (visited.Contains(coordinate)) continue;

            var colour = cube.Get<CubeComponent>()?.Color;

            if (colour is null) continue;

            if (IsClearable(MeasureGroup(grid, coordinate, colour.Value, visited)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Counts how many clearable groups the board still holds - the player's remaining moves.
    /// </summary>
    /// <param name="grid">The grid to count.</param>
    /// <returns>The number of distinct groups large enough to clear.</returns>
    /// <remarks>
    /// The same single pass as <see cref="HasClearableGroup"/>, minus the early exit: every cube is
    /// visited once whatever the answer is. Shown on the HUD so the endgame becomes a hunt for the
    /// last few moves rather than a guess about whether any remain.
    /// </remarks>
    public static int CountClearableGroups(CubeGrid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);

        var visited = new HashSet<Int3>();
        var moves = 0;

        foreach (var (coordinate, cube) in grid.Cubes)
        {
            if (visited.Contains(coordinate)) continue;

            var colour = cube.Get<CubeComponent>()?.Color;

            if (colour is null) continue;

            if (IsClearable(MeasureGroup(grid, coordinate, colour.Value, visited)))
            {
                moves++;
            }
        }

        return moves;
    }

    /// <summary>
    /// Counts the connected same-coloured group containing a coordinate, marking each cell visited.
    /// </summary>
    private static int MeasureGroup(CubeGrid grid, Int3 start, Color colour, HashSet<Int3> visited)
    {
        var size = 0;
        var pending = new Queue<Int3>();

        pending.Enqueue(start);
        visited.Add(start);

        while (pending.TryDequeue(out var coordinate))
        {
            size++;

            foreach (var offset in _neighbours)
            {
                var next = coordinate + offset;

                if (visited.Contains(next)) continue;

                if (grid.Get(next)?.Get<CubeComponent>()?.Color != colour) continue;

                visited.Add(next);
                pending.Enqueue(next);
            }
        }

        return size;
    }
}
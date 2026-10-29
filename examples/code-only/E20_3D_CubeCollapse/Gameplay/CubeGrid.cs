using CubeCollapse.Components;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace CubeCollapse.Gameplay;

/// <summary>
/// Where every cube is, as whole-number column and layer indices.
/// </summary>
/// <remarks>
/// <para>
/// This is the game's source of truth; the physics is presentation.
/// </para>
/// <para>
/// Comparing cube positions instead needs a float tolerance, and a collapsing stack sits between
/// slots, so a click during the fall finds some neighbours and misses others; rounding to the nearer
/// slot changes from frame to frame. The grid updates the moment a clear happens, so the next click
/// matches the finished layout while the cubes are still falling into it.
/// </para>
/// </remarks>
public class CubeGrid
{
    private readonly Dictionary<Int3, Entity> _cubes = [];

    /// <summary>
    /// Gets the number of cubes currently in the grid.
    /// </summary>
    public int Count => _cubes.Count;

    /// <summary>
    /// Gets every occupied coordinate paired with the cube standing on it.
    /// </summary>
    public IReadOnlyDictionary<Int3, Entity> Cubes => _cubes;

    /// <summary>
    /// Forgets every cube, for a fresh board. The entities themselves are the caller's to remove.
    /// </summary>
    public void Clear() => _cubes.Clear();

    /// <summary>
    /// Places a cube at a grid coordinate and records that coordinate on the cube itself.
    /// </summary>
    /// <param name="coordinate">Column and layer indices.</param>
    /// <param name="cube">The cube entity.</param>
    public void Add(Int3 coordinate, Entity cube)
    {
        ArgumentNullException.ThrowIfNull(cube);

        _cubes[coordinate] = cube;
        cube.Get<CubeComponent>()?.GridPosition = coordinate;
    }

    /// <summary>
    /// Returns the cube at a coordinate, or <see langword="null"/> if the space is empty.
    /// </summary>
    /// <param name="coordinate">Column and layer indices.</param>
    /// <returns>The cube occupying that space, if any.</returns>
    public Entity? Get(Int3 coordinate) => _cubes.GetValueOrDefault(coordinate);

    /// <summary>
    /// Removes a set of cubes and lets everything above each gap fall by as many places as were
    /// cleared beneath it.
    /// </summary>
    /// <param name="cleared">The cubes being removed.</param>
    /// <returns>
    /// Every cube that moved, paired with the number of layers it dropped, so the caller can move the
    /// physics bodies to match.
    /// </returns>
    /// <remarks>
    /// Columns are independent, so this walks each affected column upwards once, which keeps a large
    /// clear from being quadratic.
    /// </remarks>
    public List<(Entity Cube, int Dropped)> RemoveAndCollapse(IEnumerable<Entity> cleared)
    {
        ArgumentNullException.ThrowIfNull(cleared);

        var columns = new HashSet<Int2>();

        foreach (var cube in cleared)
        {
            var component = cube.Get<CubeComponent>();

            if (component is null) continue;

            var coordinate = component.GridPosition;

            // Only forget the cube if it is still the one recorded here. A stale entity - one already
            // cleared - must not evict whichever cube has since fallen into its place.
            if (_cubes.TryGetValue(coordinate, out var occupant) && occupant == cube)
            {
                _cubes.Remove(coordinate);
            }

            columns.Add(new Int2(coordinate.X, coordinate.Z));
        }

        var moved = new List<(Entity, int)>();

        foreach (var column in columns)
        {
            CollapseColumn(column, moved);
        }

        return moved;
    }

    private void CollapseColumn(Int2 column, List<(Entity, int)> moved)
    {
        var writeLayer = 0;

        for (var readLayer = 0; readLayer < Shared.GameSettings.MaxLayers; readLayer++)
        {
            var from = new Int3(column.X, readLayer, column.Y);

            if (!_cubes.TryGetValue(from, out var cube)) continue;

            if (readLayer != writeLayer)
            {
                var to = new Int3(column.X, writeLayer, column.Y);

                _cubes.Remove(from);
                _cubes[to] = cube;
                cube.Get<CubeComponent>()?.GridPosition = to;

                moved.Add((cube, readLayer - writeLayer));
            }

            writeLayer++;
        }
    }
}
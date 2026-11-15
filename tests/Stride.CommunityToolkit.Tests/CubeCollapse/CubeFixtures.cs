using CubeCollapse.Components;
using CubeCollapse.Gameplay;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.Tests.CubeCollapse;

/// <summary>
/// The cubes the Cube Collapse tests build: a bare entity with a colour, alone or placed in a grid.
/// </summary>
internal static class CubeFixtures
{
    /// <summary>A cube of the given colour, in no grid and no scene.</summary>
    internal static Entity MakeCube(Color colour) => new("Cube") { new CubeComponent(colour) };

    /// <summary>A cube of the given colour, added to <paramref name="grid"/> at <paramref name="at"/>.</summary>
    internal static Entity Place(CubeGrid grid, Int3 at, Color colour)
    {
        var cube = MakeCube(colour);

        grid.Add(at, cube);

        return cube;
    }
}
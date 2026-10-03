using Stride.Engine;
using Stride.Input;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// Adds a <see cref="ReferenceGrid"/> to a game.
/// </summary>
public static class ReferenceGridExtensions
{
    /// <summary>
    /// Adds a grid with numbered lines that shows world coordinates or screen pixels, for finding
    /// your way around a scene while building it.
    /// </summary>
    /// <param name="game">The game to add the grid to.</param>
    /// <param name="space">The coordinates the grid starts in.</param>
    /// <param name="toggleKey">
    /// The key that steps through off, world and screen. Pass <see cref="Keys.None"/> for no key,
    /// and switch from code with <see cref="ReferenceGrid.Cycle"/>, <see cref="ReferenceGrid.Visible"/>
    /// and <see cref="ReferenceGrid.Space"/>.
    /// </param>
    /// <returns>The grid, whose properties can be changed at any time.</returns>
    /// <remarks>
    /// Call after the graphics compositor and a camera exist, from the Start callback. The grid
    /// follows the first camera of the compositor.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The game has no scene yet.</exception>
    /// <example>
    /// <code>
    /// game.SetupBase3DScene();
    ///
    /// var grid = game.AddGrid();
    /// </code>
    /// </example>
    public static ReferenceGrid AddGrid(this Game game, GridSpace space = GridSpace.World, Keys toggleKey = Keys.G)
    {
        ArgumentNullException.ThrowIfNull(game);

        var scene = game.SceneSystem.SceneInstance?.RootScene
            ?? throw new InvalidOperationException("The game has no scene yet; add the grid from the Start callback or later.");

        var grid = new ReferenceGrid { Space = space, ToggleKey = toggleKey };

        scene.Entities.Add(new Entity("Reference grid") { grid });

        return grid;
    }
}
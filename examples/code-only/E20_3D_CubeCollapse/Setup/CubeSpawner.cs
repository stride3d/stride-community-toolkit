using CubeCollapse.Components;
using CubeCollapse.Gameplay;
using CubeCollapse.Shared;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;

namespace CubeCollapse.Setup;

/// <summary>
/// Builds the platform one layer at a time, at whatever size the current level asks for.
/// </summary>
/// <remarks>
/// Spawning is kept apart from the game loop that decides <em>when</em> to spawn, so the rule ("a new
/// layer every <see cref="GameSettings.Interval"/> seconds until the level's board is full") stays
/// readable without the entity-building detail in the way.
/// </remarks>
/// <param name="game">The running game, used to create the cube primitives.</param>
/// <param name="scene">The scene cubes are added to.</param>
/// <param name="grid">The logical grid every spawned cube is recorded in.</param>
/// <param name="levels">The current level, which sets the board's dimensions.</param>
/// <param name="seed">Seed for the colour picker, so a run is reproducible.</param>
public class CubeSpawner(Game game, Scene scene, CubeGrid grid, LevelState levels, int seed)
{
    private readonly Random _random = new(seed);

    private IReadOnlyList<Color> _colours = GameSettings.Colours;
    private IReadOnlyDictionary<Color, Material>? _materials;

    /// <summary>
    /// Sets the palette newly spawned cubes are painted with. Call before the first layer, and again
    /// whenever the player switches palettes.
    /// </summary>
    /// <param name="colours">The palette's colours.</param>
    /// <param name="materials">One material per colour in <paramref name="colours"/>.</param>
    public void UsePalette(IReadOnlyList<Color> colours, IReadOnlyDictionary<Color, Material> materials)
    {
        ArgumentNullException.ThrowIfNull(colours);
        ArgumentNullException.ThrowIfNull(materials);

        _colours = colours;
        _materials = materials;
    }

    /// <summary>
    /// Spawns one full layer of cubes at the given height, sized by the current level.
    /// </summary>
    /// <param name="layer">Which layer this is, counting from zero at the ground.</param>
    public void SpawnLayer(int layer)
    {
        var level = levels.Current;

        for (var x = 0; x < level.Rows; x++)
        {
            for (var z = 0; z < level.Rows; z++)
            {
                var cube = CreateCube(GridToWorld(level, x, layer, z));

                AddCollider(cube);
                grid.Add(new Int3(x, layer, z), cube);

                cube.Scene = scene;
            }
        }
    }

    /// <summary>
    /// Converts a grid coordinate into the world position of that cube's centre, for a given level's
    /// board.
    /// </summary>
    /// <param name="level">The board being played, which sets where its origin is.</param>
    /// <param name="x">Column index along X.</param>
    /// <param name="layer">Layer index, counting from zero at the ground.</param>
    /// <param name="z">Column index along Z.</param>
    /// <returns>The world position of the cube centre for that coordinate.</returns>
    /// <remarks>
    /// <see cref="LevelDefinition.GridOrigin"/> centres the footprint on the ground whatever the
    /// level's size. The half cube on Y is because a coordinate names a cube's centre while layer
    /// zero sits <em>on</em> the ground.
    /// </remarks>
    public static Vector3 GridToWorld(LevelDefinition level, int x, int layer, int z) => new(
        x * GameSettings.CubeSize.X + level.GridOrigin,
        (layer + 0.5f) * GameSettings.CubeSize.Y,
        z * GameSettings.CubeSize.Z + level.GridOrigin);

    private Entity CreateCube(Vector3 position)
    {
        var colour = _colours[_random.Next(0, _colours.Count)];

        var cube = game.CreatePrimitive(Primitive3D.Box(GameSettings.CubeSize), new PrimitiveEntityOptions
        {
            EntityName = EntityNames.Cube,
            Material = _materials?[colour],
            Position = position,

            // The scene setup chose Bepu, so CreatePrimitive would give the cube a dynamic body of its own; the
            // spawner adds the SlidingCubeComponent below instead, and two bodies on one cube push each other apart
            Body = BodyMode.None,
        });

        cube.Add(new CubeComponent(colour));

        return cube;
    }

    private static void AddCollider(Entity entity)
    {
        // A single BoxCollider still has to be wrapped: ColliderBase does not implement ICollider,
        // only CompoundCollider, MeshCollider and EmptyCollider do.
        var compoundCollider = new CompoundCollider();

        compoundCollider.Colliders.Add(new BoxCollider
        {
            Size = GameSettings.CubeSize,
            // An ordinary mass: a huge one (this was 1e9) scales the inertia tensor and puts contact
            // impulses far from where Bepu's absolute epsilons and sleep thresholds are tuned
            Mass = 1,
        });

        // Kinematic until the whole tower is built, so layers hang in the air while they spawn.
        // Nothing here may touch BodyInertia or the velocities: their setters no-op until the entity
        // joins the scene, at which point SlidingCubeComponent.AttachInner locks rotation.
        entity.Add(new SlidingCubeComponent
        {
            Collider = compoundCollider,
            Kinematic = true,
        });
    }
}
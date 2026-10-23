using Stride.CommunityToolkit.Bullet;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace E04_StrideUI_DragAndDrop;

public class PrimitiveGenerator
{
    /// <summary>
    /// Total number of cubes generated.
    /// </summary>
    public int TotalShapes => _totalCubes;

    private readonly Game _game;
    private readonly Scene _scene;
    private readonly Random _random = new();
    private int _totalCubes;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrimitiveGenerator"/> class.
    /// </summary>
    /// <param name="game">The game instance to use for generating cubes.</param>
    /// <param name="scene">The scene where cubes will be generated.</param>
    /// <exception cref="ArgumentNullException">Thrown if game or scene is null.</exception>
    public PrimitiveGenerator(Game game, Scene scene)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
    }

    /// <summary>
    /// Generates the specified number of random primitives and returns the total count.
    /// </summary>
    /// <param name="count">The number of primitives to generate.</param>
    /// <returns>The updated total number of primitives generated.</returns>
    public int Generate(int count, Primitive3D type)
    {
        for (int i = 0; i < count; i++)
        {
            Generate(type);

            _totalCubes++;
        }

        return _totalCubes;
    }

    /// <summary>
    /// Generates a 3D primitive (a cube 0.3 units across by default) in the scene, with optional shape and color.
    /// </summary>
    /// <param name="shape">The shape to generate, with its dimensions (default: a cube 0.3 units across).</param>
    /// <param name="color">The color of the shape (default: random color).</param>
    public void Generate(Primitive3D? shape = null, Color? color = null)
    {
        var selectedColor = color ?? _random.NextColor();

        var entity = _game.CreatePrimitive(shape ?? Primitive3D.Cube(0.3f), new BulletPhysicsOptions
        {
            Material = _game.CreateMaterial(selectedColor),
        });

        ConfigureTransform(entity);

        entity.Scene = _scene;
    }

    public void SubtractTotalCubes(int count)
    {
        _totalCubes -= count;
    }

    /// <summary>
    /// Gives the entity a random position.
    /// </summary>
    /// <param name="entity">The entity to configure.</param>
    private static void ConfigureTransform(Entity entity)
    {
        entity.Transform.Position = VectorHelper.RandomVector3(
            xRange: [-4, 4],
            yRange: [4, 8],
            zRange: [-4, 4]
        );
    }
}
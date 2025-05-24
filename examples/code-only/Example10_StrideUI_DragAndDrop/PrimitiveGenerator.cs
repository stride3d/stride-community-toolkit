using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Helpers;
using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example10_StrideUI_DragAndDrop;

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
    public int Generate(int count, PrimitiveModelType type)
    {
        for (int i = 0; i < count; i++)
        {
            Generate(type);

            _totalCubes++;
        }

        return _totalCubes;
    }

    /// <summary>
    /// Generates a 3D primitive (cube by default) in the scene, with optional type and color.
    /// </summary>
    /// <param name="modelType">The type of the 3D primitive to generate (default: Cube).</param>
    /// <param name="color">The color of the cube (default: random color).</param>
    public void Generate(PrimitiveModelType modelType = PrimitiveModelType.Cube, Color? color = null)
    {
        var selectedColor = color ?? _random.NextColor();

        var entity = _game.Create3DPrimitive(modelType, new Bepu3DPhysicsOptions
        {
            Material = _game.CreateMaterial(selectedColor),
            Size = new Vector3(0.3f)
        });

        ConfigureTransform(entity);

        entity.Scene = _scene;
    }

    public void SubtractTotalCubes(int count)
    {
        _totalCubes -= count;
    }

    /// <summary>
    /// Configures the transform properties (scale and position) of the given entity.
    /// </summary>
    /// <param name="entity">The entity to configure.</param>
    private static void ConfigureTransform(Entity entity)
    {
        // Set a random position within specified bounds
        entity.Transform.Position = VectorHelper.RandomVector3(
            xRange: [-4, 4],
            yRange: [4, 8],
            zRange: [-4, 4]
        );
    }
}
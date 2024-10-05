using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Helpers;
using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example10_StrideUI_DragAndDrop;

public class CubesGenerator
{
    public int TotalCubes => _totalCubes;

    private readonly Game _game;
    private readonly Scene _scene;
    private readonly Random _random = new();
    private int _totalCubes;

    public CubesGenerator(Game game, Scene scene)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
    }

    public int GenerateRandomCubes(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Generate(PrimitiveModelType.Sphere);

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

        var entity = _game.Create3DPrimitive(modelType, new Primitive3DCreationOptions
        {
            Material = _game.CreateMaterial(selectedColor)
        });

        ConfigureTransform(entity);

        entity.Scene = _scene;
    }

    /// <summary>
    /// Configures the transform properties (scale and position) of the given entity.
    /// </summary>
    /// <param name="entity">The entity to configure.</param>
    private static void ConfigureTransform(Entity entity)
    {
        entity.Transform.Scale = new Vector3(0.3f);

        // Set a random position within specified bounds
        entity.Transform.Position = VectorHelper.RandomVector3(
            xRange: [-4, 4],
            yRange: [4, 8],
            zRange: [-4, 4]
        );
    }
}
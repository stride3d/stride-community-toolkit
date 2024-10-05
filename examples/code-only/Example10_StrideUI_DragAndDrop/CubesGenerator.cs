using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Helpers;
using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example10_StrideUI_DragAndDrop;

public class CubesGenerator
{
    private readonly Game _game;
    private readonly Scene _scene;
    private readonly Random _random = new();

    public CubesGenerator(Game game, Scene scene)
    {
        _game = game;
        _scene = scene;
    }

    public void Generate(PrimitiveModelType type = PrimitiveModelType.Cube, Color? color = null)
    {
        var entity = _game.Create3DPrimitive(type, new Primitive3DCreationOptions
        {
            Material = _game.CreateMaterial(color ?? _random.NextColor())
        });

        entity.Transform.Scale = new Vector3(0.3f);
        entity.Transform.Position = VectorHelper.RandomVector3([-4, 4], [4, 8], [-4, 4]);

        entity.Scene = _scene;
    }
}
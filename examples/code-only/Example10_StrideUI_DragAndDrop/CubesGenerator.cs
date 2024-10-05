using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using Stride.Rendering;
using Stride.Rendering.ProceduralModels;

namespace Example10_StrideUI_DragAndDrop;

public class CubesGenerator
{
    private readonly Random _random = new();
    private readonly CubeProceduralModel _cubeModel = new();
    private readonly IServiceRegistry _services;
    private readonly Scene _scene;
    private readonly int _squareSize = 4;
    private readonly int _height = 4;

    public CubesGenerator(IServiceRegistry services, Scene scene)
    {
        _services = services;
        _scene = scene;
    }

    public void Generate()
    {
        var model = new Model();

        _cubeModel.Generate(_services, model);

        var entity = new Entity();

        entity.Transform.Scale = new Vector3(0.3f);
        entity.Transform.Position = new Vector3(
            GetRandomPosition(),
            (float)(_random.NextDouble() * 1) + _height,
            GetRandomPosition());

        entity.GetOrCreate<ModelComponent>().Model = model;

        var rigidBody = entity.GetOrCreate<RigidbodyComponent>();
        rigidBody.ColliderShape = new BoxColliderShape(false, new Vector3(1));

        entity.Scene = _scene;

        float GetRandomPosition()
        {
            return -_squareSize + (float)(_random.NextDouble() * _squareSize * 2);
        }
    }
}

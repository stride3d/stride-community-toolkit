using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Physics;
using Stride.Rendering;

namespace Example_CubeTower;

public class CubeStacker
{
    private const float Interval = 2.0f;
    private const int MaxLayers = 5;
    private const int Rows = 5;
    private readonly Vector3 _cubeSize = new Vector3(1);
    private readonly Game _game;
    private readonly List<Material> _materials = new();
    private readonly Random _random = new();
    private readonly List<Color> _colours = new() { Color.Red, Color.Green, Color.Blue };
    private double _elapsedTime;
    private int _layer = 1;

    public CubeStacker(Game game) => _game = game;

    public void Start(Scene scene)
    {
        _game.SetupBase3DScene();
        _game.AddProfiler();

        CreateMaterials();
        CreateAndCollideRow(0.5f, scene);
        CreateGameManagerEntity(scene);
    }

    private void CreateGameManagerEntity(Scene scene)
    {
        var entity = new Entity("GameManager");

        entity.Add(new RaycastHandler());

        scene.Entities.Add(entity);
    }

    public void Update(Scene scene, GameTime time)
    {
        _elapsedTime += time.Elapsed.TotalSeconds;

        if (_elapsedTime >= Interval && _layer <= MaxLayers)
        {
            _elapsedTime = 0;

            var entities = CreateModelRow(_layer + 0.5f, scene);

            AddColliders(entities);

            _layer++;
        }
    }
    private void CreateMaterials()
        => _materials.AddRange(_colours.Select(colour => _game.CreateMaterial(colour)));

    private void CreateAndCollideRow(float y, Scene scene)
    {
        var entities = CreateModelRow(y, scene);

        AddColliders(entities);
    }

    private List<Entity> CreateModelRow(float y, Scene scene)
    {
        var entities = new List<Entity>();

        for (var x = 0; x < Rows; x++)
        {
            for (var z = 0; z < Rows; z++)
            {
                var entity = GiveMeCube(_game, _cubeSize);

                entity.Transform.Position = new Vector3(x, y, z);

                entity.Scene = scene;

                entities.Add(entity);
            }
        }

        return entities;
    }

    private void AddColliders(List<Entity> entities)
    {
        foreach (var entity in entities)
        {
            var collider = new RigidbodyComponent()
            {
                Mass = 0.01f,
            };

            collider.ColliderShapes.Add(new BoxColliderShapeDesc
            {
                Size = _cubeSize,
            });

            entity.Add(collider);
        }
    }

    private Entity GiveMeCube(Game game, Vector3 size)
        => game.CreatePrimitive(PrimitiveModelType.Cube, "Cube", material: GetRandomMaterial(), includeCollider: false, size: size);

    Material GetRandomMaterial()
        => _materials[_random.Next(0, _materials.Count)];
}

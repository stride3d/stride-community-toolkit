using Example_CubeTower.Components;
using Example_CubeTower.Scripts;
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
    private readonly Vector3 _cubeSize = new Vector3(1);
    private readonly Game _game;
    private readonly Dictionary<Color, Material> _materials = new();
    private readonly Random _random = new();
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
        var entity = new Entity("GameManager")
        {
            new RaycastHandler()
        };

        scene.Entities.Add(entity);
    }

    public void Update(Scene scene, GameTime time)
    {
        _elapsedTime += time.Elapsed.TotalSeconds;

        if (_elapsedTime >= Constants.Interval && _layer <= Constants.MaxLayers)
        {
            _elapsedTime = 0;

            var entities = CreateModelRow(_layer + 0.5f, scene);

            AddColliders(entities);

            _layer++;
        }
    }

    private void CreateMaterials()
    {
        foreach (var color in Constants.Colours)
        {
            var material = _game.CreateMaterial(color);

            _materials.Add(color, material);
        }
    }

    private void CreateAndCollideRow(float y, Scene scene)
    {
        var entities = CreateModelRow(y, scene);

        AddColliders(entities);
    }

    private List<Entity> CreateModelRow(float y, Scene scene)
    {
        var entities = new List<Entity>();

        for (var x = 0; x < Constants.Rows; x++)
        {
            for (var z = 0; z < Constants.Rows; z++)
            {
                var entity = CreateCube(_game, _cubeSize);

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
            var collider = new RigidbodyComponent();

            collider.ColliderShapes.Add(new BoxColliderShapeDesc
            {
                Size = _cubeSize,
            });

            entity.Add(collider);

            collider.LinearVelocity = new Vector3(0, -1f, 0); // Set an initial velocity along the Y-axis
            collider.LinearFactor = new Vector3(0, 1, 0); // Restrict linear motion to the Y-axis
            collider.AngularFactor = Vector3.Zero; // Restrict angular rotation on all axes
        }
    }

    private Entity CreateCube(Game game, Vector3 size)
    {
        var color = Constants.Colours[_random.Next(0, Constants.Colours.Count)];

        var entity = game.CreatePrimitive(PrimitiveModelType.Cube, "Cube", material: _materials[color], includeCollider: false, size: size);

        entity.Add(new CubeComponent(color));

        return entity;
    }
}
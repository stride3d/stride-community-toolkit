using Example_CubicleCalamity.Components;
using Example_CubicleCalamity.Scripts;
using Example_CubicleCalamity.Shared;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Physics;
using Stride.Rendering;
using Stride.Rendering.Colors;
using Stride.Rendering.Lights;

namespace Example_CubicleCalamity;

public class CubeStacker
{
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

        AddMaterials();
        AddGizmo(scene);

        //_translationGizmo = new TranslationGizmo(_game.GraphicsDevice);
        //var gizmoEntity = _translationGizmo.Create(scene);
        //gizmoEntity.Transform.Position = new Vector3(-10, 0, 0);

        AddAllDirectionLighting(scene, intensity: 20f);
        AddFirstLayer(scene, 0.5f);
        AddGameManagerEntity(scene);
    }

    private void AddGizmo(Scene scene)
    {
        var entity = new Entity("MyGizmo");
        entity.AddGizmo(_game.GraphicsDevice, showAxisName: true);
        entity.Transform.Position = new Vector3(-7.5f, 1, -7.5f);
        entity.Scene = scene;
    }

    private static void AddGameManagerEntity(Scene scene)
    {
        var entity = new Entity("GameManager")
        {
            new RaycastHandler()
        };
        entity.Scene = scene;

        //scene.Entities.Add(entity);
    }

    public void Update(Scene scene, GameTime time)
    {
        _elapsedTime += time.Elapsed.TotalSeconds;

        if (_elapsedTime >= Constants.Interval && _layer <= Constants.MaxLayers - 1)
        {
            _elapsedTime = 0;

            var entities = CreateCubeLayer(_layer + 2.5f, scene);

            AddColliders(entities);

            _layer++;
        }
    }

    private void AddMaterials()
    {
        foreach (var color in Constants.Colours)
        {
            var material = _game.CreateMaterial(color);

            _materials.Add(color, material);
        }
    }

    private void AddFirstLayer(Scene scene, float y)
    {
        var entities = CreateCubeLayer(y, scene);

        AddColliders(entities);
    }

    private List<Entity> CreateCubeLayer(float y, Scene scene)
    {
        var entities = new List<Entity>();

        for (var x = 0; x < Constants.Rows; x++)
            for (var z = 0; z < Constants.Rows; z++)
            {
                var entity = CreateCube(_game, Constants.CubeSize);

                entity.Transform.Position = new Vector3(x, y, z) * Constants.CubeSize;

                entity.Scene = scene;

                //entity.AddGizmo(_game.GraphicsDevice);

                //entity.Transform.Children.Add(_translationGizmo.Create(scene).Transform);

                entities.Add(entity);
            }

        return entities;
    }

    private static void AddColliders(List<Entity> entities)
    {
        foreach (var entity in entities)
        {
            var collider = new RigidbodyComponent();

            collider.ColliderShapes.Add(new BoxColliderShapeDesc
            {
                Size = Constants.CubeSize,
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

        var entity = game.Create3DPrimitive(PrimitiveModelType.Cube, new()
        {
            EntityName = "Cube",
            Material = _materials[color],
            IncludeCollider = false,
            Size = size
        });

        entity.Add(new CubeComponent(color));

        return entity;
    }

    public void AddAllDirectionLighting(Scene scene, float intensity, bool showLightGizmo = true)
    {
        var position = new Vector3(7f, 2f, 0);

        CreateLightEntity(GetLight(), intensity, position);

        CreateLightEntity(GetLight(), intensity, position, Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(180)));

        CreateLightEntity(GetLight(), intensity, position, Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(270)));

        CreateLightEntity(GetLight(), intensity, position, Quaternion.RotationAxis(Vector3.UnitY, MathUtil.DegreesToRadians(90)));

        CreateLightEntity(GetLight(), intensity, position, Quaternion.RotationAxis(Vector3.UnitY, MathUtil.DegreesToRadians(270)));

        LightDirectional GetLight() => new() { Color = GetColor(Color.White) };

        static ColorRgbProvider GetColor(Color color) => new(color);

        void CreateLightEntity(ILight light, float intensity, Vector3 position, Quaternion? rotation = null)
        {
            var entity = new Entity() {
                new LightComponent {
                    Intensity =  intensity,
                    Type = light
                }};

            entity.Transform.Position = position;
            entity.Transform.Rotation = rotation ?? Quaternion.Identity;
            entity.Scene = scene;

            if (showLightGizmo)
                entity.AddLightDirectionalGizmo(_game.GraphicsDevice);
        }
    }
}
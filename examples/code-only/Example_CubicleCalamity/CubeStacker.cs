using CubicleCalamity.Components;
using CubicleCalamity.Scripts;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Gizmos;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Physics;
using Stride.Rendering;
using Stride.Rendering.Colors;
using Stride.Rendering.Lights;

namespace CubicleCalamity;

public class CubeStacker
{
    private readonly Game _game;
    private readonly Dictionary<Color, Material> _materials = new();
    private readonly Random _random = new();
    private TranslationGizmo? _translationGizmo;
    private double _elapsedTime;
    private int _layer = 1;

    public CubeStacker(Game game) => _game = game;

    public void Start(Scene scene)
    {
        _game.SetupBase3DScene();
        _game.AddProfiler();

        CreateMaterials();

        _translationGizmo = new TranslationGizmo(_game.GraphicsDevice);
        var gizmoEntity = _translationGizmo.Create(scene);
        gizmoEntity.Transform.Position = new Vector3(10, 0, 0);

        //SetupLighting(scene);
        CreateFirstLayer(0.5f, scene);
        CreateGameManagerEntity(scene);
    }

    private static void CreateGameManagerEntity(Scene scene)
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

        if (_elapsedTime >= Constants.Interval && _layer <= Constants.MaxLayers - 1)
        {
            _elapsedTime = 0;

            var entities = CreateCubeLayer(_layer + 2.5f, scene);

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

    private void CreateFirstLayer(float y, Scene scene)
    {
        var entities = CreateCubeLayer(y, scene);

        AddColliders(entities);
    }

    private List<Entity> CreateCubeLayer(float y, Scene scene)
    {
        var entities = new List<Entity>();

        for (var x = 0; x < Constants.Rows; x++)
        {
            for (var z = 0; z < Constants.Rows; z++)
            {
                var entity = CreateCube(_game, Constants.CubeSize);

                entity.Transform.Position = new Vector3(x, y, z) * Constants.CubeSize;

                entity.Scene = scene;

                //entity.Transform.Children.Add(_translationGizmo.Create(scene).Transform);

                entities.Add(entity);
            }
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

        var entity = game.CreatePrimitive(PrimitiveModelType.Cube, "Cube", material: _materials[color], includeCollider: false, size: size);

        entity.Add(new CubeComponent(color));

        return entity;
    }

    public static void SetupLighting(Scene scene)
    {
        CreateLight(new LightAmbient
        {
            Color = GetColor(new(0.2f, 0.2f, 0.2f))
        }, 1f, new Vector3(0, 20, 0));

        CreateLight(new LightDirectional
        {
            Color = GetColor(new(1f, 1f, 1f)),
        }, 100f, new Vector3(-20f, 5f, -20f));

        CreateLight(new LightDirectional
        {
            Color = GetColor(new(1f, 1f, 1f)),
        }, 100f, new Vector3(20f, 5f, 20f));

        CreateLight(new LightDirectional
        {
            Color = GetColor(new(1f, 1f, 1f)),
        }, 100f, new Vector3(-20f, 5f, 20f));

        CreateLight(new LightDirectional
        {
            Color = GetColor(new(1f, 1f, 1f)),
        }, 100f, new Vector3(20f, 5f, -20f));

        static ColorRgbProvider GetColor(Color color) => new(color);

        void CreateLight(ILight light, float intensity, Vector3 position)
        {
            var entity = new Entity() {
                new LightComponent {
                    Intensity =  intensity,
                    Type = light
                }};

            entity.Transform.Position = position;
            entity.Scene = scene;
        }
    }
}
using Example_CubicleCalamity.Components;
using Example_CubicleCalamity.Scripts;
using Example_CubicleCalamity.Shared;
using Example_CubicleCalamity_BulletPhysics;
using Stride.CommunityToolkit.Bullet;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Physics;
using Stride.Rendering;
using Stride.Rendering.Colors;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Example_CubicleCalamity;

public class CubeStacker
{
    private readonly Game _game;
    private readonly Dictionary<Color, Material> _materials = [];
    private const int Seed = 1;
    private readonly Random _random = new(Seed);
    private double _elapsedTime;
    private int _layer = 1;
    private bool _layersCreated;

    public CubeStacker(Game game) => _game = game;

    public void Start(Scene scene)
    {
        //_game.SetupBase3DScene();
        _game.Window.AllowUserResizing = true;
        _game.AddGraphicsCompositor().AddCleanUIStage();
        _game.Add3DCamera().Add3DCameraController();
        //_game.AddEntityDebugSceneRenderer(new()
        //{
        //    ShowFontBackground = false
        //});
        _game.AddSceneRenderer(new EntityTextRenderer());
        _game.AddDirectionalLight();
        _game.Add3DGround();
        _game.AddProfiler();

        AddMaterials();
        AddGizmo(scene);

        //_translationGizmo = new TranslationGizmo(_game.GraphicsDevice);
        //var gizmoEntity = _translationGizmo.Create(scene);
        //gizmoEntity.Transform.Position = new Vector3(-10, 0, 0);

        AddAllDirectionLighting(scene, intensity: 5f);
        AddFirstLayer(scene, 0.5f);
        AddGameManagerEntity(scene);
        AddTotalScoreEntity(scene);

        var camera = scene.GetCamera();
        camera?.Entity.Add(new CameraRotationScript());
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
            new RaycastInteractionScript()
        };
        entity.Scene = scene;
    }

    private static void AddTotalScoreEntity(Scene scene)
    {
        var entity = new Entity(Constants.TotalScore)
        {
            new EntityTextComponent()
            {
                Text = "Total Score: 0",
                FontSize = 20,
                Position = new Vector2(0, 20),
                TextColor = new Color(255, 255, 255),
            }
        };

        entity.Scene = scene;
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
            var material = CreateMaterial(color);

            _materials.Add(color, material);
        }
    }

    public Material CreateMaterial(Color? color = null, float specular = 1.0f, float microSurface = 0.65f)
    {
        var materialDescription = new MaterialDescriptor
        {
            Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(color ?? GameDefaults.DefaultMaterialColor)),
                    DiffuseModel = new MaterialLightmapModelFeature(),
                    Specular =  new MaterialMetalnessMapFeature(new ComputeFloat(specular)),
                    SpecularModel = new MaterialSpecularMicrofacetModelFeature(),
                    MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(microSurface))
                }
        };

        var materialDescription3 = new MaterialDescriptor
        {
            Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(color ?? GameDefaults.DefaultMaterialColor)),
                    DiffuseModel = new MaterialLightmapModelFeature(),
                    Specular =  new MaterialMetalnessMapFeature(new ComputeFloat(0)),
                                  SpecularModel = new MaterialSpecularMicrofacetModelFeature()
                    {
                        Fresnel = new MaterialSpecularMicrofacetFresnelSchlick(),
                        Visibility = new MaterialSpecularMicrofacetVisibilitySmithSchlickGGX(),
                        NormalDistribution = new MaterialSpecularMicrofacetNormalDistributionGGX(),
                        Environment = new MaterialSpecularMicrofacetEnvironmentGGXLUT(),
                    },
                    MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0))
                }
        };

        var materialDescription2 = new MaterialDescriptor
        {
            Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(color ?? GameDefaults.DefaultMaterialColor)),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Specular =  new MaterialMetalnessMapFeature(new ComputeFloat(0)),
                    SpecularModel = new MaterialSpecularMicrofacetModelFeature()
                    {
                        Fresnel = new MaterialSpecularMicrofacetFresnelSchlick(),
                        Visibility = new MaterialSpecularMicrofacetVisibilitySmithSchlickGGX(),
                        NormalDistribution = new MaterialSpecularMicrofacetNormalDistributionGGX(),
                        Environment = new MaterialSpecularMicrofacetEnvironmentGGXLUT(),
                    },
                    MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0)),
                }
        };

        return Material.New(_game.GraphicsDevice, materialDescription);
        //options.Size /= 2;

    }

    private void AddFirstLayer(Scene scene, float y)
    {
        var entities = CreateCubeLayer(y, scene);

        //AddColliders(entities);
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
            AddCollider(entity);
        }
    }

    private static void AddCollider(Entity entity)
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

    private Entity CreateCube(Game game, Vector3 size)
    {
        var color = Constants.Colours[_random.Next(0, Constants.Colours.Count)];

        var entity = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions()
        {
            EntityName = "Cube",
            Material = _materials[color],
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
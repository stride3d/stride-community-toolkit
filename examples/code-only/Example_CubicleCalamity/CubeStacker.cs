using BepuPhysics;
using Example_CubicleCalamity.Components;
using Example_CubicleCalamity.Scripts;
using Example_CubicleCalamity.Shared;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Graphics;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Colors;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Example_CubicleCalamity;

public class CubeStacker
{
    private const int Seed = 1;

    private readonly Game _game;
    private readonly Random _random = new(Seed);
    private readonly Dictionary<Color, Material> _materials = [];
    private double _elapsedTime;
    private int _layer = 1;
    private bool _layersCreated;
    private BepuSimulation? _simulation;
    private Scene? _scene;

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
        _scene = scene;

        AddMaterials();
        AddGizmo(scene);

        //_translationGizmo = new TranslationGizmo(_game.GraphicsDevice);
        //var gizmoEntity = _translationGizmo.Create(scene);
        //gizmoEntity.Transform.Position = new Vector3(-10, 0, 0);

        AddAllDirectionLighting(intensity: 5f);
        AddNewFirstLayer(0.5f);
        AddFirstLayer(0.5f);
        AddGameManagerEntity();
        AddTotalScoreEntity();

        var camera = _scene.GetCamera();
        camera?.Entity.Add(new CameraRotationScript());
        //_simulation = camera?.Entity.GetSimulation().Simulation;
    }

    private void AddGizmo(Scene scene)
    {
        var entity = new Entity("MyGizmo");
        entity.AddGizmo(_game.GraphicsDevice, showAxisName: true);
        entity.Transform.Position = new Vector3(-7.5f, 1, -7.5f);
        entity.Scene = scene;
    }

    private void AddGameManagerEntity()
    {
        var entity = new Entity("GameManager")
        {
            new RaycastInteractionScript()
        };
        entity.Scene = _scene;
    }

    private void AddTotalScoreEntity()
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

        entity.Scene = _scene;
    }

    public void Update(Scene scene, GameTime time)
    {
        _elapsedTime += time.Elapsed.TotalSeconds;

        if (_elapsedTime >= Constants.Interval && _layer <= Constants.MaxLayers - 1)
        {
            _elapsedTime = 0;

            var entities = CreateCubeLayer(_layer + 0.5f);

            //AddColliders(entities);

            _layer++;
        }

        if (!_layersCreated && _layer == Constants.MaxLayers)
        {
            _layersCreated = true;

            foreach (var cube in scene.Entities)
            {
                if (cube.Name != "Cube") continue;

                var body = cube.Get<BodyComponent>();

                body.Kinematic = false;
            }
        }

        foreach (var cube in scene.Entities)
        {
            if (cube.Name != "Cube") continue;

            var body = cube.Get<BodyComponent>();

            //body.AngularVelocity = Vector3.Zero;
            //body.LinearVelocity = Vector3.Zero;

            //body.AngularVelocity = Vector3.Zero;
            //body.LinearVelocity = new(0, -0.1f, 0);

            //  Make all sleeping except those moving down
            //body.Awake = false;
            //body.Gravity = new Vector3(0, -10, 0);
            //body.Kinematic = true;

            //body.BodyInertia = new BodyInertia
            //{
            //    //InverseMass = 1,
            //    InverseInertiaTensor = new BepuUtilities.Symmetric3x3
            //    {
            //        XX = 1,
            //        ZY = 1,
            //        YX = 1,
            //        YY = 1,
            //        ZX = 1,
            //        ZZ = 1
            //    }
            //};

            //if (body.AngularVelocity.LengthSquared() > 0.001f)
            //{
            //    body.AngularVelocity = Vector3.Zero;
            //}

            //if (body.Awake && body.LinearVelocity.LengthSquared() > 0.1f)
            //{
            //    body.LinearVelocity = Vector3.Zero;
            //}

            //body.SpringDampingRatio = 1;
            //body.SpringFrequency = 40;

            //if (body.Awake && body.AngularVelocitys.LengthSquared() > 0.001f)
            //{
            //    //body.BodyInertia = new BodyInertia
            //    //{
            //    //    InverseMass = 1,
            //    //    InverseInertiaTensor = default
            //    //};
            //}

            //var body = _simulatison.Bodies[cube];
            //if (body.Awake && body.AngularVelocity.LengthSquared() > 0.001f)
            //{
            //    body.AngularVelocity = Vector3.Zero;
            //}

            //if (body.Awake && body.LinearVelocity.LengthSquared() > 0.001f)
            //{
            //    //body.LinearVelocity = new Vector3(0, -10, 0);
            //}
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
        var materialDescription2 = new MaterialDescriptor
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

        var materialDescription = new MaterialDescriptor
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

        // from toolkit Stride.CommunityToolkit.Graphics
        var windowSize = _game.GraphicsDevice.GetWindowSize();

        // from Stride.Graphics
        var whiteTexture = _game.GraphicsDevice.GetSharedWhiteTexture();

        return Material.New(_game.GraphicsDevice, materialDescription);
        //options.Size /= 2;

    }

    private void AddNewFirstLayer(float y)
    {
        //var entities = CreateCubeLayer(y, scene);
        //AddColliders(entities);
    }

    private void AddFirstLayer(float y)
    {
        var entities = CreateCubeLayer(y);

        //AddColliders(entities);
    }

    private List<Entity> CreateCubeLayer(float y)
    {
        var entities = new List<Entity>();

        for (var x = 0; x < Constants.Rows; x++)
        {
            for (var z = 0; z < Constants.Rows; z++)
            {
                var entity = CreateCube(Constants.CubeSize);

                entity.Transform.Position = new Vector3(x, y, z) * Constants.CubeSize;

                AddCollider(entity);

                entity.Scene = _scene;

                //entity.AddGizmo(_game.GraphicsDevice);

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
            AddCollider(entity);
        }
    }

    private static void AddCollider(Entity entity)
    {
        var compoundCollider = new CompoundCollider();

        var boxCollider = new BoxCollider
        {
            Size = Constants.CubeSize,
            Mass = 1000000000,
        };

        compoundCollider.Colliders.Add(boxCollider);

        //var zeroInertia = new BodyInertia
        //{
        //    InverseMass = 100,
        //    InverseInertiaTensor = default
        //};

        var body = new BodyComponent
        {
            Collider = compoundCollider,
            //BodyInertia = zeroInertia,
            //AngularVelocity = Vector3.Zero,
            //LinearVelocity = Vector3.Zero,
        };

        // I need to get ShapeIndex with reflection
        //var shapeIndex = body.ShapeIndex;
        //var pos = body.Pose;

        body.Kinematic = true;

        //var pose = body.Pose;

        body.BodyInertia = new BodyInertia
        {
            InverseMass = 1 / boxCollider.Mass,
            InverseInertiaTensor = default
        };

        //body.SleepThreshold = 0.01f;
        //body.MinimumTimestepCountUnderThreshold = 32;

        entity.Add(body);

        //var index = body.Sha

        //body.SpringFrequency = 1;
        //body.SpringDampingRatio = 0;


        //body.AngularVelocity = Vector3.Zero;
        //body.LinearVelocity = Vector3.Zero;
        //body.FrictionCoefficient = 3f;
        //body.MaximumRecoveryVelocity = 10;
        //body.SpringDampingRatio = 10000000;
        //body.SpringFrequency = 1000;
        //body.SpeculativeMargin = 10;

        // This works differently when set here
        //body.BodyInertia = zeroInertia;

        //body.FrictionCoefficient = 2;
        //body.InterpolationMode = InterpolationMode.Interpolated;


        //var bodyDescription = new BodyDescription
        //{
        //    LocalInertia = zeroInertia,
        //    Pose = new RigidPose
        //    {
        //        Position = entity.Transform.Position,
        //        Orientation = entity.Transform.Rotation
        //    },
        //    Activity = new BodyActivityDescription
        //    {
        //        SleepThreshold = 0.01f,
        //        //MinimumTimestepCountForSleep = 32
        //    }
        //};

        // var collidable = new CollidableDescription(shapeIndex, 0.1f);

        // var activityDescription = new BodyActivityDescription
        // {
        //     SleepThreshold = 0.01f, // or -1 if you really don't want it to sleep
        //     MinimumTimestepCountUnderThreshold = 32
        // };

        // var bodyDescription = BodyDescription.CreateDynamic(
        //    (RigidPose)pos,
        //    zeroInertia,
        //    collidable,
        //    activityDescription
        //);

        //entity.GetSimulation().Simulation.Bodies.Add(bodyDescription);
        // Apply the OneBodyAngularServo constraint to keep the box's orientation fixed.

        var ballSocket = new BallSocketConstraintComponent
        {
            A = body,
            //B = body,
            LocalOffsetA = Vector3.Zero,
            LocalOffsetB = Vector3.Zero,
            SpringDampingRatio = 1,
            SpringFrequency = 30
        };

        var oneBodyLinearServo = new OneBodyLinearServoConstraintComponent
        {
            A = body,
            Target = new Vector3(0, -10, 0),
            //B = body,
            ServoMaximumSpeed = 0.1f,
            ServoBaseSpeed = 1,
            ServoMaximumForce = 1,

            //LocalOffsetA = Vector3.Zero,
            //LocalOffsetB = Vector3.Zero,
            //LocalPlaneNormal = new Vector3(0, 1, 0),
        };

        //var rotation = entity.Transform.Rotation;

        var oneBodyAngularServo = new OneBodyAngularServoConstraintComponent
        {
            A = body,
            TargetOrientation = Quaternion.Identity,
            SpringDampingRatio = 1,
            SpringFrequency = 30,
        };

        var pointOnLine = new PointOnLineServoConstraintComponent
        {
            A = body,
            B = body,
            LocalOffsetA = Vector3.Zero,
            LocalOffsetB = Vector3.Zero,
            LocalDirection = Vector3.UnitY,
            SpringDampingRatio = 1,
            SpringFrequency = 30,
        };

        var linearAxisServe = new LinearAxisServoConstraintComponent
        {
            A = body,
            B = body,
            LocalOffsetA = Vector3.Zero,
            LocalOffsetB = Vector3.Zero,
            LocalPlaneNormal = Vector3.UnitX,
            SpringDampingRatio = 1,
            SpringFrequency = 30,
        };

        var linearAxisServe2 = new LinearAxisServoConstraintComponent
        {
            A = body,
            B = body,
            LocalOffsetA = Vector3.Zero,
            LocalOffsetB = Vector3.Zero,
            LocalPlaneNormal = Vector3.UnitZ,
            SpringDampingRatio = 1,
            SpringFrequency = 30,
        };

        var angularServo = new AngularServoConstraintComponent
        {
            A = body,
            B = body,
            TargetRelativeRotationLocalA = Quaternion.Identity,
        };

        var _oblscc = new OneBodyLinearServoConstraintComponent();
        _oblscc.ServoMaximumSpeed = float.MaxValue;
        _oblscc.ServoBaseSpeed = 0;
        _oblscc.ServoMaximumForce = 1000;
        _oblscc.A = body;
        _oblscc.Enabled = false;

        var _obascc = new OneBodyAngularServoConstraintComponent();
        _obascc.ServoMaximumSpeed = float.MaxValue;
        _obascc.ServoBaseSpeed = 0;
        _obascc.ServoMaximumForce = 1000;
        _obascc.A = body;
        _obascc.Enabled = false;

        //entity.Add(_oblscc);
        //entity.Add(_obascc);

        //entity.Add(oneBodyLinearServo);
        //entity.Add(angularServo);
        //entity.Add(linearAxisServe);
        //entity.Add(linearAxisServe2);
        //entity.Add(angularServo);



        //var angularServo = new AngularServoConstraintComponent
        //{
        //    A = body,
        //    B = body,
        //    TargetRelativeRotationLocalA = Quaternion.Identity,
        //};

        //entity.Add(angularServo);

        //body.Constraints = new RigidConstraints();
        //body.LinearVelocity = new Vector3(0, 1f, 0); // Set an initial velocity along the Y-axis
        //body.LinearFactor = new Vector3(0, 1, 0); // Restrict linear motion to the Y-axis
        //body.AngularFactor = Vector3.Zero; // Restrict angular rotation on all axes
    }

    private Entity CreateCube(Vector3 size)
    {
        var color = Constants.Colours[_random.Next(0, Constants.Colours.Count)];

        var entity = _game.Create3DPrimitive(PrimitiveModelType.Cube, new()
        {
            EntityName = "Cube",
            Material = _materials[color],
            IncludeCollider = false,
            Size = size
        });

        entity.Add(new CubeComponent(color));

        return entity;
    }

    public void AddAllDirectionLighting(float intensity, bool showLightGizmo = true)
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
            entity.Scene = _scene;

            if (showLightGizmo)
                entity.AddLightDirectionalGizmo(_game.GraphicsDevice);
        }
    }
}
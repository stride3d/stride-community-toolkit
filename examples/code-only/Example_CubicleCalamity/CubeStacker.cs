using CubicleCalamity.Components;
using CubicleCalamity.Scripts;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Extensions;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Physics;
using Stride.Rendering;
using Stride.Rendering.Colors;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace CubicleCalamity;

public class CubeStacker
{
    private class EntitySortInfo : IComparer<EntitySortInfo>
    {
        public Entity Entity;

        public float Depth;

        public int Compare(EntitySortInfo x, EntitySortInfo y)
        {
            return Math.Sign(x.Depth - y.Depth);
        }
    };

    private readonly Game _game;
    private readonly Dictionary<Color, Material> _materials = new();
    private readonly Random _random = new();
    private double _elapsedTime;
    private int _layer = 1;

    private static readonly Color RedUniformColor = new Color(0xFC, 0x37, 0x37);
    private static readonly Color GreenUniformColor = new Color(0x32, 0xE3, 0x35);
    private static readonly Color BlueUniformColor = new Color(0x2F, 0x6A, 0xE1);
    private readonly Material[] planeMaterials = new Material[3];

    private readonly List<EntitySortInfo> sortedEntities = new List<EntitySortInfo>();
    private readonly List<Entity>[] translationAxes = { new List<Entity>(), new List<Entity>(), new List<Entity>() };
    private readonly List<Entity>[] translationPlanes = { new List<Entity>(), new List<Entity>(), new List<Entity>() };
    private readonly List<Entity>[] translationPlaneEdges = { new List<Entity>(), new List<Entity>(), new List<Entity>() };

    private readonly List<Entity> translationPlaneRoots = new List<Entity>();
    private readonly List<ModelComponent>[] translationOpositeAxes = { new List<ModelComponent>(), new List<ModelComponent>(), new List<ModelComponent>(), };
    private Entity translationOrigin;

    private GraphicsDevice GraphicsDevice;
    public RenderGroup RenderGroup = RenderGroup.Group0;
    private const int GizmoTessellation = 64;
    private const float GizmoExtremitySize = 0.15f; // the size of the object placed at the extremity of the gizmo axes
    private const float GizmoOriginScale = 1.33f; // the scale of the object placed at the origin in comparison to the extremity object
    private const float GizmoPlaneLength = 0.25f; // the size of the gizmo small planes defining transformation along planes.
    private const float GizmoDefaultSize = 133f; // the default size of the gizmo on the screen in pixels.
    private const float AxisConeRadius = GizmoExtremitySize / 3f;
    private const float AxisConeHeight = GizmoExtremitySize;
    private const float AxisBodyRadius = GizmoExtremitySize / 9f;
    private const float AxisBodyLength = 1f - AxisConeHeight;
    private const float OriginRadius = GizmoOriginScale * AxisConeRadius;

    protected Material DefaultOriginMaterial;


    protected Material RedUniformMaterial { get; private set; }

    /// <summary>
    /// A uniform green material
    /// </summary>
    protected Material GreenUniformMaterial { get; private set; }

    /// <summary>
    /// A uniform blue material
    /// </summary>
    protected Material BlueUniformMaterial { get; private set; }

    public CubeStacker(Game game)
    {
        _game = game;
    }

    public void Start(Scene scene)
    {
        _game.SetupBase3DScene();
        _game.AddProfiler();

        GraphicsDevice = _game.GraphicsDevice;

        CreateMaterials();

        //SetupLighting(scene);

        //CreateFirstLayer(0.5f, scene);

        CreateGameManagerEntity(scene);

        Create(scene);
    }

    protected Entity? CreateBase()
    {
        DefaultOriginMaterial = CreateEmissiveColorMaterial(Color.White);
        RedUniformMaterial = CreateEmissiveColorMaterial(RedUniformColor);
        GreenUniformMaterial = CreateEmissiveColorMaterial(GreenUniformColor);
        BlueUniformMaterial = CreateEmissiveColorMaterial(BlueUniformColor);

        return null;
    }

    public Entity Create(Scene scene)
    {
        CreateBase();

        planeMaterials[0] = CreateUniformColorMaterial(Color.Red.WithAlpha(86));
        planeMaterials[1] = CreateUniformColorMaterial(Color.Green.WithAlpha(86));
        planeMaterials[2] = CreateUniformColorMaterial(Color.Blue.WithAlpha(86));

        var entity = new Entity("Translation gizmo");

        entity.Scene = scene;

        var axisRootEntities = new[] { new Entity("Root X axis"), new Entity("Root Y axis"), new Entity("Root Z axis") };
        var coneMesh = GeometricPrimitive.Cone.New(GraphicsDevice, AxisConeRadius, AxisConeHeight, GizmoTessellation).ToMeshDraw();
        var bodyMesh = GeometricPrimitive.Cylinder.New(GraphicsDevice, AxisBodyLength, AxisBodyRadius, GizmoTessellation).ToMeshDraw();

        // create the axis arrows
        for (int axis = 0; axis < 3; ++axis)
        {
            var material = GetAxisDefaultMaterial(axis);

            // the end cone
            var coneEntity = new Entity("ArrowCone" + axis) { new ModelComponent { Model = new Model { material, new Mesh { Draw = coneMesh } }, RenderGroup = RenderGroup } };
            coneEntity.Transform.Rotation = Quaternion.RotationZ(-MathUtil.Pi / 2);
            coneEntity.Transform.Position.X = AxisBodyLength + AxisConeHeight * 0.5f;
            translationAxes[axis].Add(coneEntity);

            // the main body
            var bodyEntity = new Entity("ArrowBody" + axis) { new ModelComponent { Model = new Model { material, new Mesh { Draw = bodyMesh } }, RenderGroup = RenderGroup } };
            bodyEntity.Transform.Position.X = AxisBodyLength / 2;
            bodyEntity.Transform.RotationEulerXYZ = -MathUtil.Pi / 2 * Vector3.UnitZ;
            translationAxes[axis].Add(bodyEntity);

            // oposite side part (cylinder shown when camera is looking oposite direction to the axis)
            var frameMesh = GeometricPrimitive.Cylinder.New(GraphicsDevice, GizmoPlaneLength, AxisBodyRadius, GizmoTessellation).ToMeshDraw();
            var opositeFrameEntity = new Entity("Oposite Frame" + axis) { new ModelComponent { Model = new Model { material, new Mesh { Draw = frameMesh } }, RenderGroup = RenderGroup } };
            opositeFrameEntity.Transform.Position.X = -GizmoPlaneLength / 2;
            opositeFrameEntity.Transform.RotationEulerXYZ = -MathUtil.Pi / 2 * Vector3.UnitZ;
            translationAxes[axis].Add(opositeFrameEntity);
            translationOpositeAxes[axis].Add(opositeFrameEntity.Get<ModelComponent>());

            // create the arrow entity composed of the cone and body
            var arrowEntity = new Entity("ArrowEntity" + axis);
            arrowEntity.Transform.Children.Add(coneEntity.Transform);
            arrowEntity.Transform.Children.Add(bodyEntity.Transform);
            arrowEntity.Transform.Children.Add(opositeFrameEntity.Transform);

            // Add the arrow entity to the gizmo entity
            axisRootEntities[axis].Transform.Children.Add(arrowEntity.Transform);
        }

        // create the translation planes
        for (int axis = 0; axis < 3; ++axis)
        {
            // The skeleton material
            var axisMaterial = GetAxisDefaultMaterial(axis);

            // The 2 frame rectangles
            var frameMesh = GeometricPrimitive.Cube.New(GraphicsDevice, new Vector3(AxisBodyRadius / 2f, GizmoPlaneLength / 3f, AxisBodyRadius / 2f)).ToMeshDraw();
            var topFrameEntity = new Entity("TopFrame" + axis) { new ModelComponent { Model = new Model { axisMaterial, new Mesh { Draw = frameMesh } }, RenderGroup = RenderGroup } };
            var leftFrameEntity = new Entity("LeftFrame" + axis) { new ModelComponent { Model = new Model { axisMaterial, new Mesh { Draw = frameMesh } }, RenderGroup = RenderGroup } };
            topFrameEntity.Transform.Position = new Vector3(0, GizmoPlaneLength, GizmoPlaneLength - (GizmoPlaneLength / 6));
            topFrameEntity.Transform.RotationEulerXYZ = new Vector3(MathUtil.Pi / 2f, 0, 0);
            leftFrameEntity.Transform.Position = new Vector3(0, GizmoPlaneLength - (GizmoPlaneLength / 6), GizmoPlaneLength);
            translationPlaneEdges[axis].Add(topFrameEntity);
            translationPlaneEdges[axis].Add(leftFrameEntity);

            // The transparent planes (2 for correct lighting)
            var materialPlane = planeMaterials[axis];
            var planeMesh = GeometricPrimitive.Plane.New(GraphicsDevice, GizmoPlaneLength, GizmoPlaneLength).ToMeshDraw();
            var planeFrameEntityFront = new Entity("FramePlaneFront" + axis) { new ModelComponent { Model = new Model { materialPlane, new Mesh { Draw = planeMesh } }, RenderGroup = RenderGroup } };
            var planeFrameEntityBack = new Entity("FramePlaneBack" + axis) { new ModelComponent { Model = new Model { materialPlane, new Mesh { Draw = planeMesh } }, RenderGroup = RenderGroup } };
            planeFrameEntityFront.Transform.Position = new Vector3(0, GizmoPlaneLength / 2, GizmoPlaneLength / 2);
            planeFrameEntityFront.Transform.RotationEulerXYZ = new Vector3(0, MathUtil.Pi / 2f, 0);
            planeFrameEntityBack.Transform.Position = new Vector3(0, GizmoPlaneLength / 2, GizmoPlaneLength / 2);
            planeFrameEntityBack.Transform.RotationEulerXYZ = new Vector3(0, -MathUtil.Pi / 2f, 0);
            translationPlanes[axis].Add(planeFrameEntityFront);
            translationPlanes[axis].Add(planeFrameEntityBack);
            sortedEntities.Add(new EntitySortInfo { Entity = planeFrameEntityFront });
            sortedEntities.Add(new EntitySortInfo { Entity = planeFrameEntityBack });

            // Add the different parts of the plane to the plane entity
            var planeEntity = new Entity("GizmoPlane" + axis);
            planeEntity.Transform.Children.Add(topFrameEntity.Transform);
            planeEntity.Transform.Children.Add(leftFrameEntity.Transform);
            planeEntity.Transform.Children.Add(planeFrameEntityFront.Transform);
            planeEntity.Transform.Children.Add(planeFrameEntityBack.Transform);
            translationPlaneRoots.Add(planeEntity);

            // Add the plane entity to the gizmo entity
            axisRootEntities[axis].Transform.Children.Add(planeEntity.Transform);
        }

        // set the axis root entities rotation and add them to the main entity
        var axisRotations = new[] { Vector3.Zero, new Vector3(MathUtil.PiOverTwo, 0, MathUtil.PiOverTwo), new Vector3(-MathUtil.PiOverTwo, -MathUtil.PiOverTwo, 0) };
        for (int axis = 0; axis < 3; axis++)
        {
            axisRootEntities[axis].Transform.RotationEulerXYZ = axisRotations[axis];
            entity.Transform.Children.Add(axisRootEntities[axis].Transform);
        }

        // Add middle sphere
        var sphereMeshDraw = GeometricPrimitive.Sphere.New(GraphicsDevice, OriginRadius, GizmoTessellation).ToMeshDraw();
        translationOrigin = new Entity("OriginSphere") { new ModelComponent { Model = new Model { DefaultOriginMaterial, new Mesh { Draw = sphereMeshDraw } }, RenderGroup = RenderGroup } };
        entity.Transform.Children.Add(translationOrigin.Transform);

        return entity;
    }

    protected Material CreateUniformColorMaterial(Color color)
    {
        return GizmoUniformColorMaterial.Create(GraphicsDevice, color);
    }

    protected Material GetAxisDefaultMaterial(int axisIndex)
    {
        switch (axisIndex)
        {
            case 0:
                return RedUniformMaterial;
            case 1:
                return GreenUniformMaterial;
            case 2:
                return BlueUniformMaterial;
            default:
                throw new ArgumentOutOfRangeException("axisIndex");
        }
    }

    /// <summary>
    /// Creates an emissive color material.
    /// </summary>
    /// <param name="color">The color of the material</param>
    /// <returns>the material</returns>
    protected Material CreateEmissiveColorMaterial(Color color)
    {
        return GizmoEmissiveColorMaterial.Create(GraphicsDevice, color, 0.75f);
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

public static class GizmoUniformColorMaterial
{
    public static Material Create(GraphicsDevice device, Color color)
    {
        var desc = new MaterialDescriptor();
        desc.Attributes.Diffuse = new MaterialDiffuseMapFeature(new ComputeColor());
        desc.Attributes.DiffuseModel = new MaterialDiffuseLambertModelFeature();

        var material = Material.New(device, desc);

        // set the color to the material
        UpdateColor(device, material, color);

        // set the transparency property to the material if necessary
        if (color.A < Byte.MaxValue)
        {

            material.Passes[0].HasTransparency = true;
            // TODO GRAPHICS REFACTOR
            //material.Parameters.SetResourceSlow(Graphics.Effect.BlendStateKey, device.BlendStates.NonPremultiplied);
        }

        return material;
    }

    public static void UpdateColor(GraphicsDevice device, Material material, Color color)
    {
        // set the color to the material
        material.Passes[0].Parameters.Set(MaterialKeys.DiffuseValue, new Color4(color).ToColorSpace(device.ColorSpace));
    }
}

public static class GizmoEmissiveColorMaterial
{
    public static Material Create(GraphicsDevice device, Color color, float intensity = 1f)
    {
        var material = Material.New(device, new MaterialDescriptor
        {
            Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor()),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Emissive = new MaterialEmissiveMapFeature(new ComputeColor())
                }
        });

        // set the color to the material
        UpdateColor(device, material, color, intensity);

        // set the transparency property to the material if necessary
        if (color.A < byte.MaxValue)
        {

            material.Passes[0].HasTransparency = true;
        }

        return material;
    }

    public static void UpdateColor(GraphicsDevice device, Material material, Color color, float intensity = 1f)
    {
        // set the color to the material
        material.Passes[0].Parameters.Set(MaterialKeys.DiffuseValue, new Color4(color).ToColorSpace(device.ColorSpace));

        material.Passes[0].Parameters.Set(MaterialKeys.EmissiveIntensity, intensity);
        material.Passes[0].Parameters.Set(MaterialKeys.EmissiveValue, new Color4(color).ToColorSpace(device.ColorSpace));
    }
}
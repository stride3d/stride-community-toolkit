using Stride.Engine;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Stride.CommunityToolkit.Rendering.Gizmos;

public class TranslationGizmo : AxialGizmo
{
    private const float AxisConeRadius = GizmoExtremitySize / 3f;
    private const float AxisConeHeight = GizmoExtremitySize;
    private const float AxisBodyRadius = GizmoExtremitySize / 9f;
    private const float AxisBodyLength = 1f - AxisConeHeight;
    private const float OriginRadius = GizmoOriginScale * AxisConeRadius;

    private readonly Material[] _planeMaterials = new Material[3];
    private readonly List<Entity>[] _translationAxes = { new(), new(), new() };
    private readonly List<Entity>[] _translationPlanes = { new(), new(), new() };
    private readonly List<Entity>[] _translationPlaneEdges = { new(), new(), new() };
    private readonly List<Entity> _translationPlaneRoots = new();
    private readonly List<ModelComponent>[] _translationOppositeAxes = { new(), new(), new(), };

    public TranslationGizmo(GraphicsDevice graphicsDevice) : base(graphicsDevice) { }

    public TranslationGizmo(GraphicsDevice graphicsDevice, Color? xColor = null, Color? yColor = null, Color? zColor = null) : base(graphicsDevice, xColor, yColor, zColor) { }

    public void Create(Entity entity, bool showAxisName = false, bool rotateAxisNames = true)
    {
        base.Create();

        _planeMaterials[0] = CreateUniformColorMaterial(GetRedColor().WithAlpha(86));
        _planeMaterials[1] = CreateUniformColorMaterial(GetGreenColor().WithAlpha(86));
        _planeMaterials[2] = CreateUniformColorMaterial(GetBlueColor().WithAlpha(86));

        var axisRootEntities = new[] { new Entity("Root X axis"), new Entity("Root Y axis"), new Entity("Root Z axis") };

        CreateAxisArrow(axisRootEntities);

        CreateTranslationPlanes(axisRootEntities);

        // set the axis root entities rotation and add them to the main entity
        var axisRotations = new[] { Vector3.Zero, new Vector3(MathUtil.PiOverTwo, 0, MathUtil.PiOverTwo), new Vector3(-MathUtil.PiOverTwo, -MathUtil.PiOverTwo, 0) };

        for (int axis = 0; axis < 3; axis++)
        {
            axisRootEntities[axis].Transform.RotationEulerXYZ = axisRotations[axis];

            entity.Transform.Children.Add(axisRootEntities[axis].Transform);
        }

        var translationOrigin = CreateMiddleSphere();

        AddAxisNames(entity, showAxisName, rotateAxisNames);

        entity.Transform.Children.Add(translationOrigin.Transform);
    }

    private void AddAxisNames(Entity entity, bool showAxisName, bool rotateAxisNames)
    {
        if (showAxisName is false) return;

        var letter = new Letter3D(GraphicsDevice, rotateAxisNames);

        var xLetter = letter.CreateLetterX();
        xLetter.Transform.Position.X = 1.1f;
        xLetter.Transform.Position.Y = 0.15f;

        var yLetter = letter.CreateLetterY();
        yLetter.Transform.Position.Y = 1.15f;

        var zLetter = letter.CreateLetterZ();
        zLetter.Transform.Position.Z = 1.1f;
        zLetter.Transform.Position.Y = 0.15f;

        entity.AddChild(xLetter);
        entity.AddChild(yLetter);
        entity.AddChild(zLetter);
    }

    public Entity Create(Scene scene)
    {
        var entity = new Entity("Translation gizmo")
        {
            Scene = scene
        };

        Create(entity);

        return entity;
    }

    private Entity CreateMiddleSphere()
    {
        var sphereMeshDraw = GeometricPrimitive.Sphere.New(GraphicsDevice, OriginRadius, GizmoTessellation).ToMeshDraw();

        var translationOrigin = new Entity("OriginSphere")
        {
            new ModelComponent { Model = new Model { DefaultOriginMaterial, new Mesh { Draw = sphereMeshDraw } }, RenderGroup = RenderGroup }
        };

        return translationOrigin;
    }

    private void CreateAxisArrow(Entity[] axisRootEntities)
    {
        var coneMesh = GeometricPrimitive.Cone.New(GraphicsDevice, AxisConeRadius, AxisConeHeight, GizmoTessellation).ToMeshDraw();
        var bodyMesh = GeometricPrimitive.Cylinder.New(GraphicsDevice, AxisBodyLength, AxisBodyRadius, GizmoTessellation).ToMeshDraw();

        for (int axis = 0; axis < 3; ++axis)
        {
            var material = GetAxisDefaultMaterial(axis);

            // the end cone
            var coneEntity = new Entity("ArrowCone" + axis) { new ModelComponent { Model = new Model { material, new Mesh { Draw = coneMesh } }, RenderGroup = RenderGroup } };
            coneEntity.Transform.Rotation = Quaternion.RotationZ(-MathUtil.Pi / 2);
            coneEntity.Transform.Position.X = AxisBodyLength + AxisConeHeight * 0.5f;
            _translationAxes[axis].Add(coneEntity);
            //coneEntity.AddChild(CreateLetterX());

            // the main body
            var bodyEntity = new Entity("ArrowBody" + axis) { new ModelComponent { Model = new Model { material, new Mesh { Draw = bodyMesh } }, RenderGroup = RenderGroup } };
            bodyEntity.Transform.Position.X = AxisBodyLength / 2;
            bodyEntity.Transform.RotationEulerXYZ = -MathUtil.Pi / 2 * Vector3.UnitZ;
            _translationAxes[axis].Add(bodyEntity);

            // oposite side part (cylinder shown when camera is looking oposite direction to the axis)
            var frameMesh = GeometricPrimitive.Cylinder.New(GraphicsDevice, GizmoPlaneLength, AxisBodyRadius, GizmoTessellation).ToMeshDraw();
            var opositeFrameEntity = new Entity("Oposite Frame" + axis) { new ModelComponent { Model = new Model { material, new Mesh { Draw = frameMesh } }, RenderGroup = RenderGroup } };
            opositeFrameEntity.Transform.Position.X = -GizmoPlaneLength / 2;
            opositeFrameEntity.Transform.RotationEulerXYZ = -MathUtil.Pi / 2 * Vector3.UnitZ;
            _translationAxes[axis].Add(opositeFrameEntity);
            _translationOppositeAxes[axis].Add(opositeFrameEntity.Get<ModelComponent>());

            // create the arrow entity composed of the cone and body
            var arrowEntity = new Entity("ArrowEntity" + axis);
            arrowEntity.Transform.Children.Add(coneEntity.Transform);
            arrowEntity.Transform.Children.Add(bodyEntity.Transform);
            arrowEntity.Transform.Children.Add(opositeFrameEntity.Transform);


            // Add the arrow entity to the gizmo entity
            axisRootEntities[axis].Transform.Children.Add(arrowEntity.Transform);
        }
    }

    private void CreateTranslationPlanes(Entity[] axisRootEntities)
    {
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
            _translationPlaneEdges[axis].Add(topFrameEntity);
            _translationPlaneEdges[axis].Add(leftFrameEntity);

            // The transparent planes (2 for correct lighting)
            var materialPlane = _planeMaterials[axis];
            var planeMesh = GeometricPrimitive.Plane.New(GraphicsDevice, GizmoPlaneLength, GizmoPlaneLength).ToMeshDraw();
            var planeFrameEntityFront = new Entity("FramePlaneFront" + axis) { new ModelComponent { Model = new Model { materialPlane, new Mesh { Draw = planeMesh } }, RenderGroup = RenderGroup } };
            var planeFrameEntityBack = new Entity("FramePlaneBack" + axis) { new ModelComponent { Model = new Model { materialPlane, new Mesh { Draw = planeMesh } }, RenderGroup = RenderGroup } };
            planeFrameEntityFront.Transform.Position = new Vector3(0, GizmoPlaneLength / 2, GizmoPlaneLength / 2);
            planeFrameEntityFront.Transform.RotationEulerXYZ = new Vector3(0, MathUtil.Pi / 2f, 0);
            planeFrameEntityBack.Transform.Position = new Vector3(0, GizmoPlaneLength / 2, GizmoPlaneLength / 2);
            planeFrameEntityBack.Transform.RotationEulerXYZ = new Vector3(0, -MathUtil.Pi / 2f, 0);
            _translationPlanes[axis].Add(planeFrameEntityFront);
            _translationPlanes[axis].Add(planeFrameEntityBack);

            // Add the different parts of the plane to the plane entity
            var planeEntity = new Entity("GizmoPlane" + axis);
            planeEntity.Transform.Children.Add(topFrameEntity.Transform);
            planeEntity.Transform.Children.Add(leftFrameEntity.Transform);
            planeEntity.Transform.Children.Add(planeFrameEntityFront.Transform);
            planeEntity.Transform.Children.Add(planeFrameEntityBack.Transform);
            _translationPlaneRoots.Add(planeEntity);

            // Add the plane entity to the gizmo entity
            axisRootEntities[axis].Transform.Children.Add(planeEntity.Transform);
        }
    }

    protected Material CreateUniformColorMaterial(Color color)
        => GizmoUniformColorMaterial.Create(GraphicsDevice, color);

    protected Material? GetAxisDefaultMaterial(int axisIndex) => axisIndex switch
    {
        0 => RedUniformMaterial,
        1 => GreenUniformMaterial,
        2 => BlueUniformMaterial,
        _ => throw new ArgumentOutOfRangeException(nameof(axisIndex)),
    };
}
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Engine;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Stride.CommunityToolkit.Rendering.Gizmos;

/// <summary>
/// Represents a visual translation gizmo used for moving objects in a 3D scene: three axis arrows in the editor's
/// red, green and blue, three translucent planes between them and a sphere at the origin.
/// </summary>
/// <remarks>
/// This class creates a visual aid for object manipulation, consisting of axis arrows and planes,
/// which can be used to indicate or manipulate movement along specific axes or planes.
/// It is designed as an extension and can be added to any entity in a scene for visual feedback.
/// </remarks>
public class TranslationGizmo
{
    private const float AxisConeRadius = GizmoSizes.ExtremitySize / 3f;
    private const float AxisConeHeight = GizmoSizes.ExtremitySize;
    private const float AxisBodyRadius = GizmoSizes.ExtremitySize / 9f;
    private const float AxisBodyLength = 1f - AxisConeHeight;

    private readonly Material[] _planeMaterials = new Material[3];

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationGizmo"/> class with the specified graphics device and optional colors for each axis.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to render the gizmo.</param>
    /// <param name="xColor">Optional color for the X-axis. If null, the editor's red is used.</param>
    /// <param name="yColor">Optional color for the Y-axis. If null, the editor's green is used.</param>
    /// <param name="zColor">Optional color for the Z-axis. If null, the editor's blue is used.</param>
    /// <param name="renderGroup">The render group every part of the gizmo is drawn in. Defaults to <see cref="RenderGroup.Group0"/>.</param>
    /// <exception cref="ArgumentNullException">The graphics device is null.</exception>
    public TranslationGizmo(GraphicsDevice graphicsDevice, Color? xColor = null, Color? yColor = null, Color? zColor = null, RenderGroup renderGroup = RenderGroup.Group0)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        GraphicsDevice = graphicsDevice;
        RenderGroup = renderGroup;
        RedColor = xColor ?? GizmoColors.X;
        GreenColor = yColor ?? GizmoColors.Y;
        BlueColor = zColor ?? GizmoColors.Z;

        OriginMaterial = CreateEmissiveColorMaterial(Color.White);
        XMaterial = CreateEmissiveColorMaterial(RedColor);
        YMaterial = CreateEmissiveColorMaterial(GreenColor);
        ZMaterial = CreateEmissiveColorMaterial(BlueColor);
    }

    /// <summary>
    /// Gets the render group every part of the gizmo is drawn in.
    /// </summary>
    public RenderGroup RenderGroup { get; }

    /// <summary>
    /// Gets the graphics device used for rendering the gizmo.
    /// </summary>
    protected GraphicsDevice GraphicsDevice { get; }

    /// <summary>The colour of the X axis: the one given to the constructor, or the editor's red.</summary>
    protected Color RedColor { get; }

    /// <summary>The colour of the Y axis: the one given to the constructor, or the editor's green.</summary>
    protected Color GreenColor { get; }

    /// <summary>The colour of the Z axis: the one given to the constructor, or the editor's blue.</summary>
    protected Color BlueColor { get; }

    /// <summary>Gets the material of the sphere at the origin, white.</summary>
    protected Material OriginMaterial { get; }

    /// <summary>Gets the material of the X axis, emissive in <see cref="RedColor"/>.</summary>
    protected Material XMaterial { get; }

    /// <summary>Gets the material of the Y axis, emissive in <see cref="GreenColor"/>.</summary>
    protected Material YMaterial { get; }

    /// <summary>Gets the material of the Z axis, emissive in <see cref="BlueColor"/>.</summary>
    protected Material ZMaterial { get; }

    /// <summary>
    /// Creates the translation gizmo entities and adds them to the specified parent entity.
    /// </summary>
    /// <param name="entity">The entity to which the gizmo will be added.</param>
    /// <param name="showAxisName">Determines whether the axis names should be displayed.</param>
    /// <param name="rotateAxisNames">Determines whether the axis names should be rotated to match the axes' orientation.</param>
    public void Create(Entity entity, bool showAxisName = false, bool rotateAxisNames = true)
    {
        _planeMaterials[0] = CreateUniformColorMaterial(RedColor.WithAlpha(86));
        _planeMaterials[1] = CreateUniformColorMaterial(GreenColor.WithAlpha(86));
        _planeMaterials[2] = CreateUniformColorMaterial(BlueColor.WithAlpha(86));

        var axisRootEntities = new[] { new Entity("Root X axis"), new Entity("Root Y axis"), new Entity("Root Z axis") };

        CreateAxisArrow(axisRootEntities);
        CreateTranslationPlanes(axisRootEntities);

        // Each root is built along +X, then turned onto its own axis
        var axisRotations = new[] { Vector3.Zero, new Vector3(MathUtil.PiOverTwo, 0, MathUtil.PiOverTwo), new Vector3(-MathUtil.PiOverTwo, -MathUtil.PiOverTwo, 0) };

        for (var axis = 0; axis < 3; axis++)
        {
            axisRootEntities[axis].Transform.RotationEulerXYZ = axisRotations[axis];

            entity.AddChild(axisRootEntities[axis]);
        }

        AddAxisNames(entity, showAxisName, rotateAxisNames);

        entity.AddChild(Part("OriginSphere", OriginMaterial, GeometricPrimitive.Sphere.New(GraphicsDevice, GizmoSizes.OriginRadius, GizmoSizes.Tessellation).ToMeshDraw()));
    }

    /// <summary>
    /// Adds axis names (X, Y, Z) to the translation gizmo.
    /// </summary>
    /// <param name="entity">The entity to which the axis names will be added.</param>
    /// <param name="showAxisName">Determines whether the axis names should be displayed.</param>
    /// <param name="rotateAxisNames">Determines whether the axis names should rotate with the axes.</param>
    private void AddAxisNames(Entity entity, bool showAxisName, bool rotateAxisNames)
    {
        if (!showAxisName) return;

        entity.AddChild(CreateAxisLabel("X", new Vector3(1.1f, 0.15f, 0f), RedColor, rotateAxisNames));
        entity.AddChild(CreateAxisLabel("Y", new Vector3(0f, 1.15f, 0f), GreenColor, rotateAxisNames));
        entity.AddChild(CreateAxisLabel("Z", new Vector3(0f, 0.15f, 1.1f), BlueColor, rotateAxisNames));
    }

    /// <summary>
    /// Creates one axis label as world-space text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="WorldTextProcessor"/> adds the <see cref="WorldTextRenderer"/> the label needs.
    /// </para>
    /// </remarks>
    private static Entity CreateAxisLabel(string axisName, Vector3 position, Color color, bool billboard)
    {
        var label = new Entity($"Letter{axisName}")
        {
            new WorldTextComponent
            {
                Text = axisName,
                Height = 0.3f,
                TextColor = color,
                Billboard = billboard,
                Anchor = Anchor.MiddleCenter,
            }
        };

        label.Transform.Position = position;

        return label;
    }

    /// <summary>
    /// Creates and returns a new translation gizmo entity, adding it to the specified scene.
    /// </summary>
    /// <param name="scene">The scene to which the gizmo will be added.</param>
    /// <returns>The created translation gizmo entity.</returns>
    public Entity Create(Scene scene)
    {
        var entity = new Entity("Translation gizmo")
        {
            Scene = scene
        };

        Create(entity);

        return entity;
    }

    /// <summary>
    /// Creates arrow-shaped entities for the gizmo axes and adds them to the root entities for each axis.
    /// </summary>
    /// <param name="axisRootEntities">The root entities for each axis to which the arrows will be added.</param>
    private void CreateAxisArrow(Entity[] axisRootEntities)
    {
        var coneMesh = GeometricPrimitive.Cone.New(GraphicsDevice, AxisConeRadius, AxisConeHeight, GizmoSizes.Tessellation).ToMeshDraw();
        var bodyMesh = GeometricPrimitive.Cylinder.New(GraphicsDevice, AxisBodyLength, AxisBodyRadius, GizmoSizes.Tessellation).ToMeshDraw();
        var oppositeMesh = GeometricPrimitive.Cylinder.New(GraphicsDevice, GizmoSizes.PlaneLength, AxisBodyRadius, GizmoSizes.Tessellation).ToMeshDraw();

        for (var axis = 0; axis < 3; axis++)
        {
            var material = GetAxisDefaultMaterial(axis);

            // The cone at the end of the arrow
            var cone = Part("ArrowCone" + axis, material, coneMesh);

            cone.Transform.Rotation = Quaternion.RotationZ(-MathUtil.PiOverTwo);
            cone.Transform.Position.X = AxisBodyLength + AxisConeHeight * 0.5f;

            // The shaft
            var body = Part("ArrowBody" + axis, material, bodyMesh);

            body.Transform.Position.X = AxisBodyLength / 2;
            body.Transform.RotationEulerXYZ = -MathUtil.PiOverTwo * Vector3.UnitZ;

            // A short stub on the far side, seen when the camera looks against the axis
            var opposite = Part("Opposite Frame" + axis, material, oppositeMesh);

            opposite.Transform.Position.X = -GizmoSizes.PlaneLength / 2;
            opposite.Transform.RotationEulerXYZ = -MathUtil.PiOverTwo * Vector3.UnitZ;

            var arrow = new Entity("ArrowEntity" + axis);

            arrow.AddChild(cone);
            arrow.AddChild(body);
            arrow.AddChild(opposite);

            axisRootEntities[axis].AddChild(arrow);
        }
    }

    /// <summary>
    /// Creates the translation planes for each axis, allowing movement along planes.
    /// </summary>
    /// <param name="axisRootEntities">The root entities for each axis to which the translation planes will be added.</param>
    private void CreateTranslationPlanes(Entity[] axisRootEntities)
    {
        const float planeLength = GizmoSizes.PlaneLength;

        var frameMesh = GeometricPrimitive.Cube.New(GraphicsDevice, new Vector3(AxisBodyRadius / 2f, planeLength / 3f, AxisBodyRadius / 2f)).ToMeshDraw();
        var planeMesh = GeometricPrimitive.Plane.New(GraphicsDevice, planeLength, planeLength).ToMeshDraw();

        for (var axis = 0; axis < 3; axis++)
        {
            var axisMaterial = GetAxisDefaultMaterial(axis);

            // The two edges of the frame, in the axis colour
            var topFrame = Part("TopFrame" + axis, axisMaterial, frameMesh);
            var leftFrame = Part("LeftFrame" + axis, axisMaterial, frameMesh);

            topFrame.Transform.Position = new Vector3(0, planeLength, planeLength - planeLength / 6);
            topFrame.Transform.RotationEulerXYZ = new Vector3(MathUtil.PiOverTwo, 0, 0);
            leftFrame.Transform.Position = new Vector3(0, planeLength - planeLength / 6, planeLength);

            // The translucent plane, drawn from both sides so it is lit from both
            var planeFront = Part("FramePlaneFront" + axis, _planeMaterials[axis], planeMesh);
            var planeBack = Part("FramePlaneBack" + axis, _planeMaterials[axis], planeMesh);

            planeFront.Transform.Position = new Vector3(0, planeLength / 2, planeLength / 2);
            planeFront.Transform.RotationEulerXYZ = new Vector3(0, MathUtil.PiOverTwo, 0);
            planeBack.Transform.Position = new Vector3(0, planeLength / 2, planeLength / 2);
            planeBack.Transform.RotationEulerXYZ = new Vector3(0, -MathUtil.PiOverTwo, 0);

            var plane = new Entity("GizmoPlane" + axis);

            plane.AddChild(topFrame);
            plane.AddChild(leftFrame);
            plane.AddChild(planeFront);
            plane.AddChild(planeBack);

            axisRootEntities[axis].AddChild(plane);
        }
    }

    /// <summary>An entity with one mesh in one material, in the gizmo's render group.</summary>
    private Entity Part(string name, Material material, MeshDraw mesh)
        => new(name) { new ModelComponent { Model = new Model { material, new Mesh { Draw = mesh } }, RenderGroup = RenderGroup } };

    /// <summary>
    /// Creates a material with an emissive color to visually represent the gizmo's axes.
    /// </summary>
    /// <param name="color">The color of the material.</param>
    /// <returns>A <see cref="Material"/> instance with the specified emissive color.</returns>
    protected Material CreateEmissiveColorMaterial(Color color)
        => GizmoEmissiveColorMaterial.Create(GraphicsDevice, color, 0.75f);

    /// <summary>
    /// Creates a uniform color material for the gizmo.
    /// </summary>
    /// <param name="color">The color of the material.</param>
    /// <returns>The created material.</returns>
    protected Material CreateUniformColorMaterial(Color color)
        => GizmoUniformColorMaterial.Create(GraphicsDevice, color);

    /// <summary>
    /// Retrieves the default material for the specified axis.
    /// </summary>
    /// <param name="axisIndex">The index of the axis (0 for X, 1 for Y, 2 for Z).</param>
    /// <returns>The material for the specified axis.</returns>
    protected Material GetAxisDefaultMaterial(int axisIndex) => axisIndex switch
    {
        0 => XMaterial,
        1 => YMaterial,
        2 => ZMaterial,
        _ => throw new ArgumentOutOfRangeException(nameof(axisIndex)),
    };
}
using Stride.Engine;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Stride.CommunityToolkit.Rendering.Gizmos;

/// <summary>
/// Represents a directional light gizmo used to visually represent light direction in a 3D scene.
/// </summary>
public class LightDirectionalGizmo
{
    /// <summary>
    /// The tessellation value for the gizmo, which defines the level of detail for its geometry.
    /// </summary>
    protected const int GizmoTessellation = 64;

    private const float BodyLength = 0.4f;
    private const float ConeHeight = BodyLength / 5;
    private const float BodyRadius = ConeHeight / 6;
    private const float ConeRadius = ConeHeight / 3;
    private const float OriginRadius = 1.33f * 0.15f / 3f;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly RenderGroup _renderGroup = RenderGroup.Group0;
    private readonly Material _rayMaterial;

    /// <summary>
    /// Initializes a new instance of the <see cref="LightDirectionalGizmo"/> class with the specified graphics device and optional color.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to render the gizmo.</param>
    /// <param name="color">An optional color to apply to the gizmo. If null, white is used.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="graphicsDevice"/> is null.</exception>
    public LightDirectionalGizmo(GraphicsDevice graphicsDevice, Color? color = null)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _rayMaterial = GizmoEmissiveColorMaterial.Create(_graphicsDevice, color ?? Color.White, 1f);
    }

    /// <summary>
    /// Creates the directional light gizmo and attaches it to the specified root entity.
    /// </summary>
    /// <param name="root">The root entity to which the light gizmo is attached.</param>
    /// <returns>The root entity with the light gizmo added.</returns>
    public Entity Create(Entity root)
    {
        var lightRay = new Entity($"Light ray for light gizmo {root.Id}");

        lightRay.AddChild(CreateRayCone(root.Id));
        lightRay.AddChild(CreateRayBody(root.Id));
        lightRay.AddChild(CreateMiddleSphere());

        root.AddChild(lightRay);

        return root;
    }

    /// <summary>
    /// Creates the cone part of the light ray for the directional light gizmo.
    /// </summary>
    /// <param name="id">The unique identifier for the entity to associate with the cone.</param>
    /// <returns>An entity representing the cone part of the light ray.</returns>
    private Entity CreateRayCone(Guid id)
    {
        var coneMesh = GeometricPrimitive.Cone.New(_graphicsDevice, ConeRadius, ConeHeight, GizmoTessellation).ToMeshDraw();
        var coneEntity = new Entity($"Light ray cone for light gizmo {id}")         {
            CreateModelComponent(coneMesh)
        };

        coneEntity.Transform.Rotation = Quaternion.RotationX(-MathUtil.PiOverTwo);
        coneEntity.Transform.Position.Z = -BodyLength - ConeHeight * 0.5f;

        return coneEntity;
    }

    /// <summary>
    /// Creates the cylindrical body part of the light ray for the directional light gizmo.
    /// </summary>
    /// <param name="id">The unique identifier for the entity to associate with the body.</param>
    /// <returns>An entity representing the body part of the light ray.</returns>
    private Entity CreateRayBody(Guid id)
    {
        var bodyMesh = GeometricPrimitive.Cylinder.New(_graphicsDevice, BodyLength, BodyRadius, GizmoTessellation).ToMeshDraw();
        var bodyEntity = new Entity($"Light ray body for light gizmo {id}")
        {
            CreateModelComponent(bodyMesh)
        };

        bodyEntity.Transform.Position.Z = -BodyLength / 2;
        bodyEntity.Transform.Rotation = Quaternion.RotationX(-MathUtil.PiOverTwo);

        return bodyEntity;
    }

    /// <summary>
    /// Creates the spherical part of the light gizmo that appears at its origin.
    /// </summary>
    /// <returns>An entity representing the spherical part of the light gizmo at the origin.</returns>
    private Entity CreateMiddleSphere()
    {
        var sphereMeshDraw = GeometricPrimitive.Sphere.New(_graphicsDevice, OriginRadius, GizmoTessellation).ToMeshDraw();

        return new Entity("OriginSphere")
        {
             CreateModelComponent(sphereMeshDraw)
        };
    }

    /// <summary>
    /// Creates a model component with the given mesh to represent part of the gizmo.
    /// </summary>
    /// <param name="bodyMesh">The mesh to use for the model component.</param>
    /// <returns>A <see cref="ModelComponent"/> with the provided mesh and the ray material applied.</returns>
    private ModelComponent CreateModelComponent(MeshDraw bodyMesh)
        => new()
        {
            Model = new Model { _rayMaterial, new Mesh { Draw = bodyMesh } },
            RenderGroup = _renderGroup
        };
}
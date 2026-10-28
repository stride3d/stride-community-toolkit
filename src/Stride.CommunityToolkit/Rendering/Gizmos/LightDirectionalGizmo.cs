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
    private const int GizmoTessellation = 64;

    private const float BodyLength = 0.4f;
    private const float ConeHeight = BodyLength / 5;
    private const float BodyRadius = ConeHeight / 6;
    private const float ConeRadius = ConeHeight / 3;
    private const float OriginRadius = 1.33f * 0.15f / 3f;

    private readonly GraphicsDevice _graphicsDevice;
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
        // The ray runs down -Z: the cone at its far end, the shaft behind it, a sphere at the origin
        var lightRay = new Entity($"Light ray for light gizmo {root.Id}");

        lightRay.AddChild(Part($"Light ray cone for light gizmo {root.Id}", GeometricPrimitive.Cone.New(_graphicsDevice, ConeRadius, ConeHeight, GizmoTessellation), -BodyLength - ConeHeight * 0.5f));
        lightRay.AddChild(Part($"Light ray body for light gizmo {root.Id}", GeometricPrimitive.Cylinder.New(_graphicsDevice, BodyLength, BodyRadius, GizmoTessellation), -BodyLength / 2));
        lightRay.AddChild(Part("OriginSphere", GeometricPrimitive.Sphere.New(_graphicsDevice, OriginRadius, GizmoTessellation), 0f, alongRay: false));

        root.AddChild(lightRay);

        return root;
    }

    /// <summary>
    /// One part of the ray: the primitive's mesh in the ray material, at <paramref name="z"/> along the ray,
    /// turned from the primitive's own Y axis onto -Z when it is a part that has a direction.
    /// </summary>
    private Entity Part(string name, GeometricPrimitive primitive, float z, bool alongRay = true)
    {
        var part = new Entity(name)
        {
            new ModelComponent
            {
                Model = new Model { _rayMaterial, new Mesh { Draw = primitive.ToMeshDraw() } },
                RenderGroup = RenderGroup.Group0
            }
        };

        part.Transform.Position.Z = z;

        if (alongRay) part.Transform.Rotation = Quaternion.RotationX(-MathUtil.PiOverTwo);

        return part;
    }
}
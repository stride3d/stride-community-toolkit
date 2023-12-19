using Stride.Engine;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Stride.CommunityToolkit.Rendering.Gizmos;

public class LightDirectionalGizmo
{
    protected const int GizmoTessellation = 64;
    private const float BodyLength = 0.4f;
    private const float ConeHeight = BodyLength / 5;
    private const float BodyRadius = ConeHeight / 6;
    private const float ConeRadius = ConeHeight / 3;
    private const float OriginRadius = 1.33f * 0.15f / 3f;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly RenderGroup _renderGroup = RenderGroup.Group0;
    private readonly Material _rayMaterial;

    public LightDirectionalGizmo(GraphicsDevice graphicsDevice, Color? color = null)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _rayMaterial = GizmoEmissiveColorMaterial.Create(_graphicsDevice, color ?? Color.White, 1f);
    }

    public Entity Create(Entity root)
    {
        var lightRay = new Entity($"Light ray for light gizmo {root.Id}");

        lightRay.AddChild(CreateRayCone(root.Id));
        lightRay.AddChild(CreateRayBody(root.Id));
        lightRay.AddChild(CreateMiddleSphere());

        root.AddChild(lightRay);

        return root;
    }

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

    private Entity CreateMiddleSphere()
    {
        var sphereMeshDraw = GeometricPrimitive.Sphere.New(_graphicsDevice, OriginRadius, GizmoTessellation).ToMeshDraw();

        return new Entity("OriginSphere")
        {
             CreateModelComponent(sphereMeshDraw)
        };
    }

    private ModelComponent CreateModelComponent(MeshDraw bodyMesh)
        => new()
        {
            Model = new Model { _rayMaterial, new Mesh { Draw = bodyMesh } },
            RenderGroup = _renderGroup
        };
}
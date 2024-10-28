using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Games;
using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Bepu.Engine;
public static class BepuGameExtensions
{
    private const string DefaultGroundName = "Ground";
    private const string GraphicsCompositorNotSet = "GraphicsCompositor is not set.";

    private static readonly Vector2 _default3DGroundSize = new(15f);
    private static readonly Vector3 _default2DGroundSize = new(15, 0.1f, 0);
    private static readonly Color _defaultMaterialColor = Color.FromBgra(0xFF8C8C8C);
    private static readonly Color _defaultGroundMaterialColor = Color.FromBgra(0xFF242424);

    public static void SetupBase2DSceneWithBepu(this Game game)
    {
        game.AddGraphicsCompositor().AddCleanUIStage();
        game.Add2DCamera().Add2DCameraController();
        game.Add3DGroundWithBepu();
    }

    public static void SetupBase3DSceneWithBepu(this Game game)
    {
        game.AddGraphicsCompositor().AddCleanUIStage();
        game.Add3DCamera().Add3DCameraController();
        game.AddDirectionalLight();
        game.Add3DGroundWithBepu();
    }
    public static Entity Add3DGroundWithBepu(this Game game, string? entityName = DefaultGroundName, Vector2? size = null, bool includeCollider = true)
        => CreateGroundWithBepu(game, entityName, size, includeCollider, PrimitiveModelType.Plane);


    private static Entity CreateGroundWithBepu(Game game, string? entityName, Vector2? size, bool includeCollider, PrimitiveModelType type)
    {
        var validSize = size ?? _default3DGroundSize;

        var material = game.CreateMaterial(_defaultGroundMaterialColor, 0.0f, 0.1f);

        var entity = game.Create3DPrimitiveWithBepu(type, new Primitive3DCreationOptionsWithBepu()
        {
            EntityName = entityName,
            Material = material,
            IncludeCollider = includeCollider,
            Size = (Vector3)validSize,
            Component = new StaticComponent() { Collider = new CompoundCollider() }
        });

        entity.Scene = game.SceneSystem.SceneInstance.RootScene;

        return entity;
    }

    public static Entity Create2DPrimitiveWithBepu(this IGame game, Primitive2DModelType type, Primitive2DCreationOptionsWithBepu? options = null)
    {
        options ??= new();

        var modelBase = Procedural2DModelBuilder.Build(type, options.Size, options.Depth);

        var model = modelBase.Generate(game.Services);

        if (options.Material != null)
        {
            model.Materials.Add(options.Material);
        }

        var entity = new Entity(options.EntityName);

        if (type == Primitive2DModelType.Circle)
        {
            var childEntity = new Entity("Child") { new ModelComponent(model) { RenderGroup = options.RenderGroup } };
            childEntity.Transform.Rotation = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90));
            entity.AddChild(childEntity);
        }
        else
        {
            entity.Add(new ModelComponent(model) { RenderGroup = options.RenderGroup });
        }

        if (!options.IncludeCollider) return entity;

        if (type == Primitive2DModelType.Triangle)
        {
            // This is needed when using ConvexHullCollider
            //var meshData = TriangularPrismProceduralModel.New(options.Size is null ? new(1, 1, options.Depth) : new(options.Size.Value.X, options.Size.Value.Y, options.Depth));

            //var points = meshData.Vertices.Select(w => w.Position).ToList();
            //var uintIndices = meshData.Indices.Select(w => (uint)w).ToList();
            //var collider = new ConvexHullColliderShapeDesc()
            //{
            //    Model = model, // seems doing nothing
            //    ConvexHulls = [],
            //    ConvexHullsIndices = []
            //};

            //collider.ConvexHulls.Add([points]);
            //collider.ConvexHullsIndices.Add([uintIndices]);

            //List<IAssetColliderShapeDesc> descriptions = [];

            //descriptions.Add(collider);

            //var collider2 = new ConvexHullCollider() { Hull = new PhysicsColliderShape(descriptions) };

            //var compoundCollier = options.Component.Collider as CompoundCollider;

            //compoundCollier.Colliders.Add(collider2);

            // Or you can use just his
            options.Component.Collider = new MeshCollider() { Model = model, Closed = true };

            entity.Add(options.Component);
        }
        else
        {
            var colliderShape = Get2DColliderShapeWithBepu(type, options.Size, options.Depth);

            if (colliderShape is null) return entity;

            var compoundCollier = options.Component.Collider as CompoundCollider;

            compoundCollier.Colliders.Add(colliderShape);

            entity.Add(options.Component);
        }

        return entity;
    }

    public static Entity Create3DPrimitiveWithBepu(this IGame game, PrimitiveModelType type, Primitive3DCreationOptionsWithBepu? options = null)
    {
        options ??= new();

        var modelBase = Procedural3DModelBuilder.Build(type, options.Size);

        var model = modelBase.Generate(game.Services);

        if (options.Material != null)
        {
            model.Materials.Add(options.Material);
        }

        var entity = new Entity(options.EntityName) { new ModelComponent(model) { RenderGroup = options.RenderGroup } };

        if (!options.IncludeCollider) return entity;

        if (type == PrimitiveModelType.TriangularPrism)
        {
            // This is needed when using ConvexHullCollider
            //var meshData = TriangularPrismProceduralModel.New(options.Size is null ? new(1, 1, options.Depth) : new(options.Size.Value.X, options.Size.Value.Y, options.Depth));

            //var points = meshData.Vertices.Select(w => w.Position).ToList();
            //var uintIndices = meshData.Indices.Select(w => (uint)w).ToList();
            //var collider = new ConvexHullColliderShapeDesc()
            //{
            //    Model = model, // seems doing nothing
            //    ConvexHulls = [],
            //    ConvexHullsIndices = []
            //};

            //collider.ConvexHulls.Add([points]);
            //collider.ConvexHullsIndices.Add([uintIndices]);

            //List<IAssetColliderShapeDesc> descriptions = [];

            //descriptions.Add(collider);

            //var collider2 = new ConvexHullCollider() { Hull = new PhysicsColliderShape(descriptions) };

            //var compoundCollier = options.Component.Collider as CompoundCollider;

            //compoundCollier.Colliders.Add(collider2);

            // Or you can use just his
            options.Component.Collider = new MeshCollider() { Model = model, Closed = true };

            entity.Add(options.Component);
        }
        else
        {
            var colliderShape = Get3DColliderShapeWithBepu(type, options.Size);

            if (colliderShape is null) return entity;

            var compoundCollier = options.Component.Collider as CompoundCollider;

            compoundCollier.Colliders.Add(colliderShape);

            entity.Add(options.Component);
        }

        return entity;
    }

    private static ColliderBase? Get2DColliderShapeWithBepu(Primitive2DModelType type, Vector2? size = null, float depth = 0)
    {
        return type switch
        {
            Primitive2DModelType.Rectangle => size is null ? new BoxCollider() : new() { Size = new(size.Value.X, size.Value.Y, depth) },
            Primitive2DModelType.Square => size is null ? new BoxCollider() : new() { Size = new(size.Value.X, size.Value.Y, depth) },
            Primitive2DModelType.Circle => size is null ? new CylinderCollider() : new()
            {
                Radius = size.Value.X,
                Length = depth,
                RotationLocal = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90))
            },
            //Primitive2DModelType.Triangle => triangleCollider ?? new TriangleCollider(),
            _ => throw new InvalidOperationException(),
        };
    }

    private static ColliderBase? Get3DColliderShapeWithBepu(PrimitiveModelType type, Vector3? size = null)
    => type switch
    {
        PrimitiveModelType.Plane => size is null ? new BoxCollider() : new() { Size = new Vector3(size.Value.X, 0, size.Value.Y) },
        PrimitiveModelType.Cube => size is null ? new BoxCollider() : new() { Size = size ?? Vector3.One },
        PrimitiveModelType.RectangularPrism => size is null ? new BoxCollider() : new() { Size = size ?? Vector3.One },
        PrimitiveModelType.Cylinder => size is null ? new CylinderCollider() : new()
        {
            Radius = size.Value.X,
            Length = size.Value.Z,
            //RotationLocal = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90))
        },
        _ => throw new InvalidOperationException(),
    };
}

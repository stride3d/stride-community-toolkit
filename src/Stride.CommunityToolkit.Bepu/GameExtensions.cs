using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
///  Provides extension methods for the <see cref="Game"/> class to simplify common game setup tasks for the Bepu Physics engine.
/// </summary>
public static class GameExtensions
{
    public static void SetupBase2DScene(this Game game)
    {
        game.AddGraphicsCompositor().AddCleanUIStage();
        game.Add2DCamera().Add2DCameraController();
        game.Add3DGround();
    }

    /// <summary>
    /// Sets up a default 3D scene for the game, similar to creating an empty project through the editor.
    /// </summary>
    /// <remarks>
    /// This method performs the following setup operations in sequence:
    /// 1. Adds a default GraphicsCompositor to the game's SceneSystem and applies a clean UI stage.
    /// 2. Adds a camera to the game and sets it up with a MouseLookCamera component.
    /// 3. Adds a directional light to the game scene.
    /// 4. Adds ground geometry to the game scene.
    /// </remarks>
    /// <param name="game">The Game instance for which the base 3D scene will be set up.</param>
    public static void SetupBase3DScene(this Game game)
    {
        game.AddGraphicsCompositor().AddCleanUIStage();
        game.Add3DCamera().Add3DCameraController();
        game.AddDirectionalLight();
        game.Add3DGround();
    }

    /// <summary>
    /// Adds a 3D ground entity to the game with a default size of 15x15 units. The ground is created as a plane, and a collider can be optionally added.
    /// </summary>
    /// <param name="game">
    /// The <see cref="Game"/> instance to which the ground entity will be added.
    /// </param>
    /// <param name="entityName">
    /// The name to assign to the new ground entity.
    /// If <c>null</c>, <see cref="GameDefaults.DefaultGroundName"/> is used.
    /// </param>
    /// <param name="size">
    /// The width and length of the ground plane as a <see cref="Vector2"/>.
    /// If <c>null</c>, <see cref="GameDefaults.Default3DGroundSize"/> is used.
    /// </param>
    /// <param name="includeCollider">
    /// If <c>true</c>, a <see cref="CompoundCollider"/> is added to the entity for physics interactions; otherwise no collider is created.
    /// </param>
    /// <returns>The newly created <see cref="Entity"/> representing the 3D ground plane.</returns>
    /// <example>
    /// <code>
    /// // Add a 20×20 ground plane named "MyGround" with a collider
    /// var ground = game.Add3DGround("MyGround", new Vector2(20, 20), includeCollider: true);
    /// </code>
    /// </example>
    public static Entity Add3DGround(this Game game, string? entityName = GameDefaults.DefaultGroundName, Vector2? size = null, bool includeCollider = true)
        => CreateGround(game, entityName, size, includeCollider, PrimitiveModelType.Plane);

    private static Entity CreateGround(Game game, string? entityName, Vector2? size, bool includeCollider, PrimitiveModelType type)
    {
        var validSize = size ?? GameDefaults.Default3DGroundSize;

        var material = game.CreateMaterial(GameDefaults.DefaultGroundMaterialColor, 0.0f, 0.1f);

        var entity = game.Create3DPrimitive(type, new Primitive3DCreationOptions()
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

    public static Entity Create2DPrimitive(this IGame game, Primitive2DModelType type, Primitive2DCreationOptions? options = null)
    {
        options ??= new();

        var modelBase = Procedural2DModelBuilder.Build(type, options.Size, options.Depth);

        var model = modelBase.Generate(game.Services);

        if (options.Material != null)
            model.Materials.Add(options.Material);

        var entity = new Entity(options.EntityName);

        if (type == Primitive2DModelType.Circle)
        {
            var childEntity = new Entity("Child") { new ModelComponent(model) { RenderGroup = options.RenderGroup } };
            childEntity.Transform.Rotation = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90));
            entity.AddChild(childEntity);
        }
        else
            entity.Add(new ModelComponent(model) { RenderGroup = options.RenderGroup });

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
            var colliderShape = Get2DColliderShape(type, options.Size, options.Depth);

            if (colliderShape is null) return entity;

            var compoundCollier = options.Component.Collider as CompoundCollider;

            compoundCollier.Colliders.Add(colliderShape);

            entity.Add(options.Component);
        }

        return entity;
    }

    public static Entity Create3DPrimitive(this IGame game, PrimitiveModelType type, Primitive3DCreationOptions? options = null)
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
            var colliderShape = Get3DColliderShape(type, options.Size);

            if (colliderShape is null) return entity;

            var compoundCollier = options.Component.Collider as CompoundCollider;

            compoundCollier.Colliders.Add(colliderShape);

            entity.Add(options.Component);
        }

        return entity;
    }

    private static ColliderBase? Get2DColliderShape(Primitive2DModelType type, Vector2? size = null, float depth = 0)
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

    private static ColliderBase? Get3DColliderShape(PrimitiveModelType type, Vector3? size = null)
    => type switch
    {
        PrimitiveModelType.Plane => size is null ? new BoxCollider() : new() { Size = new Vector3(size.Value.X, 0, size.Value.Y) },
        PrimitiveModelType.Sphere => size is null ? new SphereCollider() : new() { Radius = size.Value.X },
        PrimitiveModelType.Cube => size is null ? new BoxCollider() : new() { Size = size ?? Vector3.One },
        PrimitiveModelType.RectangularPrism => size is null ? new BoxCollider() : new() { Size = size ?? Vector3.One },
        PrimitiveModelType.Cylinder => size is null ? new CylinderCollider() : new()
        {
            Radius = size.Value.X,
            Length = size.Value.Z,
            //RotationLocal = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90))
        },
        PrimitiveModelType.Capsule => size is null ? new CapsuleCollider() { Radius = 0.35f } : new() { Radius = size.Value.X, Length = size.Value.Y },
        _ => throw new InvalidOperationException(),
    };
}
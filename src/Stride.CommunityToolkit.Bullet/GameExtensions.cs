using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Physics;

namespace Stride.CommunityToolkit.Bullet;

/// <summary>
///  Provides extension methods for the <see cref="Game"/> class to simplify common game setup tasks for the Bullet Physics engine.
/// </summary>
public static class GameExtensions
{
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

    public static void SetupBase2DScene(this Game game)
    {
        game.AddGraphicsCompositor().AddCleanUIStage();
        game.Add2DCamera().Add2DCameraController();
        //game.AddDirectionalLight();
        game.Add2DGround();
    }

    /// <summary>
    /// Adds a 3D ground entity to the game with a default size of 10x10 units. The ground is created as a plane, and a collider can be optionally added.
    /// </summary>
    /// <param name="game">The Game instance to which the ground entity will be added.</param>
    /// <param name="entityName">The optional name for the ground entity. If not provided, it defaults to "Ground".</param>
    /// <param name="size">The size of the ground, specified as a 2D vector. If not provided, it defaults to (10, 10) units.</param>
    /// <param name="includeCollider">Specifies whether to add a collider to the ground. Defaults to true.</param>
    /// <returns>The created Entity object representing the 3D ground.</returns>
    public static Entity Add3DGround(this Game game, string? entityName = GameDefaults.DefaultGroundName, Vector2? size = null, bool includeCollider = true)
        => CreateGround(game, entityName, size, includeCollider, PrimitiveModelType.Plane);

    /// <summary>
    /// Adds an infinite 3D ground entity to the game. The visible part of the ground is defined by the <paramref name="size"/> parameter,
    /// while the collider is infinite and extends beyond the visible ground.
    /// </summary>
    /// <param name="game">The Game instance to which the infinite ground entity will be added.</param>
    /// <param name="entityName">The optional name for the ground entity. If not provided, it defaults to "Ground".</param>
    /// <param name="size">Defines the visible part of the ground, specified as a 2D vector. If not provided, it defaults to (10, 10) units.</param>
    /// <param name="includeCollider">Specifies whether to add a collider to the ground. The collider is infinite, extending beyond the visible part. Defaults to true.</param>
    /// <returns>The created Entity object representing the infinite 3D ground.</returns>
    public static Entity AddInfinite3DGround(this Game game, string? entityName = GameDefaults.DefaultGroundName, Vector2? size = null, bool includeCollider = true)
        => CreateGround(game, entityName, size, includeCollider, PrimitiveModelType.InfinitePlane);

    public static Entity Add2DGround(this Game game, string? entityName = GameDefaults.DefaultGroundName, Vector2? size = null)
    {
        var validSize = size is null ? GameDefaults._default2DGroundSize : new Vector3(size.Value.X, size.Value.Y, 0);

        var material = game.CreateMaterial(GameDefaults._defaultGroundMaterialColor, 0.0f, 0.1f);

        var proceduralModel = Procedural3DModelBuilder.Build(PrimitiveModelType.Cube, validSize);
        var model = proceduralModel.Generate(game.Services);

        if (material != null)
        {
            model.Materials.Add(material);
        }

        var entity = new Entity(entityName) { new ModelComponent(model) };

        var collider = new StaticColliderComponent();

        //collider.ColliderShape = new StaticPlaneColliderShape(Vector3.UnitY, 0)
        //{
        //    LocalOffset = new Vector3(0, 0, 0),
        //};

        collider.ColliderShape = new BoxColliderShape(is2D: true, validSize)
        {
            LocalOffset = new Vector3(0, 0, 0),
        };

        entity.Add(collider);

        entity.Scene = game.SceneSystem.SceneInstance.RootScene;

        return entity;
    }

    public static Entity Create2DPrimitive(this IGame game, Primitive2DModelType type, Primitive2DCreationOptions? options = null)
    {
        options ??= new();

        var modelBase = Procedural2DModelBuilder.Build(type, options.Size, options.Depth);

        //proceduralModel.SetMaterial("Material", options.Material);

        var model = modelBase.Generate(game.Services);

        //model.Add(options.Material);

        if (options.Material != null)
        {
            model.Materials.Add(options.Material);
        }

        var entity = new Entity(options.EntityName) { new ModelComponent(model) { RenderGroup = options.RenderGroup } };

        if (type == Primitive2DModelType.Circle)
        {
            entity.Transform.Rotation = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90));
        }

        if (!options.IncludeCollider || options.PhysicsComponent is null) return entity;

        if (type == Primitive2DModelType.Triangle)
        {
            //var a = new TriangularPrismProceduralModel() { Size = new(options.Size.Value.X, options.Size.Value.Y, options.Depth) };

            var meshData = TriangularPrismProceduralModel.New(options.Size is null ? new(1, 1, options.Depth) : new(options.Size.Value.X, options.Size.Value.Y, options.Depth));

            var points = meshData.Vertices.Select(w => w.Position).ToList();
            var uintIndices = meshData.Indices.Select(w => (uint)w).ToList();
            var collider = new ConvexHullColliderShapeDesc()
            {
                //Model = model, // seems doing nothing
                Scaling = new(0.9f),
                //LocalOffset = new(20, 20, 10),
                ConvexHulls = [],
                ConvexHullsIndices = []
            };

            collider.ConvexHulls.Add([points]);
            collider.ConvexHullsIndices.Add([uintIndices]);

            //var shapee = collider.CreateShape(game.Services);
            //var collider = new ConvexHullColliderShape(points, uintIndices, Vector3.Zero);
            //var cs = new PhysicsColliderShape(descriptions);


            List<IAssetColliderShapeDesc> descriptions = [];

            descriptions.Add(collider);

            var colliderShapeAsset = new ColliderShapeAssetDesc
            {
                Shape = new PhysicsColliderShape(descriptions)
            };

            options.PhysicsComponent.ColliderShapes.Add(colliderShapeAsset);
            //options.PhysicsComponent.ColliderShape = shapee;
            //options.PhysicsComponent.ColliderShape = collider;
        }
        else
        {
            var colliderShape = Get2DColliderShape(type, options.Size, options.Depth);

            if (colliderShape is null) return entity;

            options.PhysicsComponent.ColliderShapes.Add(colliderShape);
        }

        entity.Add(options.PhysicsComponent);

        return entity;
    }

    /// <summary>
    /// Creates a primitive 3D model entity of the specified type with optional customizations.
    /// </summary>
    /// <param name="game">The game instance.</param>
    /// <param name="type">The type of primitive model to create.</param>
    /// <param name="options">The options for creating the primitive model. If null, default options are used.</param>
    /// <returns>A new entity representing the specified primitive model.</returns>
    /// <remarks>
    /// The <paramref name="options"/> parameter allows specifying various settings such as entity name, material,
    /// collider inclusion, size, render group, and 2D flag. Dimensions in the Vector3 for size are used in the order X, Y, Z.
    /// If size is null, default dimensions are used for the model. If no collider is included, the entity is returned without it.
    /// </remarks>
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

        if (!options.IncludeCollider || options.PhysicsComponent is null) return entity;

        var colliderShape = Get3DColliderShape(type, options.Size);

        if (colliderShape is null) return entity;

        options.PhysicsComponent.ColliderShapes.Add(colliderShape);

        entity.Add(options.PhysicsComponent);

        return entity;
    }

    private static Entity CreateGround(Game game, string? entityName, Vector2? size, bool includeCollider, PrimitiveModelType type)
    {
        var validSize = size ?? GameDefaults._default3DGroundSize;

        var material = game.CreateMaterial(GameDefaults._defaultGroundMaterialColor, 0.0f, 0.1f);

        var entity = game.Create3DPrimitive(type, new()
        {
            EntityName = entityName,
            Material = material,
            IncludeCollider = includeCollider,
            Size = (Vector3)validSize,
            PhysicsComponent = new StaticColliderComponent()
        });

        // seems doing nothing
        //rigidBody.CcdMotionThreshold = 100;
        //rigidBody.CcdSweptSphereRadius = 100

        entity.Scene = game.SceneSystem.SceneInstance.RootScene;

        return entity;
    }

    private static IInlineColliderShapeDesc? Get2DColliderShape(Primitive2DModelType type, Vector2? size = null, float depth = 0)
        => type switch
        {
            Primitive2DModelType.Rectangle => size is null ? new BoxColliderShapeDesc() { Is2D = true } : new() { Size = new(size.Value.X, size.Value.Y, 0), Is2D = true },
            Primitive2DModelType.Square => size is null ? new BoxColliderShapeDesc() { Is2D = true } : new() { Size = new(size.Value.X, size.Value.Y, 0), Is2D = true },
            Primitive2DModelType.Circle => size is null ? new SphereColliderShapeDesc() : new() { Radius = size.Value.X, Is2D = true },
            Primitive2DModelType.Capsule => size is null ? new CapsuleColliderShapeDesc() : new() { Radius = size.Value.X, Is2D = true },
            _ => throw new InvalidOperationException(),
        };

    private static IInlineColliderShapeDesc? Get3DColliderShape(PrimitiveModelType type, Vector3? size = null)
        => type switch
        {
            PrimitiveModelType.Plane => size is null ? new BoxColliderShapeDesc() : new() { Size = new Vector3(size.Value.X, 0, size.Value.Y) },
            PrimitiveModelType.InfinitePlane => new StaticPlaneColliderShapeDesc(),
            PrimitiveModelType.Sphere => size is null ? new SphereColliderShapeDesc() : new() { Radius = size.Value.X },
            PrimitiveModelType.Cube => size is null ? new BoxColliderShapeDesc() : new() { Size = size ?? Vector3.One },
            PrimitiveModelType.Cylinder => size is null ? new CylinderColliderShapeDesc() : new() { Radius = size.Value.X, Height = size.Value.Y },
            PrimitiveModelType.Torus => null,
            PrimitiveModelType.Teapot => null,
            PrimitiveModelType.Cone => size is null ? new ConeColliderShapeDesc() : new() { Radius = size.Value.X, Height = size.Value.Y },
            PrimitiveModelType.Capsule => size is null ? new CapsuleColliderShapeDesc() { Radius = 0.35f } : new() { Radius = size.Value.X, Length = size.Value.Y },
            _ => throw new InvalidOperationException(),
        };
}
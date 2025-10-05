using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
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
    public static void SetupBase2DScene(this Game game)
    {
        game.SetupBase2D();
        game.Add2DCameraController();
        game.Add2DGround();
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
        game.SetupBase3D();
        game.Add3DCameraController();
        game.Add3DGround();
    }

    /// <summary>
    /// Adds a 3D ground entity to the game with a default size of 15x15 units. The ground is created as a plane, and a collider can be optionally added.
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
        var validSize = size is null ? GameDefaults.Default2DGroundSize : new Vector3(size.Value.X, size.Value.Y, 0);

        var material = game.CreateMaterial(GameDefaults.DefaultGroundMaterialColor, 0.0f, 0.1f);

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

    public static Entity Create2DPrimitive(this IGame game, Primitive2DModelType type, Bullet2DPhysicsOptions? options = null)
    {
        options ??= new();

        var entity = game.Create2DPrimitive(type, (Primitive2DEntityOptions)options);

        entity.AddBullet2DPhysics(type, options);

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
    public static Entity Create3DPrimitive(this IGame game, PrimitiveModelType type, Bullet3DPhysicsOptions? options = null)
    {
        options ??= new();

        var entity = game.Create3DPrimitive(type, (Primitive3DEntityOptions)options);
        //var entity = Games.GameExtensions.Create3DPrimitive(game, type, options);

        entity.AddBullet3DPhysics(type, options);

        return entity;
    }

    /// <summary>
    /// Enables the visualization of collider shapes in the game scene. This feature is useful for debugging physics-related issues.
    /// </summary>
    /// <param name="game">The current game instance.</param>
    /// <remarks>
    /// This method activates the rendering of collider shapes within the physics simulation. It helps to visually inspect and debug the positioning and behaviour of colliders at runtime.
    /// </remarks>
    public static void ShowColliders(this Game game)
    {
        var simulation = game.SceneSystem.SceneInstance.GetProcessor<PhysicsProcessor>()?.Simulation;

        if (simulation is null) return;

        simulation.ColliderShapesRendering = true;
    }

    private static Entity CreateGround(Game game, string? entityName, Vector2? size, bool includeCollider, PrimitiveModelType type)
    {
        var validSize = size ?? GameDefaults.Default3DGroundSize;

        var material = game.CreateMaterial(GameDefaults.DefaultGroundMaterialColor, 0.0f, 0.1f);

        var entity = game.Create3DPrimitive(type, new Bullet3DPhysicsOptions()
        {
            EntityName = entityName,
            Material = material,
            Size = (Vector3)validSize,
            PhysicsComponent = new StaticColliderComponent(),
            IncludeCollider = includeCollider
        });

        // seems doing nothing
        //rigidBody.CcdMotionThreshold = 100;
        //rigidBody.CcdSweptSphereRadius = 100

        entity.Scene = game.SceneSystem.SceneInstance.RootScene;

        return entity;
    }
}
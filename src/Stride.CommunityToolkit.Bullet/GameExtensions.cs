using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Engine;
using Stride.Games;
using Stride.Physics;

namespace Stride.CommunityToolkit.Bullet;

/// <summary>
/// Provides extension methods for <see cref="Game"/> and <see cref="IGame"/> to simplify common scene setup and
/// primitive creation tasks with Bullet physics.
/// </summary>
public static class GameExtensions
{
    /// <summary>
    /// Sets up a default 2D scene for the game, similar to creating an empty project through the editor.
    /// </summary>
    /// <param name="game">The game instance to configure with the base 2D scene setup.</param>
    /// <remarks>
    /// This method performs the following setup operations in sequence:<br />
    /// 1. Configures base 2D scene settings.<br />
    /// 2. Adds a 2D camera controller.<br />
    /// 3. Adds a 2D ground entity with Bullet physics.
    /// </remarks>
    public static void SetupBase2DScene(this Game game)
    {
        game.SetupBase2D();
        game.Add2DCameraController();
        game.Add2DGround();
    }

    /// <summary>
    /// Sets up a default 3D scene for the game, similar to creating an empty project through the editor.
    /// </summary>
    /// <param name="game">The game instance to configure with the base 3D scene setup.</param>
    /// <remarks>
    /// This method performs the following setup operations in sequence:<br />
    /// 1. Configures base 3D scene settings.<br />
    /// 2. Adds a 3D camera controller.<br />
    /// 3. Adds a 3D ground entity with Bullet physics.
    /// </remarks>
    public static void SetupBase3DScene(this Game game)
    {
        game.SetupBase3D();
        game.Add3DCameraController();
        game.Add3DGround();
    }

    /// <summary>
    /// Adds a 2D ground entity to the game using a cube primitive and Bullet static physics.
    /// </summary>
    /// <param name="game">The game instance to which the ground entity will be added.</param>
    /// <param name="options">
    /// Optional 2D physics options used to configure the ground. When provided, <see cref="Primitive2DEntityOptions.Size"/> is mapped to X/Y while Z uses <see cref="GameDefaults.Default2DGroundSize"/>, and <see cref="PrimitiveEntityOptions.Position"/> defaults to <see cref="GameDefaults.Default2DGroundPosition"/>.
    /// </param>
    /// <returns>The newly created ground <see cref="Entity"/> added to the game.</returns>
    /// <remarks>
    /// The resulting entity is created through 3D primitive generation and uses a <see cref="StaticColliderComponent"/> when no physics component is supplied in <paramref name="options"/>.
    /// </remarks>
    public static Entity Add2DGround(this Game game, Bullet2DPhysicsOptions? options = null)
    {
        var size = options?.Size is null ? GameDefaults.Default2DGroundSize : new(options.Size.Value.X, options.Size.Value.Y, GameDefaults.Default2DGroundSize.Z);

        var options3D = new Bullet3DPhysicsOptions
        {
            EntityName = options?.EntityName ?? GameDefaults.DefaultGroundName,
            Size = size,
            Position = options?.Position ?? GameDefaults.Default2DGroundPosition,
            Material = game.CreateFlatMaterial(GameDefaults.Default2DGroundMaterialColor),
            PhysicsComponent = options?.PhysicsComponent ?? new StaticColliderComponent()
        };

        return CreateGround(game, PrimitiveModelType.Cube, options3D);
    }

    /// <summary>
    /// Adds a 3D ground entity to the game using a plane primitive and Bullet static physics.
    /// </summary>
    /// <param name="game">The game instance to which the ground entity will be added.</param>
    /// <param name="options">Optional 3D physics options used to configure the ground. If <c>null</c>, defaults will be used.</param>
    /// <returns>The newly created ground <see cref="Entity"/> added to the game.</returns>
    /// <remarks>
    /// When <paramref name="options"/> is <see langword="null"/>, default options are created with a <see cref="StaticColliderComponent"/>. If <see cref="PrimitiveEntityOptions.EntityName"/> is not provided, <see cref="GameDefaults.DefaultGroundName"/> is used.
    /// </remarks>
    public static Entity Add3DGround(this Game game, Bullet3DPhysicsOptions? options = null)
    {
        var physicsComponent = new StaticColliderComponent();

        options ??= new Bullet3DPhysicsOptions() { PhysicsComponent = physicsComponent };
        options.EntityName ??= GameDefaults.DefaultGroundName;

        return CreateGround(game, PrimitiveModelType.Plane, options);
    }

    /// <summary>
    /// Adds an infinite 3D ground entity to the game using an infinite plane primitive and Bullet static physics.
    /// </summary>
    /// <param name="game">The game instance to which the infinite ground entity will be added.</param>
    /// <param name="options">Optional 3D physics options used to configure the ground. If <c>null</c>, defaults will be used.</param>
    /// <returns>The newly created infinite ground <see cref="Entity"/> added to the game.</returns>
    /// <remarks>
    /// When <paramref name="options"/> is <see langword="null"/>, default options are created with a <see cref="StaticColliderComponent"/>. If <see cref="PrimitiveEntityOptions.EntityName"/> is not provided, <see cref="GameDefaults.DefaultGroundName"/> is used.
    /// The visible part of the ground is defined by <paramref name="options"/>, while the collider is infinite and extends beyond the visible ground.
    /// </remarks>
    public static Entity AddInfinite3DGround(this Game game, Bullet3DPhysicsOptions? options = null)
    {
        var physicsComponent = new StaticColliderComponent();

        options ??= new Bullet3DPhysicsOptions() { PhysicsComponent = physicsComponent };
        options.EntityName ??= GameDefaults.DefaultGroundName;

        return CreateGround(game, PrimitiveModelType.InfinitePlane, options);
    }

    /// <summary>
    /// Creates a 2D primitive entity and attaches Bullet 2D physics as defined by <paramref name="options"/>.
    /// </summary>
    /// <param name="game">The game instance.</param>
    /// <param name="type">The type of 2D primitive shape to create.</param>
    /// <param name="options">Options for both the primitive geometry and physics. If <c>null</c>, defaults will be used.</param>
    /// <returns>The newly created <see cref="Entity"/> with Bullet 2D physics attached.</returns>
    public static Entity Create2DPrimitive(this IGame game, Primitive2DModelType type, Bullet2DPhysicsOptions? options = null)
    {
        options ??= new();

        var entity = game.Create2DPrimitive(type, (Primitive2DEntityOptions)options);

        entity.AddBullet2DPhysics(type, options);

        return entity;
    }

    /// <summary>
    /// Creates a 3D primitive entity and attaches Bullet 3D physics as defined by <paramref name="options"/>.
    /// </summary>
    /// <param name="game">The game instance.</param>
    /// <param name="type">The type of 3D primitive shape to create.</param>
    /// <param name="options">Options for both the primitive geometry and physics. If <c>null</c>, defaults will be used.</param>
    /// <returns>The newly created <see cref="Entity"/> with Bullet 3D physics attached.</returns>
    public static Entity Create3DPrimitive(this IGame game, PrimitiveModelType type, Bullet3DPhysicsOptions? options = null)
    {
        options ??= new();

        var entity = game.Create3DPrimitive(type, (Primitive3DEntityOptions)options);

        entity.AddBullet3DPhysics(type, options);

        return entity;
    }

    /// <summary>
    /// Enables the visualization of collider shapes in the game scene for debugging physics-related issues.
    /// </summary>
    /// <param name="game">The current game instance.</param>
    /// <remarks>
    /// This method activates the rendering of collider shapes within the physics simulation, helping to visually inspect and debug the positioning and behavior of colliders at runtime.
    /// </remarks>
    public static void ShowColliders(this Game game)
    {
        var simulation = game.SceneSystem.SceneInstance.GetProcessor<PhysicsProcessor>()?.Simulation;

        if (simulation is null) return;

        simulation.ColliderShapesRendering = true;
    }

    private static Entity CreateGround(Game game, PrimitiveModelType type, Bullet3DPhysicsOptions options)
    {
        options.Size ??= GameDefaults.Default3DGroundSize;
        options.Material ??= game.CreateMaterial(GameDefaults.DefaultGroundMaterialColor, 0.0f, 0.1f);

        var entity = game.Create3DPrimitive(type, options);

        entity.Scene = game.SceneSystem.SceneInstance.RootScene;

        return entity;
    }
}
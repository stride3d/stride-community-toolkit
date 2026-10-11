using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
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
    /// Adds a 2D ground entity to the game: a flat box in the XY plane with Bullet static physics.
    /// </summary>
    /// <param name="game">The game instance to which the ground entity will be added.</param>
    /// <param name="size">The width (X) and height (Y) of the ground. Defaults to <see cref="GameDefaults.Default2DGroundSize"/>; its depth along Z is always that default's.</param>
    /// <param name="options">Optional physics options for the ground. <see cref="PrimitiveEntityOptionsBase.Position"/> defaults to <see cref="GameDefaults.Default2DGroundPosition"/>.</param>
    /// <returns>The newly created ground <see cref="Entity"/> added to the game.</returns>
    /// <remarks>
    /// The ground gets a <see cref="StaticColliderComponent"/> unless <paramref name="options"/> sets <see cref="Bullet2DPhysicsOptions.PhysicsComponent"/>.
    /// </remarks>
    public static Entity Add2DGround(this Game game, Vector2? size = null, Bullet2DPhysicsOptions? options = null)
    {
        var groundSize = size ?? new Vector2(GameDefaults.Default2DGroundSize.X, GameDefaults.Default2DGroundSize.Y);

        var options3D = new Bullet3DPhysicsOptions
        {
            EntityName = options?.EntityName ?? GameDefaults.DefaultGroundName,
            Position = options?.Position ?? GameDefaults.Default2DGroundPosition,
            Material = game.CreateFlatMaterial(GameDefaults.Default2DGroundMaterialColor),
            PhysicsComponent = options is { IsPhysicsComponentSet: true } ? options.PhysicsComponent : new StaticColliderComponent()
        };

        return CreateGround(game, Primitive3D.Box(new Vector3(groundSize.X, groundSize.Y, GameDefaults.Default2DGroundSize.Z)), options3D);
    }

    /// <summary>
    /// Adds a 3D ground entity to the game: a plane with Bullet static physics.
    /// </summary>
    /// <param name="game">The game instance to which the ground entity will be added.</param>
    /// <param name="size">The width (X) and length (Z) of the ground. Defaults to <see cref="GameDefaults.Default3DGroundSize"/>.</param>
    /// <param name="options">Optional physics options for the ground. If <c>null</c>, defaults will be used.</param>
    /// <returns>The newly created ground <see cref="Entity"/> added to the game.</returns>
    /// <remarks>
    /// The ground gets a <see cref="StaticColliderComponent"/> unless <paramref name="options"/> sets <see cref="Bullet3DPhysicsOptions.PhysicsComponent"/>. If <see cref="PrimitiveEntityOptionsBase.EntityName"/> is not provided, <see cref="GameDefaults.DefaultGroundName"/> is used.
    /// </remarks>
    public static Entity Add3DGround(this Game game, Vector2? size = null, Bullet3DPhysicsOptions? options = null)
    {
        options ??= new Bullet3DPhysicsOptions();

        if (!options.IsPhysicsComponentSet) options.PhysicsComponent = new StaticColliderComponent();

        options.EntityName ??= GameDefaults.DefaultGroundName;

        var groundSize = size ?? new Vector2(GameDefaults.Default3DGroundSize.X, GameDefaults.Default3DGroundSize.Z);

        return CreateGround(game, Primitive3D.Plane(groundSize.X, groundSize.Y), options);
    }

    /// <summary>
    /// Adds an infinite 3D ground entity to the game using an infinite plane primitive and Bullet static physics.
    /// </summary>
    /// <param name="game">The game instance to which the infinite ground entity will be added.</param>
    /// <param name="size">The width (X) and length (Z) of the visible plane. Defaults to <see cref="GameDefaults.Default3DGroundSize"/>; the collider is infinite either way.</param>
    /// <param name="options">Optional 3D physics options used to configure the ground. If <c>null</c>, defaults will be used.</param>
    /// <returns>The newly created infinite ground <see cref="Entity"/> added to the game.</returns>
    /// <remarks>
    /// The ground gets a <see cref="StaticColliderComponent"/> unless <paramref name="options"/> sets <see cref="Bullet3DPhysicsOptions.PhysicsComponent"/>. If <see cref="PrimitiveEntityOptionsBase.EntityName"/> is not provided, <see cref="GameDefaults.DefaultGroundName"/> is used.
    /// The visible part of the ground is defined by <paramref name="options"/>, while the collider is infinite and extends beyond the visible ground.
    /// </remarks>
    public static Entity AddInfinite3DGround(this Game game, Vector2? size = null, Bullet3DPhysicsOptions? options = null)
    {
        options ??= new Bullet3DPhysicsOptions();

        if (!options.IsPhysicsComponentSet) options.PhysicsComponent = new StaticColliderComponent();

        options.EntityName ??= GameDefaults.DefaultGroundName;
        options.Material ??= game.CreateMaterial(GameDefaults.DefaultGroundMaterialColor, 0.0f, 0.1f);

        // The visible plane is finite; the collider is Bullet's infinite static plane, not the plane's own
        // box, so the shape's collider is left out and the plane collider added in its place
        options.IncludeCollider = false;

        var groundSize = size ?? new Vector2(GameDefaults.Default3DGroundSize.X, GameDefaults.Default3DGroundSize.Z);
        var entity = game.CreatePrimitive(Primitive3D.Plane(groundSize.X, groundSize.Y), options);

        options.PhysicsComponent?.ColliderShapes.Add(new StaticPlaneColliderShapeDesc());

        entity.Scene = game.SceneSystem.SceneInstance.RootScene;

        return entity;
    }

    /// <summary>
    /// Creates an entity with the model of a flat 2D shape and a matching Bullet collider, using the default options.
    /// </summary>
    /// <param name="game">The game instance.</param>
    /// <param name="shape">The shape and its dimensions, from the <see cref="Primitive2D"/> factories.</param>
    /// <returns>The newly created <see cref="Entity"/> with Bullet 2D physics attached.</returns>
    /// <remarks>
    /// <para>Makes <c>game.CreatePrimitive(shape)</c> with a 2D shape resolve here rather than to the physics-free overload in
    /// <c>Stride.CommunityToolkit.Engine</c>: C# and F# prefer the overload with no default arguments to fill in.
    /// VB callers pass the options explicitly. Passing a <c>Primitive2DEntityOptions</c> reaches the physics-free
    /// overload. A literal <c>null</c> for the options is ambiguous; pass a typed null.</para>
    /// </remarks>
    public static Entity CreatePrimitive(this IGame game, Primitive2D shape)
        => game.CreatePrimitive(shape, (Bullet2DPhysicsOptions?)null);

    /// <summary>
    /// Creates an entity with the model of a flat 2D shape and attaches Bullet 2D physics as defined by <paramref name="options"/>.
    /// </summary>
    /// <param name="game">The game instance.</param>
    /// <param name="shape">The shape and its dimensions, from the <see cref="Primitive2D"/> factories.</param>
    /// <param name="options">Options for both the primitive geometry and physics. If <c>null</c>, defaults will be used.</param>
    /// <returns>The newly created <see cref="Entity"/> with Bullet 2D physics attached.</returns>
    public static Entity CreatePrimitive(this IGame game, Primitive2D shape, Bullet2DPhysicsOptions? options = null)
    {
        options ??= new();

        var entity = game.CreatePrimitive(shape, (Primitive2DEntityOptions)options);

        entity.AddBullet2DPhysics(shape, options);

        return entity;
    }

    /// <summary>
    /// Creates an entity with the model of a 3D shape and a matching Bullet collider, using the default options.
    /// </summary>
    /// <param name="game">The game instance.</param>
    /// <param name="shape">The shape and its dimensions, from the <see cref="Primitive3D"/> factories.</param>
    /// <returns>The newly created <see cref="Entity"/> with Bullet 3D physics attached.</returns>
    /// <remarks>
    /// <para>Makes <c>game.CreatePrimitive(shape)</c> with a 3D shape resolve here rather than to the physics-free overload in
    /// <c>Stride.CommunityToolkit.Engine</c>: C# and F# prefer the overload with no default arguments to fill in.
    /// VB callers pass the options explicitly. Passing a <c>Primitive3DEntityOptions</c> reaches the physics-free
    /// overload. A literal <c>null</c> for the options is ambiguous; pass a typed null.</para>
    /// </remarks>
    public static Entity CreatePrimitive(this IGame game, Primitive3D shape)
        => game.CreatePrimitive(shape, (Bullet3DPhysicsOptions?)null);

    /// <summary>
    /// Creates an entity with the model of a 3D shape and attaches Bullet 3D physics as defined by <paramref name="options"/>.
    /// </summary>
    /// <param name="game">The game instance.</param>
    /// <param name="shape">The shape and its dimensions, from the <see cref="Primitive3D"/> factories.</param>
    /// <param name="options">Options for both the primitive geometry and physics. If <c>null</c>, defaults will be used.</param>
    /// <returns>The newly created <see cref="Entity"/> with Bullet 3D physics attached.</returns>
    public static Entity CreatePrimitive(this IGame game, Primitive3D shape, Bullet3DPhysicsOptions? options = null)
    {
        options ??= new();

        var entity = game.CreatePrimitive(shape, (Primitive3DEntityOptions)options);

        entity.AddBullet3DPhysics(shape, options);

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

    private static Entity CreateGround(Game game, Primitive3D shape, Bullet3DPhysicsOptions options)
    {
        options.Material ??= game.CreateMaterial(GameDefaults.DefaultGroundMaterialColor, 0.0f, 0.1f);

        var entity = game.CreatePrimitive(shape, options);

        entity.Scene = game.SceneSystem.SceneInstance.RootScene;

        return entity;
    }
}
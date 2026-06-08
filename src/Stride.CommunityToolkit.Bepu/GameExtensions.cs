using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Provides extension methods for <see cref="Game"/> and <see cref="IGame"/> to simplify common scene setup and
/// primitive creation tasks with Bepu physics.
/// </summary>
public static class GameExtensions
{
    /// <summary>
    /// Sets up a default 2D scene for the game, similar to creating an empty project through the editor.
    /// </summary>
    /// <param name="game">The game instance to configure with a 2D scene setup.</param>
    /// <remarks>
    /// This method performs the following setup operations in sequence:<br />
    /// 1. Configures base 2D scene settings.<br />
    /// 2. Adds a 2D camera controller.<br />
    /// 3. Adds a 2D ground entity with Bepu physics.
    /// </remarks>
    public static void SetupBase2DScene(this Game game)
    {
        game.SetupBase2D();
        game.Add2DCameraController();
        game.Add2DGround();
    }

    // ToDo: Maybe this could be call SetupDemo3DScene and move to a demo namespace?
    /// <summary>
    /// Sets up a default 3D scene for the game, similar to creating an empty project through the editor.
    /// </summary>
    /// <param name="game">The game instance for which the base 3D scene will be set up.</param>
    /// <remarks>
    /// This method performs the following setup operations in sequence:<br />
    /// 1. Configures base 3D scene settings.<br />
    /// 2. Adds a 3D camera controller.<br />
    /// 3. Adds a 3D ground entity with Bepu physics.
    /// </remarks>
    public static void SetupBase3DScene(this Game game)
    {
        game.SetupBase3D();
        game.Add3DCameraController();
        game.Add3DGround();
    }

    /// <summary>
    /// Adds a 2D ground entity to the game using a cube primitive and Bepu static physics.
    /// </summary>
    /// <param name="game">The game instance to which the ground entity will be added.</param>
    /// <param name="options">
    /// Optional 2D physics options used to configure the ground. When provided, <see cref="Primitive2DEntityOptions.Size"/> is mapped to X/Y while Z uses <see cref="GameDefaults.Default2DGroundSize"/>, and <see cref="PrimitiveEntityOptions.Position"/> defaults to <see cref="GameDefaults.Default2DGroundPosition"/>.
    /// </param>
    /// <returns>The newly created ground <see cref="Entity"/> added to the game.</returns>
    /// <remarks>
    /// The resulting entity is created through 3D primitive generation and uses a <see cref="StaticComponent"/> with a <see cref="CompoundCollider"/> when no physics component is supplied in <paramref name="options"/>.
    /// </remarks>
    public static Entity Add2DGround(this Game game, Bepu2DPhysicsOptions? options = null)
    {
        var size = options?.Size is null ? GameDefaults.Default2DGroundSize : new(options.Size.Value.X, options.Size.Value.Y, GameDefaults.Default2DGroundSize.Z);

        var options3D = new Bepu3DPhysicsOptions
        {
            EntityName = options?.EntityName ?? GameDefaults.DefaultGroundName,
            Size = size,
            Position = options?.Position ?? GameDefaults.Default2DGroundPosition,
            Material = game.CreateFlatMaterial(GameDefaults.Default2DGroundMaterialColor),
            Component = options?.Component ?? new StaticComponent() { Collider = new CompoundCollider() }
        };

        return CreateGround(game, PrimitiveModelType.Cube, options3D);
    }

    /// <summary>
    /// Adds a 3D ground entity to the game using a plane primitive and Bepu static physics.
    /// </summary>
    /// <param name="game">The game instance to which the ground entity will be added.</param>
    /// <param name="options">Options for both the ground geometry and physics. If <c>null</c>, defaults will be used.</param>
    /// <returns>The newly created ground <see cref="Entity"/> added to the game.</returns>
    /// <remarks>
    /// When <paramref name="options"/> is <see langword="null"/>, default options are created with a <see cref="StaticComponent"/> and <see cref="CompoundCollider"/>. If <see cref="PrimitiveEntityOptions.EntityName"/> is not provided, <see cref="GameDefaults.DefaultGroundName"/> is used.
    /// </remarks>
    public static Entity Add3DGround(this Game game, Bepu3DPhysicsOptions? options = null)
    {
        var physicsComponent = new StaticComponent() { Collider = new CompoundCollider() };

        options ??= new Bepu3DPhysicsOptions() { Component = physicsComponent };
        options.EntityName ??= GameDefaults.DefaultGroundName;

        return CreateGround(game, PrimitiveModelType.Plane, options);
    }

    /// <summary>
    /// Creates a 2D primitive entity and attaches Bepu 2D physics as defined by <paramref name="options"/>.
    /// </summary>
    /// <param name="game">The game instance.</param>
    /// <param name="type">The type of 2D primitive shape to create.</param>
    /// <param name="options">Options for both the primitive geometry and physics. If <c>null</c>, defaults will be used.</param>
    /// <returns>The newly created <see cref="Entity"/> with Bepu 2D physics attached.</returns>
    public static Entity Create2DPrimitive(this IGame game, Primitive2DModelType type, Bepu2DPhysicsOptions? options = null)
    {
        options ??= new();

        var entity = game.Create2DPrimitive(type, (Primitive2DEntityOptions)options);

        entity.AddBepu2DPhysics(type, options);

        return entity;
    }

    /// <summary>
    /// Creates a 3D primitive entity and attaches Bepu 3D physics as defined by <paramref name="options"/>.
    /// </summary>
    /// <param name="game">The game instance.</param>
    /// <param name="type">The type of 3D primitive shape to create.</param>
    /// <param name="options">Options for both the primitive geometry and physics. If <c>null</c>, defaults will be used.</param>
    /// <returns>The newly created <see cref="Entity"/> with Bepu 3D physics attached.</returns>
    public static Entity Create3DPrimitive(this IGame game, PrimitiveModelType type, Bepu3DPhysicsOptions? options = null)
    {
        options ??= new();

        var entity = game.Create3DPrimitive(type, (Primitive3DEntityOptions)options);

        entity.AddBepu3DPhysics(type, options);

        return entity;
    }

    private static Entity CreateGround(Game game, PrimitiveModelType type, Bepu3DPhysicsOptions options)
    {
        options.Size ??= GameDefaults.Default3DGroundSize;
        options.Material ??= game.CreateMaterial(GameDefaults.DefaultGroundMaterialColor, 0.0f, 0.1f);

        var entity = game.Create3DPrimitive(type, options);

        entity.Scene = game.SceneSystem.SceneInstance.RootScene;

        return entity;
    }
}
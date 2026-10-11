using Stride.BepuPhysics;
using Stride.Core.Mathematics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Engine;
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
    /// <param name="game">The game instance to configure with the base 2D scene setup.</param>
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

    /// <summary>
    /// Sets up a default 3D scene for the game, similar to creating an empty project through the editor.
    /// </summary>
    /// <param name="game">The game instance to configure with the base 3D scene setup.</param>
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
    /// Adds a 2D ground entity to the game: a flat box in the XY plane with Bepu static physics.
    /// </summary>
    /// <param name="game">The game instance to which the ground entity will be added.</param>
    /// <param name="size">The width (X) and height (Y) of the ground. Defaults to <see cref="GameDefaults.Default2DGroundSize"/>; its depth along Z is always that default's.</param>
    /// <param name="options">Optional physics options for the ground. <see cref="PrimitiveEntityOptionsBase.Position"/> defaults to <see cref="GameDefaults.Default2DGroundPosition"/>.</param>
    /// <returns>The newly created ground <see cref="Entity"/> added to the game.</returns>
    /// <remarks>
    /// The ground uses a <see cref="StaticComponent"/> with a <see cref="CompoundCollider"/> when no physics component is supplied in <paramref name="options"/>.
    /// </remarks>
    public static Entity Add2DGround(this Game game, Vector2? size = null, Bepu2DPhysicsOptions? options = null)
    {
        var groundSize = size ?? new Vector2(GameDefaults.Default2DGroundSize.X, GameDefaults.Default2DGroundSize.Y);

        var options3D = new Bepu3DPhysicsOptions
        {
            EntityName = options?.EntityName ?? GameDefaults.DefaultGroundName,
            Position = options?.Position ?? GameDefaults.Default2DGroundPosition,
            Material = game.CreateFlatMaterial(GameDefaults.Default2DGroundMaterialColor),
            Component = options?.Component ?? new StaticComponent() { Collider = new CompoundCollider() }
        };

        return CreateGround(game, Primitive3D.Box(new Vector3(groundSize.X, groundSize.Y, GameDefaults.Default2DGroundSize.Z)), options3D);
    }

    /// <summary>
    /// Adds a 3D ground entity to the game: a plane with Bepu static physics.
    /// </summary>
    /// <param name="game">The game instance to which the ground entity will be added.</param>
    /// <param name="size">The width (X) and length (Z) of the ground. Defaults to <see cref="GameDefaults.Default3DGroundSize"/>.</param>
    /// <param name="options">Optional physics options for the ground. If <c>null</c>, defaults will be used.</param>
    /// <returns>The newly created ground <see cref="Entity"/> added to the game.</returns>
    /// <remarks>
    /// Unless <see cref="Bepu3DPhysicsOptions.Component"/> is set explicitly, the ground gets a <see cref="StaticComponent"/> with a <see cref="CompoundCollider"/>. If <see cref="PrimitiveEntityOptionsBase.EntityName"/> is not provided, <see cref="GameDefaults.DefaultGroundName"/> is used.
    /// </remarks>
    public static Entity Add3DGround(this Game game, Vector2? size = null, Bepu3DPhysicsOptions? options = null)
    {
        options ??= new();
        options.EntityName ??= GameDefaults.DefaultGroundName;

        // Ground is immovable by default; a null Component would otherwise fall through to the
        // dynamic BodyComponent that primitive creation defaults to
        options.Component ??= new StaticComponent() { Collider = new CompoundCollider() };

        var groundSize = size ?? new Vector2(GameDefaults.Default3DGroundSize.X, GameDefaults.Default3DGroundSize.Z);

        return CreateGround(game, Primitive3D.Plane(groundSize.X, groundSize.Y), options);
    }

    /// <summary>
    /// Creates an entity with the model of a flat 2D shape and a matching Bepu collider, using the default options.
    /// </summary>
    /// <param name="game">The game instance.</param>
    /// <param name="shape">The shape and its dimensions, from the <see cref="Primitive2D"/> factories.</param>
    /// <returns>The newly created <see cref="Entity"/> with Bepu 2D physics attached.</returns>
    /// <remarks>
    /// <para>Makes <c>game.CreatePrimitive(shape)</c> with a 2D shape resolve here rather than to the physics-free overload in
    /// <c>Stride.CommunityToolkit.Engine</c>: C# and F# prefer the overload with no default arguments to fill in.
    /// VB callers pass the options explicitly. Passing a <c>Primitive2DEntityOptions</c> reaches the physics-free
    /// overload. A literal <c>null</c> for the options is ambiguous; pass a typed null.</para>
    /// </remarks>
    public static Entity CreatePrimitive(this IGame game, Primitive2D shape)
        => game.CreatePrimitive(shape, (Bepu2DPhysicsOptions?)null);

    /// <summary>
    /// Creates an entity with the model of a flat 2D shape and attaches Bepu 2D physics as defined by <paramref name="options"/>.
    /// </summary>
    /// <param name="game">The game instance.</param>
    /// <param name="shape">The shape and its dimensions, from the <see cref="Primitive2D"/> factories.</param>
    /// <param name="options">Options for both the primitive geometry and physics. If <c>null</c>, defaults will be used.</param>
    /// <returns>The newly created <see cref="Entity"/> with Bepu 2D physics attached.</returns>
    public static Entity CreatePrimitive(this IGame game, Primitive2D shape, Bepu2DPhysicsOptions? options = null)
    {
        options ??= new();

        var entity = game.CreatePrimitive(shape, (Primitive2DEntityOptions)options);

        entity.AddBepu2DPhysics(shape, options);

        return entity;
    }

    /// <summary>
    /// Creates an entity with the model of a 3D shape and a matching Bepu collider, using the default options.
    /// </summary>
    /// <param name="game">The game instance.</param>
    /// <param name="shape">The shape and its dimensions, from the <see cref="Primitive3D"/> factories.</param>
    /// <returns>The newly created <see cref="Entity"/> with Bepu 3D physics attached.</returns>
    /// <remarks>
    /// <para>Makes <c>game.CreatePrimitive(shape)</c> with a 3D shape resolve here rather than to the physics-free overload in
    /// <c>Stride.CommunityToolkit.Engine</c>: C# and F# prefer the overload with no default arguments to fill in.
    /// VB callers pass the options explicitly. Passing a <c>Primitive3DEntityOptions</c> reaches the physics-free
    /// overload. A literal <c>null</c> for the options is ambiguous; pass a typed null.</para>
    /// </remarks>
    public static Entity CreatePrimitive(this IGame game, Primitive3D shape)
        => game.CreatePrimitive(shape, (Bepu3DPhysicsOptions?)null);

    /// <summary>
    /// Creates an entity with the model of a 3D shape and attaches Bepu 3D physics as defined by <paramref name="options"/>.
    /// </summary>
    /// <param name="game">The game instance.</param>
    /// <param name="shape">The shape and its dimensions, from the <see cref="Primitive3D"/> factories.</param>
    /// <param name="options">Options for both the primitive geometry and physics. If <c>null</c>, defaults will be used.</param>
    /// <returns>The newly created <see cref="Entity"/> with Bepu 3D physics attached.</returns>
    public static Entity CreatePrimitive(this IGame game, Primitive3D shape, Bepu3DPhysicsOptions? options = null)
    {
        options ??= new();

        var entity = game.CreatePrimitive(shape, (Primitive3DEntityOptions)options);

        entity.AddBepu3DPhysics(shape, options);

        return entity;
    }

    private static Entity CreateGround(Game game, Primitive3D shape, Bepu3DPhysicsOptions options)
    {
        options.Material ??= game.CreateMaterial(GameDefaults.DefaultGroundMaterialColor, 0.0f, 0.1f);

        var entity = game.CreatePrimitive(shape, options);

        entity.Scene = game.SceneSystem.SceneInstance.RootScene;

        return entity;
    }

    /// <summary>
    /// Puts a <see cref="GrabberScript"/> on the camera entity, so the left mouse button picks up any
    /// dynamic body, carries it on the end of the camera ray and throws it on release.
    /// </summary>
    /// <param name="game">The game whose camera entity gets the grabber.</param>
    /// <returns>The grabber, for tuning its reach, force and keys.</returns>
    /// <remarks>
    /// The script is also a Game Studio component, under Physics, for a project that would rather place
    /// it on the camera entity in the editor.
    /// </remarks>
    public static GrabberScript AddGrabber(this Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        var grabber = new GrabberScript();

        game.GetCameraEntity().Add(grabber);

        return grabber;
    }
}
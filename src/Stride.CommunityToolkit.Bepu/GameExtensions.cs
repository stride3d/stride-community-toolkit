using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
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
    /// <summary>
    /// Sets up a default 2D scene for the game, similar to creating an empty project through the editor.
    /// </summary>
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
    /// <remarks>
    /// This method performs the following setup operations in sequence:<br />
    /// 1. Adds a default GraphicsCompositor to the game's SceneSystem and applies a clean UI stage.<br />
    /// 2. Adds a camera to the game and sets it up with a MouseLookCamera component.<br />
    /// 3. Adds a directional light to the game scene.<br />
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
    /// Adds a 2D ground entity to the specified game with optional name, size, and collider settings.
    /// </summary>
    /// <remarks>The ground entity is created using a cube primitive model and is suitable for 2D gameplay
    /// scenarios. The default size and name are defined by <see cref="GameDefaults.Default2DGroundSize"/> and <see
    /// cref="GameDefaults.DefaultGroundName"/>, respectively.</remarks>
    /// <param name="game">The game instance to which the ground entity will be added.</param>
    /// <param name="entityName">The name to assign to the ground entity. If null, a default name is used.</param>
    /// <param name="size">The size of the ground entity in world units. If null, a default size is applied.</param>
    /// <param name="includeCollider">Specifies whether a collider should be included with the ground entity. Set to <see langword="true"/> to add a
    /// collider; otherwise, <see langword="false"/>.</param>
    /// <returns>The newly created ground entity added to the game.</returns>
    public static Entity Add2DGround(this Game game, string? entityName = GameDefaults.DefaultGroundName, Vector2? size = null, bool includeCollider = true)
        => CreateGround(game, entityName, size ?? GameDefaults.Default2DGroundSize.XY(), includeCollider, PrimitiveModelType.Cube);

    /// <summary>
    /// Adds a 3D ground entity to the game.
    /// </summary>
    /// <param name="game">Game instance.</param>
    /// <param name="entityName">Optional entity name; defaults to <see cref="GameDefaults.DefaultGroundName"/>.</param>
    /// <param name="size">Optional ground size; defaults to <see cref="GameDefaults.Default3DGroundSize"/>.</param>
    /// <param name="includeCollider">If true, attaches a <see cref="CompoundCollider"/>.</param>
    /// <returns>The created ground <see cref="Entity"/>.</returns>
    public static Entity Add3DGround(this Game game, string? entityName = GameDefaults.DefaultGroundName, Vector2? size = null, bool includeCollider = true)
        => CreateGround(game, entityName, size, includeCollider, PrimitiveModelType.Plane);

    /// <summary>
    /// Creates a 2D primitive entity and attaches a Bepu physics component as defined by <paramref name="options"/>.
    /// </summary>
    public static Entity Create2DPrimitive(this IGame game, Primitive2DModelType type, Bepu2DPhysicsOptions? options = null)
    {
        options ??= new();

        var entity = game.Create2DPrimitive(type, (Primitive2DEntityOptions)options);

        entity.AddBepu2DPhysics(type, options);

        return entity;
    }

    /// <summary>
    /// Creates a 3D primitive entity and attaches a Bepu physics component as defined by <paramref name="options"/>.
    /// </summary>
    /// <param name="game">The game instance.</param>
    /// <param name="type">Which primitive shape to create.</param>
    /// <param name="options">
    /// Options for both the primitive geometry and physics.
    /// If <c>null</c>, defaults will be used.
    /// </param>
    /// <returns>The newly created <see cref="Entity"/>.</returns>
    public static Entity Create3DPrimitive(this IGame game, PrimitiveModelType type, Bepu3DPhysicsOptions? options = null)
    {
        options ??= new();

        var entity = game.Create3DPrimitive(type, (Primitive3DEntityOptions)options);

        entity.AddBepuPhysics(type, options);

        return entity;
    }

    private static Entity CreateGround(Game game, string? entityName, Vector2? size, bool includeCollider, PrimitiveModelType type)
    {
        var validSize = size ?? GameDefaults.Default3DGroundSize;

        var material = game.CreateMaterial(GameDefaults.DefaultGroundMaterialColor, 0.0f, 0.1f);

        var entity = game.Create3DPrimitive(type, new Bepu3DPhysicsOptions()
        {
            EntityName = entityName,
            Material = material,
            Size = (Vector3)validSize,
            Component = new StaticComponent() { Collider = new CompoundCollider() },
            IncludeCollider = includeCollider
        });

        entity.Scene = game.SceneSystem.SceneInstance.RootScene;

        return entity;
    }
}
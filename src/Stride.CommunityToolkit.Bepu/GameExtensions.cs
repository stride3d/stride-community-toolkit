using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
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

    // ToDo: Maybe this could be call SetupDemo3DScene and move to a demo namespace?
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

    public static Entity Create2DPrimitive(this IGame game, Primitive2DModelType type, Bepu2DPhysicsOptions? options = null)
    {
        options ??= new();

        var entity = game.Create2DPrimitive(type, (Primitive2DEntityOptions)options);

        entity.AddBepu2DPhysics(type, options);

        return entity;
    }

    /// <summary>
    /// Creates a 3D primitive (cube, sphere, plane, etc.) and attaches BEPU physics to it.
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
            Component = new StaticComponent() { Collider = new CompoundCollider() }
        });

        entity.Scene = game.SceneSystem.SceneInstance.RootScene;

        return entity;
    }
}
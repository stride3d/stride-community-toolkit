using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
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
}
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Physics;

namespace Stride.CommunityToolkit.Bullet;

/// <summary>
/// Extension methods on <see cref="Game"/> for a scene with Bullet physics. The scene itself is set up through
/// <see cref="SceneSetupExtensions.UseBullet"/> on a <see cref="SceneSetup"/>; the core's <c>CreatePrimitive</c>,
/// <c>Add3DGround</c> and <c>Add2DGround</c> then attach Bullet bodies and colliders. What is here exists only in Bullet.
/// </summary>
public static class GameExtensions
{
    /// <summary>
    /// Adds an infinite ground to the root scene: a visible plane of the given size with Bullet's infinite static
    /// plane as its collider, so nothing can fall off the edge.
    /// </summary>
    /// <param name="game">The game whose root scene gets the ground.</param>
    /// <param name="size">The width (X) and length (Z) of the visible plane. Defaults to <see cref="GameDefaults.Default3DGroundSize"/>; the collider is infinite either way.</param>
    /// <param name="options">Optional physics options. If <c>null</c>, defaults will be used.</param>
    /// <returns>The ground entity, already in the root scene.</returns>
    /// <remarks>
    /// The ground gets a <see cref="StaticColliderComponent"/> unless <paramref name="options"/> sets <see cref="Bullet3DPhysicsOptions.PhysicsComponent"/>. If <see cref="PrimitiveEntityOptionsBase.EntityName"/> is not provided, <see cref="GameDefaults.DefaultGroundName"/> is used.
    /// The physics is attached here whatever the scene setup chose, since the plane collider is Bullet's own.
    /// </remarks>
    public static Entity AddInfinite3DGround(this IGame game, Vector2? size = null, Bullet3DPhysicsOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(game);

        options ??= new Bullet3DPhysicsOptions();

        if (!options.IsPhysicsComponentSet) options.PhysicsComponent = new StaticColliderComponent();

        options.EntityName ??= GameDefaults.DefaultGroundName;
        options.Material ??= game.CreateMaterial(GameDefaults.DefaultGroundMaterialColor, 0.0f, 0.1f);

        // The visible plane is finite; the collider is Bullet's infinite static plane, not the plane's own
        // box, so the shape's collider is left out and the plane collider added in its place
        options.IncludeCollider = false;
        options.IncludePhysics = false;

        var groundSize = size ?? new Vector2(GameDefaults.Default3DGroundSize.X, GameDefaults.Default3DGroundSize.Z);
        var shape = Primitive3D.Plane(groundSize.X, groundSize.Y);
        var entity = game.CreatePrimitive(shape, options);

        entity.AddBulletPhysics(shape, options);

        options.PhysicsComponent?.ColliderShapes.Add(new StaticPlaneColliderShapeDesc());

        entity.Scene = game.GetSceneSystem().SceneInstance.RootScene;

        return entity;
    }

    /// <summary>
    /// Enables the visualization of collider shapes in the game scene for debugging physics-related issues.
    /// </summary>
    /// <param name="game">The current game instance.</param>
    /// <remarks>
    /// This method activates the rendering of collider shapes within the physics simulation, helping to visually inspect and debug the positioning and behavior of colliders at runtime.
    /// </remarks>
    public static void ShowColliders(this IGame game)
    {
        var simulation = game.GetSceneSystem().SceneInstance.GetProcessor<PhysicsProcessor>()?.Simulation;

        if (simulation is null) return;

        simulation.ColliderShapesRendering = true;
    }
}
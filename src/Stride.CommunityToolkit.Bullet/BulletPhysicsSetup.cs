using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Engine;
using Stride.Physics;

namespace Stride.CommunityToolkit.Bullet;

/// <summary>
/// Bullet as the physics engine of a <see cref="SceneSetup"/>: <c>CreatePrimitive</c> gives its entities a
/// <see cref="RigidbodyComponent"/> with the collider fitted to the shape, and the grounds a
/// <see cref="StaticColliderComponent"/>. Chosen with <see cref="SceneSetupExtensions.UseBullet"/>.
/// </summary>
/// <remarks>
/// A <see cref="Bullet3DPhysicsOptions"/> or <see cref="Bullet2DPhysicsOptions"/> passed to <c>CreatePrimitive</c> is
/// read back here, so <see cref="Bullet3DPhysicsOptions.PhysicsComponent"/> and <see cref="Bullet3DPhysicsOptions.IncludeCollider"/>
/// mean what they mean on <see cref="EntityExtensions.AddBulletPhysics(Entity, Primitive3D, Bullet3DPhysicsOptions)"/> and <see cref="EntityExtensions.AddBulletPhysics(Entity, Primitive2D, Bullet2DPhysicsOptions)"/>.
/// </remarks>
public sealed class BulletPhysicsSetup : PhysicsSetupBase
{
    /// <inheritdoc/>
    public override void AddBody(Entity entity, Primitive3D shape, Primitive3DEntityOptions? options)
        => entity.AddBulletPhysics(shape, options as Bullet3DPhysicsOptions);

    /// <inheritdoc/>
    public override void AddBody(Entity entity, Primitive2D shape, Primitive2DEntityOptions? options)
        => entity.AddBulletPhysics(shape, options as Bullet2DPhysicsOptions ?? Carry2D(options));

    /// <inheritdoc/>
    public override void AddStatic(Entity entity, Primitive3D shape, Primitive3DEntityOptions? options)
    {
        var physics = options as Bullet3DPhysicsOptions ?? new Bullet3DPhysicsOptions();

        // Immovable unless the caller supplied its own component
        if (!physics.IsPhysicsComponentSet)
            physics.PhysicsComponent = new StaticColliderComponent();

        entity.AddBulletPhysics(shape, physics);
    }

    /// <inheritdoc/>
    public override void AddStatic(Entity entity, Primitive2D shape, Primitive2DEntityOptions? options)
    {
        var physics = options as Bullet2DPhysicsOptions ?? Carry2D(options);

        if (!physics.IsPhysicsComponentSet)
            physics.PhysicsComponent = new StaticColliderComponent();

        entity.AddBulletPhysics(shape, physics);
    }

    // The collider depth is the one option of the base type the body needs
    private static Bullet2DPhysicsOptions Carry2D(Primitive2DEntityOptions? options)
        => new() { Depth = options?.Depth ?? 1 };
}
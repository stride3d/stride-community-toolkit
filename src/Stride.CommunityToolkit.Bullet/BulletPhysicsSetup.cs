using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Engine;

namespace Stride.CommunityToolkit.Bullet;

/// <summary>
/// Bullet as the physics engine of a scene setup: <c>UseBullet()</c> sets it, and from then on <c>CreatePrimitive</c>
/// and the ground helpers attach a Bullet body or collider through <see cref="EntityExtensions.AddBulletPhysics(Entity, Primitive3D, BulletPhysicsOptions?)"/>.
/// </summary>
/// <remarks>
/// Options of another type than <see cref="BulletPhysicsOptions"/>, the core's own, carry their <see cref="PrimitiveEntityOptions.Body"/>
/// across; everything else of theirs is about the model and was applied before the physics is reached.
/// </remarks>
public sealed class BulletPhysicsSetup : PhysicsSetupBase
{
    /// <inheritdoc/>
    public override void AddBody(Entity entity, Primitive3D shape, PrimitiveEntityOptions? options)
        => entity.AddBulletPhysics(shape, Carry(options));

    /// <inheritdoc/>
    public override void AddBody(Entity entity, Primitive2D shape, PrimitiveEntityOptions? options)
        => entity.AddBulletPhysics(shape, Carry(options));

    private static BulletPhysicsOptions Carry(PrimitiveEntityOptions? options)
        => options as BulletPhysicsOptions ?? new BulletPhysicsOptions { Body = options?.Body ?? BodyMode.Dynamic };
}
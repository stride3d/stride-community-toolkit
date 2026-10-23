using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Engine;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Bepu as the physics engine of a scene setup: <c>UseBepu()</c> sets it, and from then on <c>CreatePrimitive</c>
/// and the ground helpers attach a Bepu body or collider through <see cref="EntityExtensions.AddBepuPhysics(Entity, Primitive3D, BepuPhysicsOptions?)"/>.
/// </summary>
/// <remarks>
/// Options of another type than <see cref="BepuPhysicsOptions"/>, the core's own, carry their <see cref="PrimitiveEntityOptions.Body"/>
/// across; everything else of theirs is about the model and was applied before the physics is reached.
/// </remarks>
public sealed class BepuPhysicsSetup : PhysicsSetupBase
{
    /// <inheritdoc/>
    public override void AddBody(Entity entity, Primitive3D shape, PrimitiveEntityOptions? options)
        => entity.AddBepuPhysics(shape, Carry(options));

    /// <inheritdoc/>
    public override void AddBody(Entity entity, Primitive2D shape, PrimitiveEntityOptions? options)
        => entity.AddBepuPhysics(shape, Carry(options));

    private static BepuPhysicsOptions Carry(PrimitiveEntityOptions? options)
        => options as BepuPhysicsOptions ?? new BepuPhysicsOptions { Body = options?.Body ?? BodyMode.Dynamic };
}
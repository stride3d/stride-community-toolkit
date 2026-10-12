using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Engine;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Bepu as the physics engine of a <see cref="SceneSetup"/>: <c>CreatePrimitive</c> gives its entities a
/// <see cref="BodyComponent"/> (a <see cref="Body2DComponent"/> for a flat shape) with the collider fitted to the
/// shape, and the grounds a <see cref="StaticComponent"/>. Chosen with <see cref="SceneSetupExtensions.UseBepu"/>.
/// </summary>
/// <remarks>
/// A <see cref="Bepu3DPhysicsOptions"/> or <see cref="Bepu2DPhysicsOptions"/> passed to <c>CreatePrimitive</c> is read
/// back here, so <see cref="Bepu3DPhysicsOptions.Component"/> and <see cref="Bepu3DPhysicsOptions.IncludeCollider"/>
/// mean what they mean on <see cref="EntityExtensions.AddBepuPhysics(Entity, Primitive3D, Bepu3DPhysicsOptions)"/> and <see cref="EntityExtensions.AddBepuPhysics(Entity, Primitive2D, Bepu2DPhysicsOptions)"/>.
/// </remarks>
public sealed class BepuPhysicsSetup : PhysicsSetupBase
{
    /// <inheritdoc/>
    public override void AddBody(Entity entity, Primitive3D shape, Primitive3DEntityOptions? options)
        => entity.AddBepuPhysics(shape, options as Bepu3DPhysicsOptions);

    /// <inheritdoc/>
    public override void AddBody(Entity entity, Primitive2D shape, Primitive2DEntityOptions? options)
        => entity.AddBepuPhysics(shape, options as Bepu2DPhysicsOptions ?? Carry2D(options));

    /// <inheritdoc/>
    public override void AddStatic(Entity entity, Primitive3D shape, Primitive3DEntityOptions? options)
    {
        var physics = options as Bepu3DPhysicsOptions ?? new Bepu3DPhysicsOptions();

        // Immovable unless the caller supplied its own component
        physics.Component ??= new StaticComponent { Collider = new CompoundCollider() };

        entity.AddBepuPhysics(shape, physics);
    }

    /// <inheritdoc/>
    public override void AddStatic(Entity entity, Primitive2D shape, Primitive2DEntityOptions? options)
    {
        var physics = options as Bepu2DPhysicsOptions ?? Carry2D(options);

        physics.Component ??= new StaticComponent { Collider = new CompoundCollider() };

        entity.AddBepuPhysics(shape, physics);
    }

    // The collider depth is the one option of the base type the body needs
    private static Bepu2DPhysicsOptions Carry2D(Primitive2DEntityOptions? options)
        => new() { Depth = options?.Depth ?? 1 };
}
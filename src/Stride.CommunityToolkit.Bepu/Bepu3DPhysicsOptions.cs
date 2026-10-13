using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Engine;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Provides options for creating a 3D primitive entity with Bepu physics.
/// </summary>
/// <remarks>
/// <para>Extends <see cref="Primitive3DEntityOptions"/> with a configurable Bepu <see cref="CollidableComponent"/>.</para>
/// <para>When <see cref="Component"/> is left <see langword="null"/>, the creation helper that consumes the options picks a context-appropriate default: a dynamic <see cref="BodyComponent"/> for primitives, a <see cref="StaticComponent"/> for ground helpers.</para>
/// <para>The creation helpers give the component the collider shape that matches the primitive, unless the component already holds a collider of its own.</para>
/// </remarks>
public class Bepu3DPhysicsOptions : Primitive3DEntityOptions
{
    /// <summary>
    /// Gets or sets the Bepu collidable component attached to the entity.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/> (the default), the consuming helper chooses: primitive creation attaches a dynamic <see cref="BodyComponent"/> with an empty <see cref="CompoundCollider"/>, while ground helpers attach a <see cref="StaticComponent"/>. Set it explicitly to override either default. Give it an empty <see cref="CompoundCollider"/> to have the shape that fits the primitive added, or a compound that already holds your own shapes to have it kept as it is: the fitted shape is added to an empty compound only.
    /// </remarks>
    public CollidableComponent? Component { get; set; }

    /// <summary>
    /// Gets or sets the mass of the fitted collider shape, in kilograms, or <see langword="null"/> for Bepu's default of 1.
    /// </summary>
    /// <remarks>
    /// Bepu keeps mass on the collider shape, so this is applied to the shape fitted to the primitive. A compound that already holds your own shapes keeps their masses.
    /// </remarks>
    public float? Mass { get; set; }
}
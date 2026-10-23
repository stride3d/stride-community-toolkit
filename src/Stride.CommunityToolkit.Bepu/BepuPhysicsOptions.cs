using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Engine;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// The options of a primitive entity with Bepu physics, 2D or 3D: the entity options plus the body's
/// mode, component, mass and interpolation.
/// </summary>
/// <remarks>
/// <para>
/// With <see cref="Component"/> left <see langword="null"/>, <see cref="PrimitiveEntityOptions.Body"/> decides what
/// is attached: a <see cref="BodyComponent"/> (a <see cref="Body2DComponent"/> for a flat shape), the same body
/// kinematic, or a <see cref="StaticComponent"/>; <see cref="BodyMode.None"/> attaches nothing. The component gets a
/// <see cref="CompoundCollider"/> with the shape that fits the primitive, and a 2D shape's collider is as deep as the
/// shape says.
/// </para>
/// <para>
/// A supplied <see cref="Component"/> wins over the mode and is used as it is, kinematic or not, interpolated or
/// not. Give it an empty <see cref="CompoundCollider"/> to have the fitted shape added, or a compound that already
/// holds shapes to keep them: the fitted shape goes into an empty compound only. A supplied component with
/// <see cref="BodyMode.None"/> is refused, since the two contradict each other.
/// </para>
/// <para>
/// Options without a component can serve any number of entities, each getting a fresh component. Options with a
/// supplied component serve one entity: a component belongs to one entity, and the engine refuses it on a second.
/// </para>
/// </remarks>
public class BepuPhysicsOptions : PrimitiveEntityOptions
{
    /// <summary>
    /// Gets or sets the Bepu collidable component attached to the entity, or <see langword="null"/> to have one
    /// made from <see cref="PrimitiveEntityOptions.Body"/>.
    /// </summary>
    public CollidableComponent? Component { get; set; }

    /// <summary>
    /// Gets or sets the mass of the fitted collider shape, in kilograms, or <see langword="null"/> for Bepu's default of 1.
    /// </summary>
    /// <remarks>
    /// Bepu keeps mass on the collider shape, so this is applied to the shape fitted to the primitive. A compound that
    /// already holds your own shapes keeps their masses. A static collider has no mass; a value given with
    /// <see cref="BodyMode.Static"/> is ignored with a line in the log.
    /// </remarks>
    public float? Mass { get; set; }

    /// <summary>
    /// Whether a body the options create is interpolated between physics steps, so it moves smoothly at any frame
    /// rate above the physics rate. On by default; a supplied <see cref="Component"/> keeps its own setting.
    /// </summary>
    /// <remarks>
    /// The engine's default is no interpolation, which stutters whenever a frame lands between two steps. Turn it off
    /// for a body whose transform you read right after the step, as a sensor or a test does.
    /// </remarks>
    public bool Interpolation { get; set; } = true;
}
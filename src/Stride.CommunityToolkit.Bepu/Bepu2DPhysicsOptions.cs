using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Engine;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Provides options for creating a 2D-style primitive entity with Bepu physics.
/// </summary>
/// <remarks>
/// <para>Extends <see cref="Primitive2DEntityOptions"/> with a configurable Bepu <see cref="CollidableComponent"/>.</para>
/// <para>When <see cref="Component"/> is left <see langword="null"/>, the creation helper that consumes the options picks a context-appropriate default: a dynamic <see cref="Body2DComponent"/> for primitives, a <see cref="StaticComponent"/> for the ground helper.</para>
/// <para>Although this option type is for 2D-style primitives, Bepu simulation still runs in 3D space with motion constrained to the XY plane.</para>
/// </remarks>
public class Bepu2DPhysicsOptions : Primitive2DEntityOptions
{
    /// <summary>
    /// Gets or sets the Bepu collidable component attached to the entity.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/> (the default), the consuming helper chooses: primitive creation attaches a dynamic <see cref="Body2DComponent"/> with an empty <see cref="CompoundCollider"/>, while the ground helper attaches a <see cref="StaticComponent"/>. Set it explicitly to override either default.
    /// </remarks>
    public CollidableComponent? Component { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a collider shape matching the primitive type is created automatically.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="true"/>. When set to <see langword="false"/>, the <see cref="Component"/> is attached without generated collider shapes so they can be added later.
    /// </remarks>
    public bool IncludeCollider { get; set; } = true;
}
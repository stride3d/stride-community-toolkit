using Stride.Rendering;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The options of a primitive entity created with <c>CreatePrimitive</c>: its name, material, render group,
/// position, and the kind of physics the chosen engine gives it.
/// </summary>
/// <remarks>
/// The physics packages derive their own options from this one, <c>BepuPhysicsOptions</c> and
/// <c>BulletPhysicsOptions</c>, adding the engine's component and mass. A 2D shape's collider depth is on the
/// shape, not here.
/// </remarks>
public class PrimitiveEntityOptions
{
    /// <summary>
    /// Gets or sets the optional name assigned to the created entity.
    /// </summary>
    /// <remarks>
    /// If <see langword="null"/>, the creation helper may assign a name or leave the entity unnamed.
    /// </remarks>
    public string? EntityName { get; set; }

    /// <summary>
    /// Gets or sets the material to apply to the generated primitive model.
    /// </summary>
    /// <remarks>
    /// If <see langword="null"/>, the generated model uses the material behaviour defined by the creation helper or the engine.
    /// </remarks>
    public Material? Material { get; set; }

    /// <summary>
    /// Gets or sets the render group assigned to the generated model component.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="RenderGroup.Group0"/>. Render groups can be used to route entities through different rendering passes or layers.
    /// </remarks>
    public RenderGroup RenderGroup { get; set; } = RenderGroup.Group0;

    /// <summary>
    /// Gets or sets the initial world position applied to the entity's transform.
    /// </summary>
    /// <remarks>
    /// If <see langword="null"/>, the entity keeps the default transform position, <see cref="Vector3.Zero"/>.
    /// </remarks>
    public Vector3? Position { get; set; }

    /// <summary>
    /// The kind of physics the engine chosen by the scene setup attaches: a dynamic body by default, a static
    /// collider, a kinematic body, or none.
    /// </summary>
    /// <remarks>
    /// Read only when the physics options supply no component of their own; a supplied component wins, and a
    /// supplied component with <see cref="BodyMode.None"/> is refused. Without a setup, or with
    /// <see cref="PhysicsSetupBase.None"/>, nothing is attached whatever this says. The physics packages' entity
    /// extensions (<c>AddBepuPhysics</c>, <c>AddBulletPhysics</c>) read it the same way.
    /// </remarks>
    public BodyMode Body { get; set; } = BodyMode.Dynamic;

    /// <summary>
    /// A copy of the options with another <see cref="Body"/>, the rest as it is, whatever the runtime type.
    /// </summary>
    /// <param name="body">The mode of the copy.</param>
    /// <returns>A shallow copy: a supplied component, where the type has one, is the same instance.</returns>
    /// <remarks>
    /// The ground helpers use it to ask for a static collider without changing the options they were given.
    /// </remarks>
    public PrimitiveEntityOptions WithBody(BodyMode body)
    {
        var copy = (PrimitiveEntityOptions)MemberwiseClone();

        copy.Body = body;

        return copy;
    }
}
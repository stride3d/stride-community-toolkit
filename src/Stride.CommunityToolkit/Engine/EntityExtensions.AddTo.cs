using Stride.Engine;

namespace Stride.CommunityToolkit.Engine;

public static partial class EntityExtensions
{
    /// <summary>
    /// Adds the entity to <paramref name="scene"/> and returns it, so creation, placement and adding read as one line.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="scene">The scene to add it to; the same as setting <see cref="Entity.Scene"/>.</param>
    /// <returns>The entity.</returns>
    /// <example>
    /// <code>
    /// game.CreatePrimitive(Primitive3D.Cube()).AddTo(scene).Transform.Position = new Vector3(0, 8, 0);
    /// </code>
    /// </example>
    public static Entity AddTo(this Entity entity, Scene scene)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(scene);

        entity.Scene = scene;

        return entity;
    }

    /// <summary>
    /// Adds the entity as a child of <paramref name="parent"/> and returns it.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="parent">The parent; the same as <c>parent.AddChild(entity)</c>.</param>
    /// <returns>The entity.</returns>
    public static Entity AddTo(this Entity entity, Entity parent)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(parent);

        parent.AddChild(entity);

        return entity;
    }
}
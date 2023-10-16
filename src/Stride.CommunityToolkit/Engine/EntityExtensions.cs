using Stride.CommunityToolkit.Scripts;
using Stride.Engine;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides extension methods for <see cref="Entity"/> to simplify common operations.
/// </summary>
public static class EntityExtensions
{
    /// <summary>
    /// Adds a mouse-look camera controller to the specified entity, enabling camera movement and rotation through various input methods.
    /// </summary>
    /// <param name="entity">The entity to which the camera controller will be added.</param>
    /// <remarks>
    /// The camera entity can be moved using W, A, S, D, Q and E, arrow keys, a gamepad's left stick or dragging/scaling using multi-touch.
    /// Rotation is achieved using the Numpad, the mouse while holding the right mouse button, a gamepad's right stick, or dragging using single-touch.
    /// </remarks>
    public static void AddMouseLookCamera(this Entity entity)
    {
        entity.Add(new BasicCameraController());
    }

    /// <summary>
    /// Retrieves the first component of the specified type from the entity.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <returns>The first component of the specified type, or null if no such component exists.</returns>
    public static T? GetComponent<T>(this Entity entity)
    {
        var result = entity.OfType<T>().FirstOrDefault();

        return result;
    }

    /// <summary>
    /// Retrieves all components of the specified type from the entity.
    /// </summary>
    /// <typeparam name="T">The type of components to retrieve.</typeparam>
    /// <returns>An IEnumerable of components of the specified type.</returns>
    public static IEnumerable<T> GetComponents<T>(this Entity entity)
    {
        var result = entity.OfType<T>();

        return result;
    }

    /// <summary>
    /// Attempts to remove the entity from its scene, destroying it.
    /// </summary>
    /// <returns>True if the entity was successfully removed; otherwise, false.</returns>
    public static bool DestroyEntity(this Entity entity)
    {
        try
        {
            entity.Scene.Entities.Remove(entity);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// An easier way to get world position rather than getting <see cref="Matrix.TranslationVector"/> from <see cref="TransformComponent.WorldMatrix"/>
    /// </summary>
    /// <param name="entity">The <see cref="Entity"/> to get the World Position</param>
    /// <returns>The <see cref="Vector3"/> as the World Position of the <see cref="Entity"/></returns>
    public static Vector3 WorldPosition(this Entity entity)
    {
        return entity.Transform.WorldMatrix.TranslationVector;
    }
}
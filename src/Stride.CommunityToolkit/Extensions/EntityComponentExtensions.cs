namespace Stride.Engine;

public static class EntityComponentExtensions
{
    /// <summary>
    /// Definitely not a rip off of Unity's GetComponent.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <returns></returns>
    public static T GetComponent<T>(this Entity entity)
    {
        var result = entity.OfType<T>().FirstOrDefault();

        return result;
    }

    /// <summary>
    /// Gets all components of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <returns></returns>
    public static IEnumerable<T> GetComponents<T>(this Entity entity)
    {
        var result = entity.OfType<T>();

        return result;
    }

    /// <summary>
    /// Destroys the entity that calls this method.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
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
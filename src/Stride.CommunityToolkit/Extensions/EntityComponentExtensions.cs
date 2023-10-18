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
    [Obsolete("Use Remove instead")]
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
    /// Sets the entity scene to null to remove the entity.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public static void Remove(this Entity entity)
    {
        entity.Scene = null;
    }

    /// <summary>
    /// An easier way to get the previous frames world position rather than getting <see cref="Matrix.TranslationVector"/> from <see cref="TransformComponent.WorldMatrix"/>
    /// </summary>
    /// <param name="entity">The <see cref="Entity"/> to get the World Position</param>
    /// <param name="updateTransforms"> If true it will get the current frames world matrix</param>
    /// <returns>The <see cref="Vector3"/> as the World Position of the <see cref="Entity"/></returns>
    public static Vector3 WorldPosition(this Entity entity, bool updateTransforms = false)
    {
        if(updateTransforms)
        {
            entity.Transform.UpdateWorldMatrix();
        }
        return entity.Transform.WorldMatrix.TranslationVector;
    }
}
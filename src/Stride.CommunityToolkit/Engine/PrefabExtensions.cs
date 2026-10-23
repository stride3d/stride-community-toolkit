using Stride.CommunityToolkit.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Extension methods for <see cref="Prefab"/>.
/// </summary>
public static class PrefabExtensions
{
    /// <summary>
    /// Instantiates a <see cref="Prefab"/> that contains a single <see cref="Entity"/>.
    /// </summary>
    /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
    /// <returns>The instantiated and translated <see cref="Entity"/>.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">If <paramref name="prefab"/> has more than one <see cref="Entity"/>.</exception>
    public static Entity InstantiateSingle(this Prefab prefab)
    {
        ValidateSinglePrefab(prefab);

        return prefab.Instantiate()[0];
    }

    /// <summary>
    /// Instantiates a <see cref="Prefab"/> that contains a single <see cref="Entity"/> and applies a transform.
    /// </summary>
    /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
    /// <param name="translation">The <see cref="Vector3"/> to translate the entity by.</param>
    /// <param name="rotation">The <see cref="Quaternion"/> to rotate the entity by.</param>
    /// <param name="scale">The <see cref="Vector3"/> to scale the entity by.</param>
    /// <returns>The instantiated and translated <see cref="Entity"/>.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">If <paramref name="prefab"/> has more than one <see cref="Entity"/>.</exception>
    public static Entity InstantiateSingle(this Prefab prefab, ref Vector3 translation, ref Quaternion rotation, ref Vector3 scale)
    {
        ValidateSinglePrefab(prefab);

        return prefab.Instantiate(ref translation, ref rotation, ref scale)[0];
    }

    /// <inheritdoc cref="InstantiateSingle(Prefab, ref Vector3, ref Quaternion, ref Vector3)"/>
    public static Entity InstantiateSingle(this Prefab prefab, Vector3 translation, Quaternion rotation, Vector3 scale)
        => prefab.InstantiateSingle(ref translation, ref rotation, ref scale);

    /// <summary>
    /// Instantiates a <see cref="Prefab"/> that contains a single <see cref="Entity"/> and applies a transform.
    /// </summary>
    /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
    /// <param name="translation">The <see cref="Vector3"/> to translate the entity by.</param>
    /// <param name="rotationEulerAngles">The X, Y and Z rotations in radians to rotate the entity by.</param>
    /// <param name="scale">The <see cref="Vector3"/> to scale the entity by.</param>
    /// <returns>The instantiated and translated <see cref="Entity"/>.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">If <paramref name="prefab"/> has more than one <see cref="Entity"/>.</exception>
    public static Entity InstantiateSingle(this Prefab prefab, ref Vector3 translation, ref Vector3 rotationEulerAngles, ref Vector3 scale)
    {
        MathUtilEx.ToQuaternion(ref rotationEulerAngles, out var rotation);

        return prefab.InstantiateSingle(ref translation, ref rotation, ref scale);
    }

    /// <inheritdoc cref="InstantiateSingle(Prefab, ref Vector3, ref Vector3, ref Vector3)"/>
    public static Entity InstantiateSingle(this Prefab prefab, Vector3 translation, Vector3 rotationEulerAngles, Vector3 scale)
        => prefab.InstantiateSingle(ref translation, ref rotationEulerAngles, ref scale);

    /// <inheritdoc cref="InstantiateSingle(Prefab, ref Vector3, ref Quaternion, ref Vector3)"/>
    public static Entity InstantiateSingle(this Prefab prefab, ref Vector3 translation, ref Quaternion rotation)
    {
        var one = Vector3.One;

        return prefab.InstantiateSingle(ref translation, ref rotation, ref one);
    }

    /// <inheritdoc cref="InstantiateSingle(Prefab, ref Vector3, ref Quaternion, ref Vector3)"/>
    public static Entity InstantiateSingle(this Prefab prefab, Vector3 translation, Quaternion rotation)
        => prefab.InstantiateSingle(ref translation, ref rotation);

    /// <inheritdoc cref="InstantiateSingle(Prefab, ref Vector3, ref Vector3, ref Vector3)"/>
    public static Entity InstantiateSingle(this Prefab prefab, ref Vector3 translation, ref Vector3 rotationEulerAngles)
    {
        var one = Vector3.One;

        return prefab.InstantiateSingle(ref translation, ref rotationEulerAngles, ref one);
    }

    /// <inheritdoc cref="InstantiateSingle(Prefab, ref Vector3, ref Vector3, ref Vector3)"/>
    public static Entity InstantiateSingle(this Prefab prefab, Vector3 translation, Vector3 rotationEulerAngles)
        => prefab.InstantiateSingle(ref translation, ref rotationEulerAngles);

    /// <inheritdoc cref="InstantiateSingle(Prefab, ref Vector3, ref Quaternion, ref Vector3)"/>
    public static Entity InstantiateSingle(this Prefab prefab, ref Vector3 translation)
    {
        var rotation = Quaternion.Identity;

        return prefab.InstantiateSingle(ref translation, ref rotation);
    }

    /// <inheritdoc cref="InstantiateSingle(Prefab, ref Vector3, ref Quaternion, ref Vector3)"/>
    public static Entity InstantiateSingle(this Prefab prefab, Vector3 translation)
        => prefab.InstantiateSingle(ref translation);

    /// <summary>
    /// Instantiates a <see cref="Prefab"/> and applies a transform to all its entities.
    /// </summary>
    /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
    /// <param name="translation">The <see cref="Vector3"/> to translate the entities by.</param>
    /// <param name="rotation">The <see cref="Quaternion"/> to rotate the entities by.</param>
    /// <param name="scale">The <see cref="Vector3"/> to scale the entities by.</param>
    /// <returns>The instantiated and translated entities.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
    public static List<Entity> Instantiate(this Prefab prefab, ref Vector3 translation, ref Quaternion rotation, ref Vector3 scale)
    {
        ArgumentNullException.ThrowIfNull(prefab);

        Matrix.Transformation(ref scale, ref rotation, ref translation, out var localMatrix);

        var instances = prefab.Instantiate();

        foreach (var instance in instances)
        {
            instance.Transform.UpdateLocalMatrix();

            var entityMatrix = instance.Transform.LocalMatrix * localMatrix;

            if (instance.Transform.UseTRS)
                entityMatrix.Decompose(out instance.Transform.Scale, out instance.Transform.Rotation, out instance.Transform.Position);
            else
                instance.Transform.LocalMatrix = entityMatrix;
        }

        return instances;
    }

    /// <inheritdoc cref="Instantiate(Prefab, ref Vector3, ref Quaternion, ref Vector3)"/>
    public static List<Entity> Instantiate(this Prefab prefab, Vector3 translation, Quaternion rotation, Vector3 scale)
        => prefab.Instantiate(ref translation, ref rotation, ref scale);

    /// <summary>
    /// Instantiates a <see cref="Prefab"/> and applies a transform to all its entities.
    /// </summary>
    /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
    /// <param name="translation">The <see cref="Vector3"/> to translate the entities by.</param>
    /// <param name="rotationEulerAngles">The X, Y and Z rotations, in radians, to rotate the entities by.</param>
    /// <param name="scale">The <see cref="Vector3"/> to scale the entities by.</param>
    /// <returns>The instantiated and translated entities.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
    public static List<Entity> Instantiate(this Prefab prefab, ref Vector3 translation, ref Vector3 rotationEulerAngles, ref Vector3 scale)
    {
        MathUtilEx.ToQuaternion(ref rotationEulerAngles, out var rotation);

        return prefab.Instantiate(ref translation, ref rotation, ref scale);
    }

    /// <inheritdoc cref="Instantiate(Prefab, ref Vector3, ref Vector3, ref Vector3)"/>
    public static List<Entity> Instantiate(this Prefab prefab, Vector3 translation, Vector3 rotationEulerAngles, Vector3 scale)
        => prefab.Instantiate(ref translation, ref rotationEulerAngles, ref scale);

    /// <inheritdoc cref="Instantiate(Prefab, ref Vector3, ref Quaternion, ref Vector3)"/>
    public static List<Entity> Instantiate(this Prefab prefab, ref Vector3 translation, ref Quaternion rotation)
    {
        var one = Vector3.One;

        return prefab.Instantiate(ref translation, ref rotation, ref one);
    }

    /// <inheritdoc cref="Instantiate(Prefab, ref Vector3, ref Quaternion, ref Vector3)"/>
    public static List<Entity> Instantiate(this Prefab prefab, Vector3 translation, Quaternion rotation)
        => prefab.Instantiate(ref translation, ref rotation);

    /// <inheritdoc cref="Instantiate(Prefab, ref Vector3, ref Vector3, ref Vector3)"/>
    public static List<Entity> Instantiate(this Prefab prefab, ref Vector3 translation, ref Vector3 rotationEulerAngles)
    {
        var one = Vector3.One;

        return prefab.Instantiate(ref translation, ref rotationEulerAngles, ref one);
    }

    /// <inheritdoc cref="Instantiate(Prefab, ref Vector3, ref Vector3, ref Vector3)"/>
    public static List<Entity> Instantiate(this Prefab prefab, Vector3 translation, Vector3 rotationEulerAngles)
        => prefab.Instantiate(ref translation, ref rotationEulerAngles);

    /// <inheritdoc cref="Instantiate(Prefab, ref Vector3, ref Quaternion, ref Vector3)"/>
    public static List<Entity> Instantiate(this Prefab prefab, ref Vector3 translation)
    {
        var rotation = Quaternion.Identity;

        return prefab.Instantiate(ref translation, ref rotation);
    }

    /// <inheritdoc cref="Instantiate(Prefab, ref Vector3, ref Quaternion, ref Vector3)"/>
    public static List<Entity> Instantiate(this Prefab prefab, Vector3 translation)
        => prefab.Instantiate(ref translation);

    private static void ValidateSinglePrefab(Prefab prefab)
    {
        ArgumentNullException.ThrowIfNull(prefab);

        if (prefab.Entities.Count > 1)
            throw new InvalidOperationException("Prefab contains more than one entity.");
    }
}

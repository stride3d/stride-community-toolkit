namespace Stride.CommunityToolkit.Mathematics;

/// <summary>
/// Rotations that <see cref="Quaternion"/> does not build directly.
/// </summary>
public static class QuaternionUtil
{
    /// <summary>
    /// The rotation of something at <paramref name="eye"/> that looks at <paramref name="target"/>, the way a camera
    /// does: down its own -Z, with <paramref name="up"/> kept upwards so it does not roll. A chase camera, a turret,
    /// a fixed camera aimed at a point.
    /// </summary>
    /// <param name="eye">Where the looker is.</param>
    /// <param name="target">What it looks at.</param>
    /// <param name="up">Which way is up; <see langword="null"/>, the default, is <see cref="Vector3.UnitY"/>.</param>
    /// <returns>The rotation, for a transform's <c>Rotation</c>.</returns>
    /// <remarks>
    /// Undefined when the two points coincide or the line between them runs along <paramref name="up"/>, as any
    /// look-at is. The same construction as <c>Matrix.LookAtRH</c>, inverted: a view matrix looks down -Z, so its
    /// inverse is the pose of the thing that looks.
    /// </remarks>
    public static Quaternion LookAt(Vector3 eye, Vector3 target, Vector3? up = null)
    {
        var view = Matrix.LookAtRH(eye, target, up ?? Vector3.UnitY);

        Matrix.Invert(ref view, out var world);

        return Quaternion.RotationMatrix(world);
    }
}
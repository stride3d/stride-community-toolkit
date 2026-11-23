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
    /// <para>
    /// The result is always finite and unit length, including for the degenerate inputs: an eye on the target gives
    /// <see cref="Quaternion.Identity"/>, since no rotation is more right than any other; a zero <paramref name="up"/>
    /// is taken as <see cref="Vector3.UnitY"/>; a line of sight along <paramref name="up"/> takes an arbitrary roll.
    /// </para>
    /// <para>
    /// Stride's own <see cref="Quaternion.LookRotation"/> takes a direction rather than two points: the local +Z axis,
    /// which for a camera points from the target back to the eye, and it must be unit length. This is the
    /// eye-and-target form with the guards; the basis it builds is Stride's.
    /// </para>
    /// </remarks>
    public static Quaternion LookAt(Vector3 eye, Vector3 target, Vector3? up = null)
    {
        var upwards = up ?? Vector3.UnitY;

        LookAt(in eye, in target, in upwards, out var result);

        return result;
    }

    /// <summary>
    /// The rotation of something at <paramref name="eye"/> that looks at <paramref name="target"/>, by reference, for
    /// a caller that already holds the vectors; <see cref="LookAt(Vector3, Vector3, Vector3?)"/> says what it builds.
    /// </summary>
    /// <param name="eye">Where the looker is.</param>
    /// <param name="target">What it looks at.</param>
    /// <param name="up">Which way is up; a zero vector is taken as <see cref="Vector3.UnitY"/>.</param>
    /// <param name="result">The rotation, for a transform's <c>Rotation</c>; always finite and unit length.</param>
    public static void LookAt(ref readonly Vector3 eye, ref readonly Vector3 target, ref readonly Vector3 up, out Quaternion result)
    {
        // Stride is right-handed and a camera looks down -Z, so the basis is built around the axis pointing from the
        // target back towards the eye rather than along the line of sight
        var forward = eye - target;

        // Nothing to face: the observer is standing on the target, so no rotation is more correct than any other and
        // the caller's existing orientation is worth more than a guess
        if (forward.Length() < MathUtil.ZeroTolerance)
        {
            result = Quaternion.Identity;

            return;
        }

        forward.Normalize();

        var upwards = up.Length() < MathUtil.ZeroTolerance ? Vector3.UnitY : up;

        // Looking straight along the up vector leaves nothing to say which way is sideways. Any axis not parallel to
        // forward restores that, and the choice only sets the roll, which is arbitrary in this case anyway.
        if (Vector3.Cross(upwards, forward).Length() < MathUtil.ZeroTolerance)
            upwards = MathF.Abs(forward.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX;

        // Stride's basis: right from up and forward, up re-derived from the two, so the three axes are exactly
        // orthogonal whatever was passed in and the quaternion comes out unit length
        result = Quaternion.LookRotation(forward, upwards);
    }
}
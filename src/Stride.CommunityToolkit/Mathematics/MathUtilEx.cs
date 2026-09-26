using System.Runtime.CompilerServices;

namespace Stride.CommunityToolkit.Mathematics;

/// <summary>
/// Some more common utility methods for math operations.
/// </summary>
public static class MathUtilEx
{
    /// <summary>
    /// Gets the smallest integer greater than or equal to the amount.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The smallest integer greater than or equal to the amount.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CeilingToInt(this float value) => (int)Math.Ceiling(value);

    /// <summary>
    /// Gets largest integer less than or equal to the amount.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The largest integer less than or equal to the amount.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FloorToInt(this float value) => (int)Math.Floor(value);

    /// <summary>
    /// Gets the integer value nearest to the amount.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The integer value nearest to the amount.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int RoundToInt(this float value) => (int)Math.Round(value);

    /// <summary>
    /// Clamps the value between 0 and 1.
    /// </summary>
    /// <param name="value">The Value.</param>
    /// <returns>Value clamped between 0 and 1.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp01(float value) => MathUtil.Clamp(value, 0, 1);

    /// <summary>
    /// Orthonormalizes 2 vectors.
    /// </summary>
    /// <param name="normal">The normal vector.</param>
    /// <param name="tangent">The tangent vector.</param>
    /// <remarks>
    /// <para>Makes vectors normalized and orthogonal to each other.
    /// Normalizes normal. Normalizes tangent and makes sure it is orthogonal to normal.</para>
    /// </remarks>
    public static void Orthonormalize(ref Vector3 normal, ref Vector3 tangent)
    {
        //Uses the modified Gram-Schmidt process.
        //Because we are making unit vectors, we can optimize the math for orthogonalization
        //and simplify the projection operation to remove the division.
        //q1 = m1 / |m1|
        //q2 = (m2 - (q1 ⋅ m2) * q1) / |m2 - (q1 ⋅ m2) * q1|

        normal.Normalize();
        tangent -= Vector3.Dot(normal, tangent) * normal;
        tangent.Normalize();
    }

    /// <summary>
    /// Creates a rotation with the specified forward and upwards directions.
    /// </summary>
    /// <param name="eye">The postion of the observer. i.e. camera</param>
    /// <param name="target">The location of the object to look-at.</param>
    /// <param name="up">The vector that defines which direction is up.</param>
    /// <param name="result">The created quaternion rotation</param>
    /// <remarks>
    /// The result is always finite and unit length, including for the degenerate inputs: an eye
    /// sitting on the target, a line of sight parallel to <paramref name="up"/>, and a zero-length
    /// <paramref name="up"/>.
    /// <para>
    /// This used to build the quaternion with the single-branch trace formula,
    /// <c>w = sqrt(1 + m11 + m22 + m33) / 2</c>, which is only valid while that sum is positive. A
    /// camera orbited to the far side of its target is a 180 degree rotation, where the sum is
    /// exactly -1: <c>w</c> came out as zero, the reciprocal that follows it divided by zero, and the
    /// quaternion's components became <c>0 * infinity</c>, which is <c>NaN</c>. Assigning that to a
    /// <c>TransformComponent.Rotation</c> poisoned the entity's matrix, and any position integrated
    /// through that matrix afterwards became <c>NaN</c> as well - a camera that could not be
    /// recovered without resetting its transform. Delegating to
    /// <see cref="Quaternion.RotationMatrix(Matrix)"/> picks up the branch for each sign of the
    /// trace, which is what makes every orientation safe rather than merely most of them.
    /// </para>
    /// </remarks>
    public static void LookRotation(ref Vector3 eye, ref Vector3 target, ref Vector3 up, out Quaternion result)
    {
        // Stride is right-handed, so the basis is built around the axis pointing from the target back
        // towards the eye rather than along the line of sight
        var forward = eye - target;

        // Nothing to face: the observer is standing on the target, so no rotation is more correct
        // than any other and the caller's existing orientation is worth more than a guess
        if (forward.Length() < MathUtil.ZeroTolerance)
        {
            result = Quaternion.Identity;

            return;
        }

        forward.Normalize();

        var upwards = up;

        if (upwards.Length() < MathUtil.ZeroTolerance)
        {
            upwards = Vector3.UnitY;
        }

        Vector3.Cross(ref upwards, ref forward, out var right);

        // Looking straight along the up vector leaves nothing to say which way is sideways. Any axis
        // not parallel to forward restores that, and the choice only sets the roll, which is
        // arbitrary in this case anyway.
        if (right.Length() < MathUtil.ZeroTolerance)
        {
            var reference = MathF.Abs(forward.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX;

            Vector3.Cross(ref reference, ref forward, out right);
        }

        right.Normalize();

        // Re-derived rather than reusing the caller's up, so the three axes are exactly orthogonal
        // whatever was passed in. A skewed basis is what makes the conversion below produce a
        // quaternion that is finite but not unit length.
        Vector3.Cross(ref forward, ref right, out upwards);

        upwards.Normalize();

        var orientation = new Matrix
        {
            M11 = right.X,
            M12 = right.Y,
            M13 = right.Z,
            M21 = upwards.X,
            M22 = upwards.Y,
            M23 = upwards.Z,
            M31 = forward.X,
            M32 = forward.Y,
            M33 = forward.Z,
            M44 = 1f,
        };

        Quaternion.RotationMatrix(ref orientation, out result);
    }

    /// <summary>
    /// Creates a rotation with the specified forward and upwards directions.
    /// </summary>
    /// <param name="eye">The postion of the observer. i.e. camera</param>
    /// <param name="target">The location of the object to look-at.</param>
    /// <param name="up">The vector that defines which direction is up.</param>
    /// <returns>The created quaternion rotation</returns>
    /// <example>
    /// var cameraRotation = Quaternion.LookRotation(cameraPosition, targetPosition, Vector3.UnitY);
    /// </example>
    public static Quaternion LookRotation(Vector3 eye, Vector3 target, Vector3 up)
    {
        LookRotation(ref eye, ref target, ref up, out var result);
        return result;
    }

    /// <summary>
    /// Convert rotation Euler angles to a <see cref="Quaternion"/>.
    /// </summary>
    /// <param name="rotationEulerXYZ">The euler rotation, with XYZ order.</param>
    /// <param name="result">Resulting quaternion rotation</param>
    public static void ToQuaternion(ref Vector3 rotationEulerXYZ, out Quaternion result)
    {
        // Equilvalent to:
        //  Quaternion quatX, quatY, quatZ;
        //
        //  Quaternion.RotationX(value.X, out quatX);
        //  Quaternion.RotationY(value.Y, out quatY);
        //  Quaternion.RotationZ(value.Z, out quatZ);
        //
        //  rotation = quatX * quatY * quatZ;

        var halfAngles = rotationEulerXYZ * 0.5f;
        var fSinX = (float)Math.Sin(halfAngles.X);
        var fCosX = (float)Math.Cos(halfAngles.X);
        var fSinY = (float)Math.Sin(halfAngles.Y);
        var fCosY = (float)Math.Cos(halfAngles.Y);
        var fSinZ = (float)Math.Sin(halfAngles.Z);
        var fCosZ = (float)Math.Cos(halfAngles.Z);
        var fCosXY = fCosX * fCosY;
        var fSinXY = fSinX * fSinY;

        result.X = fSinX * fCosY * fCosZ - fSinZ * fSinY * fCosX;
        result.Y = fSinY * fCosX * fCosZ + fSinZ * fSinX * fCosY;
        result.Z = fSinZ * fCosXY - fSinXY * fCosZ;
        result.W = fCosZ * fCosXY + fSinXY * fSinZ;

    }

    /// <summary>
    /// Convert rotation Euler angles to a <see cref="Quaternion"/>.
    /// </summary>
    /// <param name="rotationEulerXYZ">The euler rotation, with XYZ order.</param>
    /// <returns>Resulting quaternion rotation</returns>
    public static Quaternion ToQuaternion(this Vector3 rotationEulerXYZ)
    {
        ToQuaternion(ref rotationEulerXYZ, out var result);
        return result;
    }

    /// <summary>
    /// Convert <see cref="Quaternion"/> to rotation Euler angles.
    /// </summary>
    /// <param name="rotation">The rotation.</param>
    /// <param name="result">Reulting euler rotation, with XYZ order.</param>
    public static void ToRotationEulerXYZ(ref Quaternion rotation, out Vector3 result)
    {

        // Equivalent to:
        //  Matrix rotationMatrix;
        //  Matrix.Rotation(ref cachedRotation, out rotationMatrix);
        //  rotationMatrix.DecomposeXYZ(out rotationEuler);
        float xx = rotation.X * rotation.X;
        float yy = rotation.Y * rotation.Y;
        float zz = rotation.Z * rotation.Z;
        float xy = rotation.X * rotation.Y;
        float zw = rotation.Z * rotation.W;
        float zx = rotation.Z * rotation.X;
        float yw = rotation.Y * rotation.W;
        float yz = rotation.Y * rotation.Z;
        float xw = rotation.X * rotation.W;
        result.Y = (float)Math.Asin(2.0f * (yw - zx));
        double test = Math.Cos(result.Y);
        if (test > 1e-6f)
        {
            result.Z = (float)Math.Atan2(2.0f * (xy + zw), 1.0f - (2.0f * (yy + zz)));
            result.X = (float)Math.Atan2(2.0f * (yz + xw), 1.0f - (2.0f * (yy + xx)));
        }
        else
        {
            result.Z = (float)Math.Atan2(2.0f * (zw - xy), 2.0f * (zx + yw));
            result.X = 0.0f;
        }

    }

    /// <summary>
    /// Convert <see cref="Quaternion"/> to rotation Euler angles.
    /// </summary>
    /// <param name="rotation">The rotation.</param>
    /// <returns>Reulting euler rotation, with XYZ order.</returns>
    public static Vector3 ToRotationEulerXYZ(this Quaternion rotation)
    {
        ToRotationEulerXYZ(ref rotation, out var result);
        return result;
    }

    /// <summary>
    /// Performs an interpolation between two values using an easing function.
    /// </summary>
    /// <param name="start">Start value.</param>
    /// <param name="end">End value.</param>
    /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>; values outside that range are clamped to it.</param>
    /// <param name="easingFunction">The function used to ease the interpolation.</param>
    /// <remarks>
    /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Interpolate(float start, float end, float amount, EasingFunction easingFunction)
    {
        return MathUtil.Lerp(start, end, Easing.Ease(amount, easingFunction));
    }

    /// <summary>
    /// Performs an interpolation between two vectors using an easing function.
    /// </summary>
    /// <param name="start">Start vector.</param>
    /// <param name="end">End vector.</param>
    /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>; values outside that range are clamped to it.</param>
    /// <param name="easingFunction">The function used to ease the interpolation.</param>
    /// <param name="result">When the method completes, contains the interpolation of the two vectors.</param>
    /// <remarks>
    /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Interpolate(ref Vector2 start, ref Vector2 end, float amount, EasingFunction easingFunction, out Vector2 result)
    {
        Vector2.Lerp(ref start, ref end, Easing.Ease(amount, easingFunction), out result);
    }

    /// <summary>
    /// Performs an interpolation between two vectors using an easing function.
    /// </summary>
    /// <param name="start">Start vector.</param>
    /// <param name="end">End vector.</param>
    /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>; values outside that range are clamped to it.</param>
    /// <param name="easingFunction">The function used to ease the interpolation.</param>
    /// <returns>The interpolation of the two vectors.</returns>
    /// <remarks>
    /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Interpolate(Vector2 start, Vector2 end, float amount, EasingFunction easingFunction)
    {
        return Vector2.Lerp(start, end, Easing.Ease(amount, easingFunction));
    }

    /// <summary>
    /// Performs an interpolation between two vectors using an easing function.
    /// </summary>
    /// <param name="start">Start vector.</param>
    /// <param name="end">End vector.</param>
    /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>; values outside that range are clamped to it.</param>
    /// <param name="easingFunction">The function used to ease the interpolation.</param>
    /// <param name="result">When the method completes, contains the interpolation of the two vectors.</param>
    /// <remarks>
    /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Interpolate(ref Vector3 start, ref Vector3 end, float amount, EasingFunction easingFunction, out Vector3 result)
    {
        Vector3.Lerp(ref start, ref end, Easing.Ease(amount, easingFunction), out result);
    }

    /// <summary>
    /// Performs an interpolation between two vectors using an easing function.
    /// </summary>
    /// <param name="start">Start vector.</param>
    /// <param name="end">End vector.</param>
    /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>; values outside that range are clamped to it.</param>
    /// <param name="easingFunction">The function used to ease the interpolation.</param>
    /// <returns>The interpolation of the two vectors.</returns>
    /// <remarks>
    /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned.
    /// </remarks>

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Interpolate(Vector3 start, Vector3 end, float amount, EasingFunction easingFunction)
    {
        return Vector3.Lerp(start, end, Easing.Ease(amount, easingFunction));
    }
    /// <summary>
    /// Performs an interpolation between two vectors using an easing function.
    /// </summary>
    /// <param name="start">Start vector.</param>
    /// <param name="end">End vector.</param>
    /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>; values outside that range are clamped to it.</param>
    /// <param name="easingFunction">The function used to ease the interpolation.</param>
    /// <param name="result">When the method completes, contains the interpolation of the two vectors.</param>
    /// <remarks>
    /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Interpolate(ref Vector4 start, ref Vector4 end, float amount, EasingFunction easingFunction, out Vector4 result)
    {
        Vector4.Lerp(ref start, ref end, Easing.Ease(amount, easingFunction), out result);
    }

    /// <summary>
    /// Performs an interpolation between two vectors using an easing function.
    /// </summary>
    /// <param name="start">Start vector.</param>
    /// <param name="end">End vector.</param>
    /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>; values outside that range are clamped to it.</param>
    /// <param name="easingFunction">The function used to ease the interpolation.</param>
    /// <returns>The interpolation of the two vectors.</returns>
    /// <remarks>
    /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Interpolate(Vector4 start, Vector4 end, float amount, EasingFunction easingFunction)
    {
        return Vector4.Lerp(start, end, Easing.Ease(amount, easingFunction));
    }

    /// <summary>
    /// Performs an interpolation between two colors using an easing function.
    /// </summary>
    /// <param name="start">Start color.</param>
    /// <param name="end">End color.</param>
    /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>; values outside that range are clamped to it.</param>
    /// <param name="easingFunction">The function used to ease the interpolation.</param>
    /// <param name="result">When the method completes, contains the interpolation of the two colors.</param>
    /// <remarks>
    /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Interpolate(ref Color start, ref Color end, float amount, EasingFunction easingFunction, out Color result)
    {
        Color.Lerp(ref start, ref end, Easing.Ease(amount, easingFunction), out result);
    }

    /// <summary>
    /// Performs an interpolation between two colors using an easing function.
    /// </summary>
    /// <param name="start">Start color.</param>
    /// <param name="end">End color.</param>
    /// <param name="easingFunction">The function used to ease the interpolation.</param>
    /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>; values outside that range are clamped to it.</param>
    /// <returns>The interpolation of the two colors.</returns>
    /// <remarks>
    /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color Interpolate(Color start, Color end, float amount, EasingFunction easingFunction)
    {
        return Color.Lerp(start, end, Easing.Ease(amount, easingFunction));
    }
}
using System.Runtime.CompilerServices;

namespace Stride.CommunityToolkit.Mathematics;

/// <summary>
/// Integer rounding, <c>MoveTowards</c>, Euler conversions and eased <c>Interpolate</c> overloads, beside Stride's
/// <see cref="MathUtil"/>.
/// </summary>
public static class MathUtilEx
{
    /// <summary>
    /// Gets the smallest integer greater than or equal to the amount.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The smallest integer greater than or equal to the amount.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CeilingToInt(this float value) => (int)MathF.Ceiling(value);

    /// <summary>
    /// Gets largest integer less than or equal to the amount.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The largest integer less than or equal to the amount.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FloorToInt(this float value) => (int)MathF.Floor(value);

    /// <summary>
    /// Gets the integer value nearest to the amount. A value exactly halfway between two integers rounds to the even
    /// one, as <see cref="MathF.Round(float)"/> does (<see cref="MidpointRounding.ToEven"/>): 2.5 gives 2 and 3.5 gives 4.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The integer value nearest to the amount, halves to even.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int RoundToInt(this float value) => (int)MathF.Round(value);

    /// <summary>
    /// Clamps the value between 0 and 1.
    /// </summary>
    /// <param name="value">The Value.</param>
    /// <returns>Value clamped between 0 and 1.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp01(float value) => MathUtil.Clamp(value, 0, 1);

    /// <summary>
    /// Moves <paramref name="current"/> towards <paramref name="target"/> by at most <paramref name="maxDelta"/>,
    /// landing on the target exactly rather than overshooting it.
    /// </summary>
    /// <param name="current">The value now.</param>
    /// <param name="target">The value to reach.</param>
    /// <param name="maxDelta">The furthest the value may move in this call; a negative value moves it away from the target.</param>
    /// <returns>The target when it is within reach, otherwise <paramref name="current"/> moved by <paramref name="maxDelta"/> towards it.</returns>
    /// <remarks>
    /// A frame-rate independent approach is <c>MoveTowards(value, target, speed * deltaTime)</c>: the value arrives at a
    /// constant speed and stops, where a lerp towards the target slows down and never quite arrives.
    /// </remarks>
    public static float MoveTowards(float current, float target, float maxDelta)
    {
        var delta = target - current;

        return MathF.Abs(delta) <= maxDelta ? target : current + MathF.Sign(delta) * maxDelta;
    }

    /// <summary>
    /// Moves <paramref name="current"/> towards <paramref name="target"/> along the line between them by at most
    /// <paramref name="maxDelta"/>, landing on the target exactly rather than overshooting it.
    /// </summary>
    /// <param name="current">The position now.</param>
    /// <param name="target">The position to reach.</param>
    /// <param name="maxDelta">The furthest the position may move in this call; a negative value moves it away from the target.</param>
    /// <returns>The target when it is within reach, otherwise <paramref name="current"/> moved by <paramref name="maxDelta"/> towards it.</returns>
    public static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDelta)
    {
        var delta = target - current;
        var distance = delta.Length();

        return distance <= maxDelta || distance == 0f ? target : current + delta * (maxDelta / distance);
    }

    /// <summary>
    /// Moves <paramref name="current"/> towards <paramref name="target"/> along the line between them by at most
    /// <paramref name="maxDelta"/>, landing on the target exactly rather than overshooting it.
    /// </summary>
    /// <param name="current">The position now.</param>
    /// <param name="target">The position to reach.</param>
    /// <param name="maxDelta">The furthest the position may move in this call; a negative value moves it away from the target.</param>
    /// <returns>The target when it is within reach, otherwise <paramref name="current"/> moved by <paramref name="maxDelta"/> towards it.</returns>
    public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDelta)
    {
        var delta = target - current;
        var distance = delta.Length();

        return distance <= maxDelta || distance == 0f ? target : current + delta * (maxDelta / distance);
    }

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
    /// Convert rotation Euler angles to a <see cref="Quaternion"/>.
    /// </summary>
    /// <param name="rotationEulerXYZ">The euler rotation, with XYZ order.</param>
    /// <param name="result">Resulting quaternion rotation</param>
    public static void ToQuaternion(ref Vector3 rotationEulerXYZ, out Quaternion result)
    {
        // Equivalent to:
        //  Quaternion quatX, quatY, quatZ;
        //
        //  Quaternion.RotationX(value.X, out quatX);
        //  Quaternion.RotationY(value.Y, out quatY);
        //  Quaternion.RotationZ(value.Z, out quatZ);
        //
        //  rotation = quatX * quatY * quatZ;

        var halfAngles = rotationEulerXYZ * 0.5f;
        var fSinX = MathF.Sin(halfAngles.X);
        var fCosX = MathF.Cos(halfAngles.X);
        var fSinY = MathF.Sin(halfAngles.Y);
        var fCosY = MathF.Cos(halfAngles.Y);
        var fSinZ = MathF.Sin(halfAngles.Z);
        var fCosZ = MathF.Cos(halfAngles.Z);
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
    /// <param name="result">The Euler rotation, in XYZ order.</param>
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
        result.Y = MathF.Asin(2.0f * (yw - zx));
        float test = MathF.Cos(result.Y);
        if (!MathUtil.IsZero(test))
        {
            result.Z = MathF.Atan2(2.0f * (xy + zw), 1.0f - (2.0f * (yy + zz)));
            result.X = MathF.Atan2(2.0f * (yz + xw), 1.0f - (2.0f * (yy + xx)));
        }
        else
        {
            result.Z = MathF.Atan2(2.0f * (zw - xy), 2.0f * (zx + yw));
            result.X = 0.0f;
        }
    }

    /// <summary>
    /// Convert <see cref="Quaternion"/> to rotation Euler angles.
    /// </summary>
    /// <param name="rotation">The rotation.</param>
    /// <returns>The Euler rotation, in XYZ order.</returns>
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
        => MathUtil.Lerp(start, end, Easing.Ease(amount, easingFunction));

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
        => Vector2.Lerp(ref start, ref end, Easing.Ease(amount, easingFunction), out result);

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
        => Vector2.Lerp(start, end, Easing.Ease(amount, easingFunction));

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
        => Vector3.Lerp(ref start, ref end, Easing.Ease(amount, easingFunction), out result);

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
        => Vector3.Lerp(start, end, Easing.Ease(amount, easingFunction));

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
        => Vector4.Lerp(ref start, ref end, Easing.Ease(amount, easingFunction), out result);

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
        => Vector4.Lerp(start, end, Easing.Ease(amount, easingFunction));

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
        => Color.Lerp(ref start, ref end, Easing.Ease(amount, easingFunction), out result);

    /// <summary>
    /// Performs an interpolation between two colors using an easing function.
    /// </summary>
    /// <param name="start">Start color.</param>
    /// <param name="end">End color.</param>
    /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>; values outside that range are clamped to it.</param>
    /// <param name="easingFunction">The function used to ease the interpolation.</param>
    /// <returns>The interpolation of the two colors.</returns>
    /// <remarks>
    /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color Interpolate(Color start, Color end, float amount, EasingFunction easingFunction)
        => Color.Lerp(start, end, Easing.Ease(amount, easingFunction));
}
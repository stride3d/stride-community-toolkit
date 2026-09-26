namespace Stride.CommunityToolkit.Mathematics;

/// <summary>
/// The fluent face of <see cref="Easing"/>: evaluate or interpolate straight from an
/// <see cref="EasingFunction"/> value, so a curve held in a field or a setting reads as
/// <c>curve.Interpolate(from, to, t)</c>.
/// </summary>
/// <remarks>
/// Every method forwards to <see cref="Easing.Ease{T}(T, EasingFunction)"/> or to the matching
/// <see cref="MathUtilEx"/> overload, so the time is clamped to [0, 1] the same way there.
/// </remarks>
public static class EasingFunctionExtensions
{
    /// <summary>Evaluates the curve at a normalised time, clamped to [0, 1].</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="function">The curve.</param>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    public static T Ease<T>(this EasingFunction function, T amount) where T : System.Numerics.IFloatingPointIeee754<T>
        => Easing.Ease(amount, function);

    /// <summary>Interpolates between two values along the curve.</summary>
    /// <param name="function">The curve.</param>
    /// <param name="start">The value at time 0.</param>
    /// <param name="end">The value at time 1.</param>
    /// <param name="amount">The normalised time, clamped to [0, 1].</param>
    /// <returns>The interpolated value.</returns>
    public static float Interpolate(this EasingFunction function, float start, float end, float amount)
        => MathUtilEx.Interpolate(start, end, amount, function);

    /// <summary>Interpolates between two points along the curve.</summary>
    /// <param name="function">The curve.</param>
    /// <param name="start">The point at time 0.</param>
    /// <param name="end">The point at time 1.</param>
    /// <param name="amount">The normalised time, clamped to [0, 1].</param>
    /// <returns>The interpolated point.</returns>
    public static Vector2 Interpolate(this EasingFunction function, Vector2 start, Vector2 end, float amount)
        => MathUtilEx.Interpolate(start, end, amount, function);

    /// <summary>Interpolates between two points along the curve.</summary>
    /// <param name="function">The curve.</param>
    /// <param name="start">The point at time 0.</param>
    /// <param name="end">The point at time 1.</param>
    /// <param name="amount">The normalised time, clamped to [0, 1].</param>
    /// <returns>The interpolated point.</returns>
    public static Vector3 Interpolate(this EasingFunction function, Vector3 start, Vector3 end, float amount)
        => MathUtilEx.Interpolate(start, end, amount, function);

    /// <summary>Interpolates between two vectors along the curve.</summary>
    /// <param name="function">The curve.</param>
    /// <param name="start">The vector at time 0.</param>
    /// <param name="end">The vector at time 1.</param>
    /// <param name="amount">The normalised time, clamped to [0, 1].</param>
    /// <returns>The interpolated vector.</returns>
    public static Vector4 Interpolate(this EasingFunction function, Vector4 start, Vector4 end, float amount)
        => MathUtilEx.Interpolate(start, end, amount, function);

    /// <summary>Interpolates between two colours along the curve, channel by channel.</summary>
    /// <param name="function">The curve.</param>
    /// <param name="start">The colour at time 0.</param>
    /// <param name="end">The colour at time 1.</param>
    /// <param name="amount">The normalised time, clamped to [0, 1].</param>
    /// <returns>The interpolated colour.</returns>
    public static Color Interpolate(this EasingFunction function, Color start, Color end, float amount)
        => MathUtilEx.Interpolate(start, end, amount, function);
}
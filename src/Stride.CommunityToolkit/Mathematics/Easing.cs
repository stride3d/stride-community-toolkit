using System.Numerics;

namespace Stride.CommunityToolkit.Mathematics;

/// <summary>
/// Easing curves: functions from a normalised time in [0, 1] to a progress in [0, 1] that start at
/// 0, end at 1, and shape the motion in between.
/// </summary>
/// <remarks>
/// <para>
/// Every curve is one generic method over any IEEE floating-point type, so <c>Easing.CubicEaseOut(0.3f)</c>
/// and <c>Easing.CubicEaseOut(0.3)</c> both work and share one implementation; the JIT specialises
/// the arithmetic per type, so the <see langword="float"/> path costs what a hand-written one would.
/// The curve methods are the raw formulas and do not clamp their input: a time past 1 keeps
/// following the polynomial. <see cref="Ease{T}(T, EasingFunction)"/>, which picks a curve by
/// <see cref="EasingFunction"/>, clamps the time first, because that is what an animation wants
/// when its clock runs past the end.
/// </para>
/// <para>
/// The polynomial, sine, circular, exponential, elastic, back and bounce families are the classic
/// set from Robert Penner's easing functions, modeled after the equations noted on each method.
/// <see cref="SmoothStep{T}(T)"/> and <see cref="SmootherStep{T}(T)"/> are the two Hermite blends
/// graphics code reaches for. To interpolate between two values with a curve, see
/// <see cref="MathUtilEx.Interpolate(float, float, float, EasingFunction)"/> and its vector and
/// colour overloads, or the fluent <see cref="EasingFunctionExtensions"/>.
/// </para>
/// </remarks>
public static class Easing
{
    /// <summary>
    /// Evaluates the curve named by <paramref name="function"/> at a normalised time, clamped to [0, 1].
    /// </summary>
    /// <typeparam name="T">A floating-point type: <see langword="float"/>, <see langword="double"/> or <see cref="System.Half"/>.</typeparam>
    /// <param name="amount">The normalised time. Values below 0 read as 0 and values above 1 as 1, so a clock that runs past the end holds at the end.</param>
    /// <param name="function">The curve to evaluate.</param>
    /// <returns>The eased progress: 0 at time 0, 1 at time 1, and for the elastic and back families briefly outside [0, 1] between.</returns>
    /// <remarks>
    /// This is the entry point for an animation configured by name. Call a curve method directly,
    /// such as <see cref="ElasticEaseOut{T}(T)"/>, when the curve is fixed and the input is known to
    /// be in range, or when the formula's behaviour outside [0, 1] is wanted.
    /// </remarks>
    public static T Ease<T>(T amount, EasingFunction function) where T : IFloatingPointIeee754<T>
    {
        amount = T.Clamp(amount, T.Zero, T.One);

        return function switch
        {
            EasingFunction.SmoothStep => SmoothStep(amount),
            EasingFunction.SmootherStep => SmootherStep(amount),
            EasingFunction.QuadraticEaseIn => QuadraticEaseIn(amount),
            EasingFunction.QuadraticEaseOut => QuadraticEaseOut(amount),
            EasingFunction.QuadraticEaseInOut => QuadraticEaseInOut(amount),
            EasingFunction.CubicEaseIn => CubicEaseIn(amount),
            EasingFunction.CubicEaseOut => CubicEaseOut(amount),
            EasingFunction.CubicEaseInOut => CubicEaseInOut(amount),
            EasingFunction.QuarticEaseIn => QuarticEaseIn(amount),
            EasingFunction.QuarticEaseOut => QuarticEaseOut(amount),
            EasingFunction.QuarticEaseInOut => QuarticEaseInOut(amount),
            EasingFunction.QuinticEaseIn => QuinticEaseIn(amount),
            EasingFunction.QuinticEaseOut => QuinticEaseOut(amount),
            EasingFunction.QuinticEaseInOut => QuinticEaseInOut(amount),
            EasingFunction.SineEaseIn => SineEaseIn(amount),
            EasingFunction.SineEaseOut => SineEaseOut(amount),
            EasingFunction.SineEaseInOut => SineEaseInOut(amount),
            EasingFunction.CircularEaseIn => CircularEaseIn(amount),
            EasingFunction.CircularEaseOut => CircularEaseOut(amount),
            EasingFunction.CircularEaseInOut => CircularEaseInOut(amount),
            EasingFunction.ExponentialEaseIn => ExponentialEaseIn(amount),
            EasingFunction.ExponentialEaseOut => ExponentialEaseOut(amount),
            EasingFunction.ExponentialEaseInOut => ExponentialEaseInOut(amount),
            EasingFunction.ElasticEaseIn => ElasticEaseIn(amount),
            EasingFunction.ElasticEaseOut => ElasticEaseOut(amount),
            EasingFunction.ElasticEaseInOut => ElasticEaseInOut(amount),
            EasingFunction.BackEaseIn => BackEaseIn(amount),
            EasingFunction.BackEaseOut => BackEaseOut(amount),
            EasingFunction.BackEaseInOut => BackEaseInOut(amount),
            EasingFunction.BounceEaseIn => BounceEaseIn(amount),
            EasingFunction.BounceEaseOut => BounceEaseOut(amount),
            EasingFunction.BounceEaseInOut => BounceEaseInOut(amount),
            _ => Linear(amount),
        };
    }

    /// <summary>No easing: the progress is the time.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The same value.</returns>
    /// <remarks>Modeled after the line <c>y = x</c>.</remarks>
    public static T Linear<T>(T amount) where T : IFloatingPointIeee754<T> => amount;

    /// <summary>The Hermite blend: gentle at both ends, straight through the middle.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>Modeled after <c>y = x²(3 - 2x)</c>, the cubic whose slope is 0 at both ends; what shader languages call <c>smoothstep</c>.</remarks>
    public static T SmoothStep<T>(T amount) where T : IFloatingPointIeee754<T>
        => amount * amount * (K<T>.Three - K<T>.Two * amount);

    /// <summary>Ken Perlin's smoother blend: flat at both ends, with no curvature there either.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>Modeled after <c>y = x³(x(6x - 15) + 10)</c>, the quintic whose first and second derivatives are 0 at both ends.</remarks>
    public static T SmootherStep<T>(T amount) where T : IFloatingPointIeee754<T>
        => amount * amount * amount * (amount * (K<T>.Six * amount - K<T>.Fifteen) + K<T>.Ten);

    /// <summary>Quadratic ease-in: starts slowly and accelerates.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>Modeled after the parabola <c>y = x²</c>.</remarks>
    public static T QuadraticEaseIn<T>(T amount) where T : IFloatingPointIeee754<T>
        => amount * amount;

    /// <summary>Quadratic ease-out: starts fast and decelerates.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>Modeled after the parabola <c>y = -x² + 2x</c>.</remarks>
    public static T QuadraticEaseOut<T>(T amount) where T : IFloatingPointIeee754<T>
        => -(amount * (amount - K<T>.Two));

    /// <summary>Quadratic ease-in-out: slow, fast, slow.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>
    /// Modeled after the piecewise quadratic
    /// <c>y = (1/2)((2x)²)</c> on [0, 0.5] and
    /// <c>y = -(1/2)((2x - 1)(2x - 3) - 1)</c> on [0.5, 1].
    /// </remarks>
    public static T QuadraticEaseInOut<T>(T amount) where T : IFloatingPointIeee754<T>
        => amount < K<T>.Half
            ? K<T>.Two * amount * amount
            : -K<T>.Two * amount * amount + K<T>.Four * amount - T.One;

    /// <summary>Cubic ease-in: starts slowly and accelerates harder than the quadratic.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>Modeled after the cubic <c>y = x³</c>.</remarks>
    public static T CubicEaseIn<T>(T amount) where T : IFloatingPointIeee754<T>
        => amount * amount * amount;

    /// <summary>Cubic ease-out: starts fast and decelerates harder than the quadratic.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>Modeled after the cubic <c>y = (x - 1)³ + 1</c>.</remarks>
    public static T CubicEaseOut<T>(T amount) where T : IFloatingPointIeee754<T>
    {
        var f = amount - T.One;

        return f * f * f + T.One;
    }

    /// <summary>Cubic ease-in-out: slow, fast, slow.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>
    /// Modeled after the piecewise cubic
    /// <c>y = (1/2)((2x)³)</c> on [0, 0.5] and
    /// <c>y = (1/2)((2x - 2)³ + 2)</c> on [0.5, 1].
    /// </remarks>
    public static T CubicEaseInOut<T>(T amount) where T : IFloatingPointIeee754<T>
    {
        if (amount < K<T>.Half)
        {
            return K<T>.Four * amount * amount * amount;
        }

        var f = K<T>.Two * amount - K<T>.Two;

        return K<T>.Half * f * f * f + T.One;
    }

    /// <summary>Quartic ease-in: starts very slowly and accelerates.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>Modeled after the quartic <c>y = x⁴</c>.</remarks>
    public static T QuarticEaseIn<T>(T amount) where T : IFloatingPointIeee754<T>
        => amount * amount * amount * amount;

    /// <summary>Quartic ease-out: starts fast and decelerates into a long tail.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>Modeled after the quartic <c>y = 1 - (x - 1)⁴</c>.</remarks>
    public static T QuarticEaseOut<T>(T amount) where T : IFloatingPointIeee754<T>
    {
        var f = amount - T.One;

        return f * f * f * (T.One - amount) + T.One;
    }

    /// <summary>Quartic ease-in-out: slow, fast, slow.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>
    /// Modeled after the piecewise quartic
    /// <c>y = (1/2)((2x)⁴)</c> on [0, 0.5] and
    /// <c>y = -(1/2)((2x - 2)⁴ - 2)</c> on [0.5, 1].
    /// </remarks>
    public static T QuarticEaseInOut<T>(T amount) where T : IFloatingPointIeee754<T>
    {
        if (amount < K<T>.Half)
        {
            return K<T>.Eight * amount * amount * amount * amount;
        }

        var f = amount - T.One;

        return -K<T>.Eight * f * f * f * f + T.One;
    }

    /// <summary>Quintic ease-in: the slowest start of the polynomial family.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>Modeled after the quintic <c>y = x⁵</c>.</remarks>
    public static T QuinticEaseIn<T>(T amount) where T : IFloatingPointIeee754<T>
        => amount * amount * amount * amount * amount;

    /// <summary>Quintic ease-out: the longest tail of the polynomial family.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>Modeled after the quintic <c>y = (x - 1)⁵ + 1</c>.</remarks>
    public static T QuinticEaseOut<T>(T amount) where T : IFloatingPointIeee754<T>
    {
        var f = amount - T.One;

        return f * f * f * f * f + T.One;
    }

    /// <summary>Quintic ease-in-out: slow, fast, slow.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>
    /// Modeled after the piecewise quintic
    /// <c>y = (1/2)((2x)⁵)</c> on [0, 0.5] and
    /// <c>y = (1/2)((2x - 2)⁵ + 2)</c> on [0.5, 1].
    /// </remarks>
    public static T QuinticEaseInOut<T>(T amount) where T : IFloatingPointIeee754<T>
    {
        if (amount < K<T>.Half)
        {
            return K<T>.Sixteen * amount * amount * amount * amount * amount;
        }

        var f = K<T>.Two * amount - K<T>.Two;

        return K<T>.Half * f * f * f * f * f + T.One;
    }

    /// <summary>Sine ease-in: the softest possible start.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>Modeled after a quarter cycle of a sine wave, <c>y = sin((x - 1)π/2) + 1</c>.</remarks>
    public static T SineEaseIn<T>(T amount) where T : IFloatingPointIeee754<T>
        => T.Sin((amount - T.One) * K<T>.PiOverTwo) + T.One;

    /// <summary>Sine ease-out: the softest possible finish.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>Modeled after a quarter cycle of a sine wave in a different phase, <c>y = sin(xπ/2)</c>.</remarks>
    public static T SineEaseOut<T>(T amount) where T : IFloatingPointIeee754<T>
        => T.Sin(amount * K<T>.PiOverTwo);

    /// <summary>Sine ease-in-out: soft at both ends.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>Modeled after half a sine wave, <c>y = (1 - cos(xπ)) / 2</c>.</remarks>
    public static T SineEaseInOut<T>(T amount) where T : IFloatingPointIeee754<T>
        => K<T>.Half * (T.One - T.Cos(amount * T.Pi));

    /// <summary>Circular ease-in: barely moves, then rushes the end.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>Modeled after the shifted fourth quadrant of the unit circle, <c>y = 1 - √(1 - x²)</c>.</remarks>
    public static T CircularEaseIn<T>(T amount) where T : IFloatingPointIeee754<T>
        => T.One - T.Sqrt(T.One - amount * amount);

    /// <summary>Circular ease-out: rushes the start, then barely moves.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>Modeled after the shifted second quadrant of the unit circle, <c>y = √((2 - x)x)</c>.</remarks>
    public static T CircularEaseOut<T>(T amount) where T : IFloatingPointIeee754<T>
        => T.Sqrt((K<T>.Two - amount) * amount);

    /// <summary>Circular ease-in-out: two quarter circles meeting in the middle.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>
    /// Modeled after the piecewise circular function
    /// <c>y = (1/2)(1 - √(1 - 4x²))</c> on [0, 0.5] and
    /// <c>y = (1/2)(√(-(2x - 3)(2x - 1)) + 1)</c> on [0.5, 1].
    /// </remarks>
    public static T CircularEaseInOut<T>(T amount) where T : IFloatingPointIeee754<T>
        => amount < K<T>.Half
            ? K<T>.Half * (T.One - T.Sqrt(T.One - K<T>.Four * amount * amount))
            : K<T>.Half * (T.Sqrt(-(K<T>.Two * amount - K<T>.Three) * (K<T>.Two * amount - T.One)) + T.One);

    /// <summary>Exponential ease-in: almost still, then explosive.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>Modeled after <c>y = 2^(10(x - 1))</c>, pinned to exactly 0 at time 0, where the formula alone would give 1/1024.</remarks>
    public static T ExponentialEaseIn<T>(T amount) where T : IFloatingPointIeee754<T>
        => amount == T.Zero ? amount : T.Pow(K<T>.Two, K<T>.Ten * (amount - T.One));

    /// <summary>Exponential ease-out: explosive, then almost still.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>Modeled after <c>y = 1 - 2^(-10x)</c>, pinned to exactly 1 at time 1.</remarks>
    public static T ExponentialEaseOut<T>(T amount) where T : IFloatingPointIeee754<T>
        => amount == T.One ? amount : T.One - T.Pow(K<T>.Two, -K<T>.Ten * amount);

    /// <summary>Exponential ease-in-out: still, explosive through the middle, still.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>
    /// Modeled after the piecewise exponential
    /// <c>y = (1/2)2^(10(2x - 1))</c> on [0, 0.5] and
    /// <c>y = -(1/2)2^(-10(2x - 1)) + 1</c> on [0.5, 1], pinned to the exact ends.
    /// </remarks>
    public static T ExponentialEaseInOut<T>(T amount) where T : IFloatingPointIeee754<T>
    {
        if (amount == T.Zero || amount == T.One)
        {
            return amount;
        }

        return amount < K<T>.Half
            ? K<T>.Half * T.Pow(K<T>.Two, K<T>.Twenty * amount - K<T>.Ten)
            : -K<T>.Half * T.Pow(K<T>.Two, -K<T>.Twenty * amount + K<T>.Ten) + T.One;
    }

    /// <summary>Elastic ease-in: winds up with growing wobbles, then snaps to the end.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress, dipping below 0 on the way.</returns>
    /// <remarks>Modeled after the damped sine wave <c>y = sin(13πx/2) 2^(10(x - 1))</c>.</remarks>
    public static T ElasticEaseIn<T>(T amount) where T : IFloatingPointIeee754<T>
        => T.Sin(K<T>.Thirteen * K<T>.PiOverTwo * amount) * T.Pow(K<T>.Two, K<T>.Ten * (amount - T.One));

    /// <summary>Elastic ease-out: snaps past the end and wobbles back onto it.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress, rising above 1 on the way.</returns>
    /// <remarks>Modeled after the damped sine wave <c>y = sin(-13π(x + 1)/2) 2^(-10x) + 1</c>.</remarks>
    public static T ElasticEaseOut<T>(T amount) where T : IFloatingPointIeee754<T>
        => T.Sin(-K<T>.Thirteen * K<T>.PiOverTwo * (amount + T.One)) * T.Pow(K<T>.Two, -K<T>.Ten * amount) + T.One;

    /// <summary>Elastic ease-in-out: wobbles out of the start and into the end.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress, leaving [0, 1] near both ends.</returns>
    /// <remarks>
    /// Modeled after the piecewise exponentially damped sine wave
    /// <c>y = (1/2) sin(13π(2x)/2) 2^(10(2x - 1))</c> on [0, 0.5] and
    /// <c>y = (1/2)(sin(-13π((2x - 1) + 1)/2) 2^(-10(2x - 1)) + 2)</c> on [0.5, 1].
    /// </remarks>
    public static T ElasticEaseInOut<T>(T amount) where T : IFloatingPointIeee754<T>
    {
        if (amount < K<T>.Half)
        {
            var f = K<T>.Two * amount;

            return K<T>.Half * T.Sin(K<T>.Thirteen * K<T>.PiOverTwo * f) * T.Pow(K<T>.Two, K<T>.Ten * (f - T.One));
        }

        var g = K<T>.Two * amount - T.One;

        return K<T>.Half * (T.Sin(-K<T>.Thirteen * K<T>.PiOverTwo * (g + T.One)) * T.Pow(K<T>.Two, -K<T>.Ten * g) + K<T>.Two);
    }

    /// <summary>Back ease-in: pulls back below 0 before setting off.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress, dipping below 0 early on.</returns>
    /// <remarks>Modeled after the overshooting cubic <c>y = x³ - x sin(xπ)</c>.</remarks>
    public static T BackEaseIn<T>(T amount) where T : IFloatingPointIeee754<T>
        => amount * amount * amount - amount * T.Sin(amount * T.Pi);

    /// <summary>Back ease-out: overshoots past 1 and settles back onto it.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress, rising above 1 late on.</returns>
    /// <remarks>Modeled after the overshooting cubic <c>y = 1 - ((1 - x)³ - (1 - x) sin((1 - x)π))</c>.</remarks>
    public static T BackEaseOut<T>(T amount) where T : IFloatingPointIeee754<T>
    {
        var f = T.One - amount;

        return T.One - (f * f * f - f * T.Sin(f * T.Pi));
    }

    /// <summary>Back ease-in-out: pulls back at the start and overshoots at the end.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress, leaving [0, 1] near both ends.</returns>
    /// <remarks>
    /// Modeled after the piecewise overshooting cubic
    /// <c>y = (1/2)((2x)³ - (2x) sin(2xπ))</c> on [0, 0.5] and
    /// <c>y = (1/2)(1 - ((1 - x)³ - (1 - x) sin((1 - x)π))) + 1/2</c> on [0.5, 1], with <c>x</c> rescaled to the half.
    /// </remarks>
    public static T BackEaseInOut<T>(T amount) where T : IFloatingPointIeee754<T>
    {
        if (amount < K<T>.Half)
        {
            var f = K<T>.Two * amount;

            return K<T>.Half * (f * f * f - f * T.Sin(f * T.Pi));
        }

        var g = T.One - (K<T>.Two * amount - T.One);

        return K<T>.Half * (T.One - (g * g * g - g * T.Sin(g * T.Pi))) + K<T>.Half;
    }

    /// <summary>Bounce ease-in: bounces off the start, a dropped ball played in reverse.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>The mirror of <see cref="BounceEaseOut{T}(T)"/>: <c>y = 1 - BounceEaseOut(1 - x)</c>.</remarks>
    public static T BounceEaseIn<T>(T amount) where T : IFloatingPointIeee754<T>
        => T.One - BounceEaseOut(T.One - amount);

    /// <summary>Bounce ease-out: lands with diminishing bounces, as a dropped ball.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>
    /// Modeled after four parabolic arcs of shrinking height, with the breaks at 4/11, 8/11 and 9/10
    /// of the time; the classic coefficients from Penner's bounce.
    /// </remarks>
    public static T BounceEaseOut<T>(T amount) where T : IFloatingPointIeee754<T>
    {
        if (amount < K<T>.BounceBreak1)
        {
            return K<T>.BounceA1 * amount * amount;
        }

        if (amount < K<T>.BounceBreak2)
        {
            return K<T>.BounceA2 * amount * amount - K<T>.BounceB2 * amount + K<T>.BounceC2;
        }

        if (amount < K<T>.BounceBreak3)
        {
            return K<T>.BounceA3 * amount * amount - K<T>.BounceB3 * amount + K<T>.BounceC3;
        }

        return K<T>.BounceA4 * amount * amount - K<T>.BounceB4 * amount + K<T>.BounceC4;
    }

    /// <summary>Bounce ease-in-out: bounces off the start and lands with bounces.</summary>
    /// <typeparam name="T">A floating-point type.</typeparam>
    /// <param name="amount">The normalised time.</param>
    /// <returns>The eased progress.</returns>
    /// <remarks>
    /// <see cref="BounceEaseIn{T}(T)"/> squeezed into the first half and
    /// <see cref="BounceEaseOut{T}(T)"/> into the second.
    /// </remarks>
    public static T BounceEaseInOut<T>(T amount) where T : IFloatingPointIeee754<T>
        => amount < K<T>.Half
            ? K<T>.Half * BounceEaseIn(amount * K<T>.Two)
            : K<T>.Half * BounceEaseOut(amount * K<T>.Two - T.One) + K<T>.Half;

    /// <summary>
    /// The constants the curves use, converted once per floating-point type so the formulas read as
    /// they are written and the JIT sees a constant.
    /// </summary>
    private static class K<T> where T : IFloatingPointIeee754<T>
    {
        public static readonly T Half = T.CreateTruncating(0.5);
        public static readonly T Two = T.CreateTruncating(2);
        public static readonly T Three = T.CreateTruncating(3);
        public static readonly T Four = T.CreateTruncating(4);
        public static readonly T Six = T.CreateTruncating(6);
        public static readonly T Eight = T.CreateTruncating(8);
        public static readonly T Ten = T.CreateTruncating(10);
        public static readonly T Thirteen = T.CreateTruncating(13);
        public static readonly T Fifteen = T.CreateTruncating(15);
        public static readonly T Sixteen = T.CreateTruncating(16);
        public static readonly T Twenty = T.CreateTruncating(20);
        public static readonly T PiOverTwo = T.Pi / Two;

        // Penner's bounce: four parabolas, y = a·x² - b·x + c, with the breaks between them
        public static readonly T BounceBreak1 = T.CreateTruncating(4.0 / 11.0);
        public static readonly T BounceBreak2 = T.CreateTruncating(8.0 / 11.0);
        public static readonly T BounceBreak3 = T.CreateTruncating(9.0 / 10.0);
        public static readonly T BounceA1 = T.CreateTruncating(121.0 / 16.0);
        public static readonly T BounceA2 = T.CreateTruncating(363.0 / 40.0);
        public static readonly T BounceB2 = T.CreateTruncating(99.0 / 10.0);
        public static readonly T BounceC2 = T.CreateTruncating(17.0 / 5.0);
        public static readonly T BounceA3 = T.CreateTruncating(4356.0 / 361.0);
        public static readonly T BounceB3 = T.CreateTruncating(35442.0 / 1805.0);
        public static readonly T BounceC3 = T.CreateTruncating(16061.0 / 1805.0);
        public static readonly T BounceA4 = T.CreateTruncating(54.0 / 5.0);
        public static readonly T BounceB4 = T.CreateTruncating(513.0 / 25.0);
        public static readonly T BounceC4 = T.CreateTruncating(268.0 / 25.0);
    }
}
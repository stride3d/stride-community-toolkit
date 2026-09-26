namespace Stride.CommunityToolkit.Mathematics;

/// <summary>
/// The easing curves <see cref="Easing"/> implements, selectable by value so an animation can be
/// configured with a name instead of a method.
/// </summary>
/// <remarks>
/// Every curve maps a normalised time in [0, 1] to a progress in [0, 1], starting at 0 and ending
/// at 1. <c>EaseIn</c> starts slowly and accelerates, <c>EaseOut</c> starts fast and decelerates,
/// <c>EaseInOut</c> does both. The <c>Elastic</c> and <c>Back</c> families overshoot the ends on
/// the way, which is what gives a bounce or a wobble; every other curve stays inside [0, 1].
/// Evaluate one with <see cref="Easing.Ease{T}(T, EasingFunction)"/> or the extension
/// <see cref="EasingFunctionExtensions.Ease{T}(EasingFunction, T)"/>.
/// </remarks>
public enum EasingFunction
{
    /// <summary>No easing: progress equals time. <c>y = x</c>.</summary>
    Linear,

    /// <summary>Hermite blend, gentle at both ends. <c>y = x²(3 - 2x)</c>.</summary>
    SmoothStep,

    /// <summary>Ken Perlin's smoother blend, flat at both ends. <c>y = x³(x(6x - 15) + 10)</c>.</summary>
    SmootherStep,

    /// <summary>Starts slowly, accelerates. <c>y = x²</c>.</summary>
    QuadraticEaseIn,

    /// <summary>Starts fast, decelerates. <c>y = -x² + 2x</c>.</summary>
    QuadraticEaseOut,

    /// <summary>Slow, fast, slow, on a parabola each side of the middle.</summary>
    QuadraticEaseInOut,

    /// <summary>Starts slowly, accelerates harder than quadratic. <c>y = x³</c>.</summary>
    CubicEaseIn,

    /// <summary>Starts fast, decelerates harder than quadratic. <c>y = (x - 1)³ + 1</c>.</summary>
    CubicEaseOut,

    /// <summary>Slow, fast, slow, on a cubic each side of the middle.</summary>
    CubicEaseInOut,

    /// <summary>Starts very slowly, accelerates. <c>y = x⁴</c>.</summary>
    QuarticEaseIn,

    /// <summary>Starts fast, decelerates to a long tail. <c>y = 1 - (x - 1)⁴</c>.</summary>
    QuarticEaseOut,

    /// <summary>Slow, fast, slow, on a quartic each side of the middle.</summary>
    QuarticEaseInOut,

    /// <summary>The slowest start of the polynomial family. <c>y = x⁵</c>.</summary>
    QuinticEaseIn,

    /// <summary>The longest tail of the polynomial family. <c>y = (x - 1)⁵ + 1</c>.</summary>
    QuinticEaseOut,

    /// <summary>Slow, fast, slow, on a quintic each side of the middle.</summary>
    QuinticEaseInOut,

    /// <summary>A quarter of a sine wave: the softest ease-in. <c>y = sin((x - 1)π/2) + 1</c>.</summary>
    SineEaseIn,

    /// <summary>A quarter of a sine wave: the softest ease-out. <c>y = sin(xπ/2)</c>.</summary>
    SineEaseOut,

    /// <summary>Half a sine wave: soft at both ends. <c>y = (1 - cos(xπ)) / 2</c>.</summary>
    SineEaseInOut,

    /// <summary>A quarter circle: barely moves, then rushes the end. <c>y = 1 - √(1 - x²)</c>.</summary>
    CircularEaseIn,

    /// <summary>A quarter circle: rushes the start, then barely moves. <c>y = √((2 - x)x)</c>.</summary>
    CircularEaseOut,

    /// <summary>Two quarter circles meeting in the middle.</summary>
    CircularEaseInOut,

    /// <summary>Almost still, then explosive. <c>y = 2^(10(x - 1))</c>.</summary>
    ExponentialEaseIn,

    /// <summary>Explosive, then almost still. <c>y = 1 - 2^(-10x)</c>.</summary>
    ExponentialEaseOut,

    /// <summary>Still, explosive through the middle, still.</summary>
    ExponentialEaseInOut,

    /// <summary>Winds up with growing wobbles, then snaps to the end. Overshoots below 0.</summary>
    ElasticEaseIn,

    /// <summary>Snaps past the end and wobbles back onto it. Overshoots above 1.</summary>
    ElasticEaseOut,

    /// <summary>Wobbles out of the start and into the end.</summary>
    ElasticEaseInOut,

    /// <summary>Pulls back below 0 before setting off. <c>y = x³ - x sin(xπ)</c>.</summary>
    BackEaseIn,

    /// <summary>Overshoots past 1 and settles back. <c>y = 1 - ((1 - x)³ - (1 - x) sin((1 - x)π))</c>.</summary>
    BackEaseOut,

    /// <summary>Pulls back at the start and overshoots at the end.</summary>
    BackEaseInOut,

    /// <summary>Bounces off the start, as a ball dropped in reverse.</summary>
    BounceEaseIn,

    /// <summary>Lands with diminishing bounces, as a dropped ball.</summary>
    BounceEaseOut,

    /// <summary>Bounces off the start and lands with bounces.</summary>
    BounceEaseInOut,
}
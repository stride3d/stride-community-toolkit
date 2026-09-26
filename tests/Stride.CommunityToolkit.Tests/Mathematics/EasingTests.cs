using Stride.CommunityToolkit.Mathematics;
using Stride.Core.Mathematics;
using System.Reflection;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Mathematics;

/// <summary>
/// The properties every easing curve must have, checked over the whole <see cref="EasingFunction"/>
/// list so a curve added later is held to the same contract: the ends land exactly, the in-out
/// curves pass through the middle, ease-in and ease-out mirror each other, the enum dispatch
/// agrees with the direct call, the float and double paths agree, and the dispatcher clamps.
/// </summary>
public class EasingTests
{
    private const float Tolerance = 1e-5f;

    public static TheoryData<EasingFunction> AllFunctions
        => new(Enum.GetValues<EasingFunction>());

    public static TheoryData<EasingFunction> InOutFunctions
        => new(Enum.GetValues<EasingFunction>().Where(f => f.ToString().EndsWith("InOut", StringComparison.Ordinal)));

    public static TheoryData<EasingFunction, EasingFunction> InOutPairs
    {
        get
        {
            var pairs = new TheoryData<EasingFunction, EasingFunction>();

            foreach (var easeIn in Enum.GetValues<EasingFunction>().Where(f => f.ToString().EndsWith("EaseIn", StringComparison.Ordinal)))
            {
                pairs.Add(easeIn, Enum.Parse<EasingFunction>(easeIn.ToString()[..^2] + "Out"));
            }

            return pairs;
        }
    }

    [Theory]
    [MemberData(nameof(AllFunctions))]
    public void StartsAtZeroAndEndsAtOne(EasingFunction function)
    {
        Assert.Equal(0f, Easing.Ease(0f, function), Tolerance);
        Assert.Equal(1f, Easing.Ease(1f, function), Tolerance);
    }

    [Theory]
    [MemberData(nameof(InOutFunctions))]
    public void InOutPassesThroughTheMiddle(EasingFunction function)
        => Assert.Equal(0.5f, Easing.Ease(0.5f, function), Tolerance);

    [Theory]
    [InlineData(EasingFunction.Linear)]
    [InlineData(EasingFunction.SmoothStep)]
    [InlineData(EasingFunction.SmootherStep)]
    public void SymmetricBlendsPassThroughTheMiddle(EasingFunction function)
        => Assert.Equal(0.5f, Easing.Ease(0.5f, function), Tolerance);

    [Theory]
    [MemberData(nameof(InOutPairs))]
    public void EaseOutMirrorsEaseIn(EasingFunction easeIn, EasingFunction easeOut)
    {
        for (var i = 0; i <= 20; i++)
        {
            var t = i / 20f;

            Assert.Equal(1f - Easing.Ease(1f - t, easeIn), Easing.Ease(t, easeOut), Tolerance);
        }
    }

    [Theory]
    [MemberData(nameof(AllFunctions))]
    public void DispatchMatchesTheDirectCall(EasingFunction function)
    {
        var method = typeof(Easing).GetMethod(function.ToString(), BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Easing has no method named {function}.");

        var direct = method.MakeGenericMethod(typeof(float));

        for (var i = 0; i <= 20; i++)
        {
            var t = i / 20f;

            Assert.Equal((float)direct.Invoke(null, [t])!, Easing.Ease(t, function), Tolerance);
        }
    }

    [Theory]
    [MemberData(nameof(AllFunctions))]
    public void FloatAndDoubleAgree(EasingFunction function)
    {
        for (var i = 0; i <= 20; i++)
        {
            var t = i / 20.0;

            Assert.Equal(Easing.Ease(t, function), Easing.Ease((float)t, function), 1e-4);
        }
    }

    [Theory]
    [MemberData(nameof(AllFunctions))]
    public void DispatchClampsTheTime(EasingFunction function)
    {
        Assert.Equal(Easing.Ease(0f, function), Easing.Ease(-1f, function));
        Assert.Equal(Easing.Ease(1f, function), Easing.Ease(2f, function));
    }

    [Fact]
    public void TheRawCurvesDoNotClamp()
        => Assert.Equal(4f, Easing.QuadraticEaseIn(2f));

    [Fact]
    public void SmootherStepIsPerlinsQuintic()
        => Assert.Equal(0.103515625f, Easing.SmootherStep(0.25f), Tolerance);

    [Fact]
    public void TheExtensionsForwardToTheSameCurve()
    {
        Assert.Equal(Easing.CubicEaseOut(0.3f), EasingFunction.CubicEaseOut.Ease(0.3f), Tolerance);
        Assert.Equal(MathUtilEx.Interpolate(2f, 6f, 0.5f, EasingFunction.Linear), EasingFunction.Linear.Interpolate(2f, 6f, 0.5f), Tolerance);

        var point = EasingFunction.QuadraticEaseOut.Interpolate(Vector3.Zero, new Vector3(0f, 10f, 0f), 0.5f);

        Assert.Equal(7.5f, point.Y, Tolerance);
    }

    [Fact]
    public void InterpolateHoldsAtTheEndsPastTheClock()
    {
        Assert.Equal(6f, EasingFunction.ElasticEaseOut.Interpolate(2f, 6f, 1.5f), Tolerance);
        Assert.Equal(2f, EasingFunction.ElasticEaseIn.Interpolate(2f, 6f, -0.5f), Tolerance);
    }
}
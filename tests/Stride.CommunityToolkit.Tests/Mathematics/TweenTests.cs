using Stride.CommunityToolkit.Mathematics;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Mathematics;

/// <summary>
/// The contract of <see cref="Tween"/>: it advances only while running, folds its clock the way
/// each <see cref="TweenLoop"/> promises, completes exactly once, and interpolates with the eased
/// value rather than the raw progress.
/// </summary>
public class TweenTests
{
    [Fact]
    public void DoesNotAdvanceUntilStarted()
    {
        var tween = new Tween(1f);

        tween.Update(0.5f);

        Assert.False(tween.IsRunning);
        Assert.Equal(0f, tween.Progress);
    }

    [Fact]
    public void RunsToTheEndOnceAndCompletes()
    {
        var tween = Tween.Run(2f, EasingFunction.QuadraticEaseIn);

        tween.Update(1f);

        Assert.Equal(0.5f, tween.Progress, Tolerances.Float);
        Assert.Equal(0.25f, tween.Value, Tolerances.Float);
        Assert.False(tween.IsComplete);

        tween.Update(5f);

        Assert.Equal(1f, tween.Progress, Tolerances.Float);
        Assert.Equal(1f, tween.Value, Tolerances.Float);
        Assert.True(tween.IsComplete);
        Assert.False(tween.IsRunning);
    }

    [Fact]
    public void RepeatWrapsTheClock()
    {
        var tween = Tween.Run(1f, loop: TweenLoop.Repeat);

        tween.Update(2.25f);

        Assert.Equal(0.25f, tween.Progress, Tolerances.Float);
        Assert.True(tween.IsRunning);
        Assert.False(tween.IsComplete);
    }

    [Fact]
    public void PingPongRunsBackAlongTheCurve()
    {
        var tween = Tween.Run(1f, loop: TweenLoop.PingPong);

        tween.Update(1.25f);

        Assert.Equal(0.75f, tween.Progress, Tolerances.Float);

        tween.Update(1f);

        Assert.Equal(0.25f, tween.Progress, Tolerances.Float);
    }

    [Fact]
    public void StopFreezesAndResumeCarriesOn()
    {
        var tween = Tween.Run(1f);

        tween.Update(0.25f);
        tween.Stop();
        tween.Update(0.25f);

        Assert.Equal(0.25f, tween.Progress, Tolerances.Float);

        tween.Resume();
        tween.Update(0.25f);

        Assert.Equal(0.5f, tween.Progress, Tolerances.Float);
    }

    [Fact]
    public void StartRewindsARunningTween()
    {
        var tween = Tween.Run(1f);

        tween.Update(0.8f);
        tween.Start();

        Assert.Equal(0f, tween.Progress);
        Assert.True(tween.IsRunning);
    }

    [Fact]
    public void LerpUsesTheEasedValue()
    {
        var tween = Tween.Run(1f, EasingFunction.QuadraticEaseOut);

        tween.Update(0.5f);

        Assert.Equal(7.5f, tween.Lerp(0f, 10f), Tolerances.Float);
        Assert.Equal(7.5f, tween.Lerp(new Vector3(0f), new Vector3(10f)).X, Tolerances.Float);
    }

    [Fact]
    public void ResetReturnsToTheUnstartedState()
    {
        var tween = Tween.Run(1f);

        tween.Update(2f);
        tween.Reset();

        Assert.False(tween.IsRunning);
        Assert.False(tween.IsComplete);
        Assert.Equal(0f, tween.Progress);
    }

    [Fact]
    public void RejectsANonPositiveDuration()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Tween(0f));

    // NaN passes every comparison, so it would slip past a sign check and make a tween whose progress is NaN
    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void RejectsANonFiniteDuration(float duration)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Tween(duration));
}
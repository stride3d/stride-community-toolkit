using Stride.Games;

namespace Stride.CommunityToolkit.Mathematics;

/// <summary>
/// A clock over one easing curve: start it, feed it the frame time, read a value between 0 and 1
/// or a value interpolated between two of your own. The plumbing every eased animation needs -
/// a start, a duration, elapsed time, what happens at the end - in one object instead of three
/// fields and an <c>if</c>.
/// </summary>
/// <remarks>
/// <para>
/// A tween is not a scheduler: it does not run itself, call anything back, or know about the
/// scene. Call <see cref="Update(float)"/> once per frame from wherever the frame time is known,
/// then read <see cref="Value"/> or one of the <c>Lerp</c> helpers. That keeps it usable from a
/// script, a processor or a plain update callback alike, and makes a paused game trivial: stop
/// feeding it time.
/// </para>
/// <para>
/// <see cref="Loop"/> decides what the end means. <see cref="TweenLoop.None"/> holds the last
/// value and sets <see cref="IsComplete"/>; <see cref="TweenLoop.Repeat"/> starts over;
/// <see cref="TweenLoop.PingPong"/> runs the curve backwards to the start and forwards again.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var pop = Tween.Run(0.6f, EasingFunction.BackEaseOut);
///
/// // every frame
/// pop.Update(gameTime);
/// entity.Transform.Scale = new Vector3(pop.Lerp(0f, 1f));
/// </code>
/// </example>
public sealed class Tween
{
    /// <summary>Creates a tween that is not running yet; call <see cref="Start"/> or use <see cref="Run"/>.</summary>
    /// <param name="duration">How long one run takes, in seconds. Must be positive.</param>
    /// <param name="function">The curve the value follows.</param>
    /// <param name="loop">What happens at the end of a run.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="duration"/> is not positive.</exception>
    public Tween(float duration, EasingFunction function = EasingFunction.Linear, TweenLoop loop = TweenLoop.None)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(duration);

        Duration = duration;
        Function = function;
        Loop = loop;
    }

    /// <summary>How long one run takes, in seconds.</summary>
    public float Duration { get; }

    /// <summary>The curve the value follows.</summary>
    public EasingFunction Function { get; set; }

    /// <summary>What happens at the end of a run.</summary>
    public TweenLoop Loop { get; set; }

    /// <summary>Seconds fed in since the last <see cref="Start"/>, folded into the current run for a looping tween.</summary>
    public float Elapsed { get; private set; }

    /// <summary>Whether <see cref="Update(float)"/> still advances the clock.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Whether a non-looping tween has reached its end. A looping tween never completes; stop it
    /// with <see cref="Stop"/>.
    /// </summary>
    /// <remarks>
    /// Stays set until <see cref="Start"/> or <see cref="Reset"/>. Code that writes a transform from the
    /// tween every frame it reports running or complete keeps writing the end pose for ever, which pins
    /// the object there against anything else that moves it, such as a camera controller. Write the end
    /// pose once on the frame it completes and then <see cref="Reset"/>, or drive only while
    /// <see cref="IsRunning"/> and let the last update land it.
    /// </remarks>
    public bool IsComplete { get; private set; }

    /// <summary>The normalised time in [0, 1], before easing: where the run is, with ping-pong folded back.</summary>
    public float Progress => Loop == TweenLoop.PingPong && Elapsed > Duration
        ? 2f - Elapsed / Duration
        : Elapsed / Duration;

    /// <summary>The eased value in [0, 1], or briefly outside it for the elastic and back curves.</summary>
    public float Value => Easing.Ease(Progress, Function);

    /// <summary>Creates a tween and starts it in one call.</summary>
    /// <param name="duration">How long one run takes, in seconds.</param>
    /// <param name="function">The curve the value follows.</param>
    /// <param name="loop">What happens at the end of a run.</param>
    /// <returns>The running tween.</returns>
    public static Tween Run(float duration, EasingFunction function = EasingFunction.Linear, TweenLoop loop = TweenLoop.None)
        => new Tween(duration, function, loop).Start();

    /// <summary>Rewinds to the start and runs; on a running tween this is a restart.</summary>
    /// <returns>This tween, so a call can chain onto the constructor.</returns>
    public Tween Start()
    {
        Elapsed = 0f;
        IsRunning = true;
        IsComplete = false;

        return this;
    }

    /// <summary>Rewinds to the start without running: the state a new tween has, so <see cref="IsComplete"/> is off and <see cref="Value"/> is the curve at 0.</summary>
    public void Reset()
    {
        Elapsed = 0f;
        IsRunning = false;
        IsComplete = false;
    }

    /// <summary>Freezes the clock where it is; <see cref="Value"/> keeps reporting that point. <see cref="Start"/> rewinds, <see cref="Resume"/> carries on.</summary>
    public void Stop() => IsRunning = false;

    /// <summary>Carries on from where <see cref="Stop"/> left the clock.</summary>
    public void Resume()
    {
        if (!IsComplete) IsRunning = true;
    }

    /// <summary>Advances the clock by a frame's worth of seconds.</summary>
    /// <param name="deltaSeconds">The seconds since the last update. Nothing happens for zero or a stopped tween.</param>
    public void Update(float deltaSeconds)
    {
        if (!IsRunning || deltaSeconds <= 0f) return;

        Elapsed += deltaSeconds;

        switch (Loop)
        {
            case TweenLoop.None when Elapsed >= Duration:
                Elapsed = Duration;
                IsRunning = false;
                IsComplete = true;
                break;

            case TweenLoop.Repeat:
                Elapsed %= Duration;
                break;

            case TweenLoop.PingPong:
                Elapsed %= 2f * Duration;
                break;
        }
    }

    /// <summary>Advances the clock by the frame time of a <see cref="GameTime"/>.</summary>
    /// <param name="time">The game time of the current update.</param>
    public void Update(GameTime time) => Update((float)time.Elapsed.TotalSeconds);

    /// <summary>The value between two numbers at the tween's current point.</summary>
    /// <param name="from">The value at the start.</param>
    /// <param name="to">The value at the end.</param>
    /// <returns>The interpolated value.</returns>
    public float Lerp(float from, float to) => MathUtil.Lerp(from, to, Value);

    /// <summary>The point between two points at the tween's current point.</summary>
    /// <param name="from">The point at the start.</param>
    /// <param name="to">The point at the end.</param>
    /// <returns>The interpolated point.</returns>
    public Vector2 Lerp(Vector2 from, Vector2 to) => Vector2.Lerp(from, to, Value);

    /// <summary>The point between two points at the tween's current point.</summary>
    /// <param name="from">The point at the start.</param>
    /// <param name="to">The point at the end.</param>
    /// <returns>The interpolated point.</returns>
    public Vector3 Lerp(Vector3 from, Vector3 to) => Vector3.Lerp(from, to, Value);

    /// <summary>The colour between two colours at the tween's current point, channel by channel.</summary>
    /// <param name="from">The colour at the start.</param>
    /// <param name="to">The colour at the end.</param>
    /// <returns>The interpolated colour.</returns>
    public Color Lerp(Color from, Color to) => Color.Lerp(from, to, Value);

    /// <summary>The rotation between two rotations at the tween's current point, along the shorter arc.</summary>
    /// <param name="from">The rotation at the start.</param>
    /// <param name="to">The rotation at the end.</param>
    /// <returns>The interpolated rotation.</returns>
    public Quaternion Slerp(Quaternion from, Quaternion to) => Quaternion.Slerp(from, to, Value);
}
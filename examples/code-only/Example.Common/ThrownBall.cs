namespace Example.Common;

/// <summary>
/// A ball thrown and integrated by hand, for an example that records where a body goes and is not
/// about the integrator: semi-implicit Euler under gravity on Y.
/// </summary>
public sealed class ThrownBall
{
    /// <summary>The gravity the ball falls under, on Y, in metres per second squared.</summary>
    public const float Gravity = 9.81f;

    /// <summary>Where the ball is.</summary>
    public Vector3 Position { get; private set; }

    /// <summary>How fast it is going.</summary>
    public Vector3 Velocity { get; private set; }

    /// <summary>Whether the ball is in the air: from <see cref="Launch"/> until <see cref="Land"/>.</summary>
    public bool Flying { get; private set; }

    /// <summary>Starts a flight.</summary>
    /// <param name="position">Where the ball starts.</param>
    /// <param name="velocity">Its velocity as it leaves.</param>
    public void Launch(Vector3 position, Vector3 velocity)
    {
        Position = position;
        Velocity = velocity;
        Flying = true;
    }

    /// <summary>Moves a flying ball by one frame. The step is capped, so a stall - a dragged window - cannot teleport it.</summary>
    /// <param name="seconds">The frame's elapsed time.</param>
    public void Step(float seconds)
    {
        if (!Flying) return;

        var dt = MathF.Min(seconds, 0.1f);

        Velocity -= new Vector3(0f, Gravity * dt, 0f);
        Position += Velocity * dt;
    }

    /// <summary>Ends the flight where the ball is.</summary>
    public void Land() => Flying = false;
}
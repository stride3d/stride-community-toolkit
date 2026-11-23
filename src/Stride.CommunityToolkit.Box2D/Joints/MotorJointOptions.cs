using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Drives body B relative to body A: a velocity to chase with capped force and torque, and a
/// spring toward the frames' alignment. The mouse drag idiom, and the way to move a body without
/// making it kinematic.
/// </summary>
public sealed record MotorJointOptions : JointOptionsBase
{
    /// <summary>Relative linear velocity to drive, in metres per second.</summary>
    public Vector2? LinearVelocity { get; init; }

    /// <summary>The most force the velocity drive may apply, in newtons.</summary>
    public float? MaxVelocityForce { get; init; }

    /// <summary>Relative angular velocity to drive, in radians per second.</summary>
    public float? AngularVelocity { get; init; }

    /// <summary>The most torque the velocity drive may apply, in newton-metres.</summary>
    public float? MaxVelocityTorque { get; init; }

    /// <summary>Linear spring stiffness toward the frames' alignment, in hertz.</summary>
    public float? LinearHertz { get; init; }

    /// <summary>Linear spring damping ratio.</summary>
    public float? LinearDampingRatio { get; init; }

    /// <summary>The most force the linear spring may apply, in newtons.</summary>
    public float? MaxSpringForce { get; init; }

    /// <summary>Angular spring stiffness, in hertz.</summary>
    public float? AngularHertz { get; init; }

    /// <summary>Angular spring damping ratio.</summary>
    public float? AngularDampingRatio { get; init; }

    /// <summary>The most torque the angular spring may apply, in newton-metres.</summary>
    public float? MaxSpringTorque { get; init; }
}
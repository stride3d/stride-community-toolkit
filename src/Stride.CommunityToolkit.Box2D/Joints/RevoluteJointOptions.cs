namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// A hinge: the bodies share a pivot and turn about it. Optionally sprung toward a target angle,
/// limited to an angular range, or driven by a motor.
/// </summary>
public sealed record RevoluteJointOptions : JointOptionsBase
{
    /// <summary>Angle the spring pulls toward, in radians from the pose at creation.</summary>
    public float? TargetAngle { get; init; }

    /// <summary>Turn the spring on.</summary>
    public bool? EnableSpring { get; init; }

    /// <summary>Spring stiffness, in hertz.</summary>
    public float? Hertz { get; init; }

    /// <summary>Spring damping ratio; 1 is critical.</summary>
    public float? DampingRatio { get; init; }

    /// <summary>Clamp the angle to <see cref="LowerAngle"/>..<see cref="UpperAngle"/>.</summary>
    public bool? EnableLimit { get; init; }

    /// <summary>Lower angular limit, in radians from the pose at creation.</summary>
    public float? LowerAngle { get; init; }

    /// <summary>Upper angular limit, in radians from the pose at creation.</summary>
    public float? UpperAngle { get; init; }

    /// <summary>Drive the hinge at <see cref="MotorSpeed"/>.</summary>
    public bool? EnableMotor { get; init; }

    /// <summary>The most torque the motor may apply, in newton-metres.</summary>
    public float? MaxMotorTorque { get; init; }

    /// <summary>Motor speed, in radians per second.</summary>
    public float? MotorSpeed { get; init; }
}
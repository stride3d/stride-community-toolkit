namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// A wheel on a suspension: the wheel turns freely about the pivot and slides along the axis,
/// usually sprung and limited, with a motor to drive it.
/// </summary>
public sealed record WheelJointOptions : JointOptionsBase
{
    /// <summary>Turn the suspension spring on.</summary>
    public bool? EnableSpring { get; init; }

    /// <summary>Suspension stiffness, in hertz.</summary>
    public float? Hertz { get; init; }

    /// <summary>Suspension damping ratio; 1 is critical.</summary>
    public float? DampingRatio { get; init; }

    /// <summary>Clamp the suspension travel to <see cref="LowerTranslation"/>..<see cref="UpperTranslation"/>.</summary>
    public bool? EnableLimit { get; init; }

    /// <summary>Lower travel limit, in metres along the axis.</summary>
    public float? LowerTranslation { get; init; }

    /// <summary>Upper travel limit, in metres along the axis.</summary>
    public float? UpperTranslation { get; init; }

    /// <summary>Drive the wheel at <see cref="MotorSpeed"/>.</summary>
    public bool? EnableMotor { get; init; }

    /// <summary>The most torque the motor may apply, in newton-metres.</summary>
    public float? MaxMotorTorque { get; init; }

    /// <summary>Motor speed, in radians per second.</summary>
    public float? MotorSpeed { get; init; }
}
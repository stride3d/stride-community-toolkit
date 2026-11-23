namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// A slider: body B moves along an axis fixed in body A, without turning relative to it.
/// </summary>
public sealed record PrismaticJointOptions : JointOptionsBase
{
    /// <summary>Turn the spring on.</summary>
    public bool? EnableSpring { get; init; }

    /// <summary>Spring stiffness, in hertz.</summary>
    public float? Hertz { get; init; }

    /// <summary>Spring damping ratio; 1 is critical.</summary>
    public float? DampingRatio { get; init; }

    /// <summary>Translation the spring pulls toward, in metres along the axis.</summary>
    public float? TargetTranslation { get; init; }

    /// <summary>Clamp the translation to <see cref="LowerTranslation"/>..<see cref="UpperTranslation"/>.</summary>
    public bool? EnableLimit { get; init; }

    /// <summary>Lower translation limit, in metres along the axis.</summary>
    public float? LowerTranslation { get; init; }

    /// <summary>Upper translation limit, in metres along the axis.</summary>
    public float? UpperTranslation { get; init; }

    /// <summary>Drive the slider at <see cref="MotorSpeed"/>.</summary>
    public bool? EnableMotor { get; init; }

    /// <summary>The most force the motor may apply, in newtons.</summary>
    public float? MaxMotorForce { get; init; }

    /// <summary>Motor speed, in metres per second.</summary>
    public float? MotorSpeed { get; init; }
}
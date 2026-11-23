namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// A rod or a rope: keeps two anchors a set distance apart, or within a range, optionally sprung
/// or driven along its length.
/// </summary>
public sealed record DistanceJointOptions : JointOptionsBase
{
    /// <summary>Rest length, in metres. Defaults to the distance between the anchors at creation.</summary>
    public float? Length { get; init; }

    /// <summary>Turn the spring on; the joint then behaves as a soft rod rather than a rigid one.</summary>
    public bool? EnableSpring { get; init; }

    /// <summary>Spring stiffness, in hertz.</summary>
    public float? Hertz { get; init; }

    /// <summary>Spring damping ratio; 1 is critical.</summary>
    public float? DampingRatio { get; init; }

    /// <summary>Lowest force the spring may apply, in newtons; negative pulls.</summary>
    public float? LowerSpringForce { get; init; }

    /// <summary>Highest force the spring may apply, in newtons.</summary>
    public float? UpperSpringForce { get; init; }

    /// <summary>Clamp the length to <see cref="MinLength"/>..<see cref="MaxLength"/> - a rope when the spring is on.</summary>
    public bool? EnableLimit { get; init; }

    /// <summary>Shortest allowed length, in metres.</summary>
    public float? MinLength { get; init; }

    /// <summary>Longest allowed length, in metres.</summary>
    public float? MaxLength { get; init; }

    /// <summary>Drive the length at <see cref="MotorSpeed"/>.</summary>
    public bool? EnableMotor { get; init; }

    /// <summary>The most force the motor may apply, in newtons.</summary>
    public float? MaxMotorForce { get; init; }

    /// <summary>Motor speed, in metres per second.</summary>
    public float? MotorSpeed { get; init; }
}
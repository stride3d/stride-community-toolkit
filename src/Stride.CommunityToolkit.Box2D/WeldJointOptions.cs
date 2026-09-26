namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Glues two bodies together. Zero hertz is rigid; a positive value makes the weld springy on
/// that axis, which is how breakable or soft assemblies are built.
/// </summary>
public sealed record WeldJointOptions : JointOptionsBase
{
    /// <summary>Linear spring stiffness, in hertz; 0 is rigid.</summary>
    public float? LinearHertz { get; init; }

    /// <summary>Angular spring stiffness, in hertz; 0 is rigid.</summary>
    public float? AngularHertz { get; init; }

    /// <summary>Linear damping ratio.</summary>
    public float? LinearDampingRatio { get; init; }

    /// <summary>Angular damping ratio.</summary>
    public float? AngularDampingRatio { get; init; }
}
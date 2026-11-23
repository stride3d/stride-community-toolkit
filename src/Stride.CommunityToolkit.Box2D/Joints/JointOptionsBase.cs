namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// The settings every Box2D joint shares. A property left <see langword="null"/> keeps Box2D's
/// default for that joint type, so an empty options object is the same as passing none.
/// </summary>
public abstract record JointOptionsBase
{
    /// <summary>Whether the two bodies still collide with each other. Box2D default: no.</summary>
    public bool? CollideConnected { get; init; }

    /// <summary>Force above which the joint raises a joint event, in newtons. Box2D default: never.</summary>
    public float? ForceThreshold { get; init; }

    /// <summary>Torque above which the joint raises a joint event, in newton-metres. Box2D default: never.</summary>
    public float? TorqueThreshold { get; init; }
}
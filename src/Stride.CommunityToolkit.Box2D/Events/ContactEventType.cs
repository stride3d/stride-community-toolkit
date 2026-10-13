namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// The kind of contact event carried by a <see cref="ContactEventData"/>.
/// </summary>
public enum ContactEventType
{
    /// <summary>Two shapes started touching.</summary>
    BeginTouch,

    /// <summary>Two shapes stopped touching.</summary>
    EndTouch,

    /// <summary>Two shapes collided with enough speed to register an impact.</summary>
    Hit
}
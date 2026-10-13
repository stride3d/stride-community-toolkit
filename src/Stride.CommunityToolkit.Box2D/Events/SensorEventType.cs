namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// The kind of sensor event carried by a <see cref="SensorEventData"/>.
/// </summary>
public enum SensorEventType
{
    /// <summary>A shape entered the sensor.</summary>
    BeginTouch,

    /// <summary>A shape left the sensor.</summary>
    EndTouch
}
using Box2D.NET;

namespace Example18_Box2DPhysics.Reusable.Events;

/// <summary>
/// Sensor event types (extraction candidate). Separate from current example names to prevent collisions.
/// </summary>
public enum SensorEventType2D
{
    BeginTouch,
    EndTouch
}

/// <summary>
/// Data describing a sensor overlap event.
/// </summary>
public readonly record struct SensorEventData2D(
    SensorEventType2D Type,
    B2ShapeId SensorShape,
    B2ShapeId VisitorShape);

/// <summary>
/// Interface for systems interested in sensor events.
/// </summary>
public interface ISensorEventHandler2D
{
    void OnSensor(SensorEventData2D evt);
}

using Box2D.NET;

namespace Example18_Box2DPhysics.Reusable.Events;

/// <summary>
/// Contact event types (extraction candidate). Duplicates names found in the current example but
/// lives in a different namespace to avoid clashes until migration completes.
/// </summary>
public enum ContactEventType2D
{
    BeginTouch,
    EndTouch,
    Hit
}

/// <summary>
/// Data describing a single contact event. (Will merge with existing structs during migration.)
/// </summary>
public readonly record struct ContactEventData2D(
    ContactEventType2D Type,
    B2ShapeId ShapeIdA,
    B2ShapeId ShapeIdB)
{
    // TODO: Add normal, point, speed once unified with original example implementation.
}

/// <summary>
/// Interface for systems interested in contact events.
/// </summary>
public interface IContactEventHandler2D
{
    void OnContact(ContactEventData2D evt);
}
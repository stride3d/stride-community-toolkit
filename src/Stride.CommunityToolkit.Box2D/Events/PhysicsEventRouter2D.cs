using Box2D.NET;
using Stride.Core.Mathematics;
using Stride.Engine;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Routes Box2D contact and sensor events to registered handlers. Delegates resolution of bodies
/// to Stride entities via a provided callback so it remains agnostic of the mapping implementation.
/// </summary>
public sealed class PhysicsEventRouter2D
{
    private readonly List<IContactEventHandler> _contactHandlers = new();
    private readonly List<ISensorEventHandler> _sensorHandlers = new();

    /// <summary>Registers a contact event handler; a handler is only registered once.</summary>
    /// <param name="handler">The handler to receive contact events.</param>
    public void RegisterContactEventHandler(IContactEventHandler handler)
    {
        if (!_contactHandlers.Contains(handler)) _contactHandlers.Add(handler);
    }

    /// <summary>Unregisters a contact event handler.</summary>
    /// <param name="handler">The handler to remove.</param>
    public void UnregisterContactEventHandler(IContactEventHandler handler) => _contactHandlers.Remove(handler);

    /// <summary>Registers a sensor event handler; a handler is only registered once.</summary>
    /// <param name="handler">The handler to receive sensor events.</param>
    public void RegisterSensorEventHandler(ISensorEventHandler handler)
    {
        if (!_sensorHandlers.Contains(handler)) _sensorHandlers.Add(handler);
    }

    /// <summary>Unregisters a sensor event handler.</summary>
    /// <param name="handler">The handler to remove.</param>
    public void UnregisterSensorEventHandler(ISensorEventHandler handler) => _sensorHandlers.Remove(handler);

    /// <summary>
    /// Reads this step's contact events from the world and dispatches them to registered handlers.
    /// </summary>
    /// <param name="worldId">The world to read events from.</param>
    /// <param name="entityResolver">Maps a body id to its entity; events with unresolved entities are dropped.</param>
    /// <param name="enableContactEvents">Whether begin/end touch events are dispatched.</param>
    /// <param name="enableHitEvents">Whether hit (impact) events are dispatched.</param>
    public void ProcessContacts(
        B2WorldId worldId,
        Func<B2BodyId, Entity?> entityResolver,
        bool enableContactEvents,
        bool enableHitEvents)
    {
        if ((!enableContactEvents && !enableHitEvents) || _contactHandlers.Count == 0) return;

        var contactEvents = b2World_GetContactEvents(worldId);

        if (enableContactEvents)
        {
            // Begin touch
            for (int i = 0; i < contactEvents.beginCount; i++)
            {
                ref var evt = ref contactEvents.beginEvents[i];
                DispatchContactEvent(entityResolver, evt.shapeIdA, evt.shapeIdB, ContactEventType.BeginTouch, Vector2.Zero, Vector2.Zero, 0f);
            }
            // End touch
            for (int i = 0; i < contactEvents.endCount; i++)
            {
                ref var evt = ref contactEvents.endEvents[i];
                DispatchContactEvent(entityResolver, evt.shapeIdA, evt.shapeIdB, ContactEventType.EndTouch, Vector2.Zero, Vector2.Zero, 0f);
            }
        }

        if (enableHitEvents)
        {
            for (int i = 0; i < contactEvents.hitCount; i++)
            {
                ref var evt = ref contactEvents.hitEvents[i];
                DispatchContactEvent(entityResolver,
                    evt.shapeIdA,
                    evt.shapeIdB,
                    ContactEventType.Hit,
                    new Vector2(evt.point.X, evt.point.Y),
                    new Vector2(evt.normal.X, evt.normal.Y),
                    evt.approachSpeed);
            }
        }
    }

    /// <summary>
    /// Reads this step's sensor events from the world and dispatches them to registered handlers.
    /// </summary>
    /// <param name="worldId">The world to read events from.</param>
    /// <param name="entityResolver">Maps a body id to its entity; events with unresolved entities are dropped.</param>
    /// <param name="enableSensorEvents">Whether sensor events are dispatched.</param>
    public void ProcessSensors(
        B2WorldId worldId,
        Func<B2BodyId, Entity?> entityResolver,
        bool enableSensorEvents)
    {
        if (!enableSensorEvents || _sensorHandlers.Count == 0) return;

        var sensorEvents = b2World_GetSensorEvents(worldId);

        for (int i = 0; i < sensorEvents.beginCount; i++)
        {
            ref var evt = ref sensorEvents.beginEvents[i];
            DispatchSensorEvent(entityResolver, evt.sensorShapeId, evt.visitorShapeId, SensorEventType.BeginTouch);
        }
        for (int i = 0; i < sensorEvents.endCount; i++)
        {
            ref var evt = ref sensorEvents.endEvents[i];
            DispatchSensorEvent(entityResolver, evt.sensorShapeId, evt.visitorShapeId, SensorEventType.EndTouch);
        }
    }

    private void DispatchContactEvent(
        Func<B2BodyId, Entity?> entityResolver,
        B2ShapeId shapeIdA,
        B2ShapeId shapeIdB,
        ContactEventType type,
        Vector2 point,
        Vector2 normal,
        float approachSpeed)
    {
        var entityA = entityResolver(b2Shape_GetBody(shapeIdA));
        var entityB = entityResolver(b2Shape_GetBody(shapeIdB));

        if (entityA == null || entityB == null) return;

        var data = new ContactEventData(type, entityA, entityB, shapeIdA, shapeIdB, point, normal, approachSpeed);

        foreach (var handler in _contactHandlers) handler.OnContactEvent(data);
    }

    private void DispatchSensorEvent(
        Func<B2BodyId, Entity?> entityResolver,
        B2ShapeId sensorShape,
        B2ShapeId visitorShape,
        SensorEventType type)
    {
        var sensorEntity = entityResolver(b2Shape_GetBody(sensorShape));
        var visitorEntity = entityResolver(b2Shape_GetBody(visitorShape));

        if (sensorEntity == null || visitorEntity == null) return;

        var data = new SensorEventData(type, sensorEntity, visitorEntity, sensorShape, visitorShape);

        foreach (var handler in _sensorHandlers) handler.OnSensorEvent(data);
    }
}
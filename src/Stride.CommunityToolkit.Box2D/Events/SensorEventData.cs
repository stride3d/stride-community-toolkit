using Box2D.NET;
using Stride.Engine;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// A sensor overlap event delivered to <see cref="ISensorEventHandler"/> implementations.
/// </summary>
/// <param name="Type">The kind of sensor event.</param>
/// <param name="SensorEntity">The entity owning the sensor shape.</param>
/// <param name="VisitorEntity">The entity that entered or left the sensor.</param>
/// <param name="SensorShapeId">The sensor shape.</param>
/// <param name="VisitorShapeId">The visiting shape.</param>
public readonly record struct SensorEventData(
    SensorEventType Type,
    Entity SensorEntity,
    Entity VisitorEntity,
    B2ShapeId SensorShapeId,
    B2ShapeId VisitorShapeId);
using Box2D.NET;
using Stride.Engine;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// A sensor overlap event delivered to <see cref="ISensorEventHandler"/> implementations.
/// </summary>
/// <remarks>
/// A body made without an entity has a <see langword="null"/> entity; its side is still identified by
/// <see cref="SensorBodyId"/> or <see cref="VisitorBodyId"/> and the shape id.
/// </remarks>
/// <param name="Type">The kind of sensor event.</param>
/// <param name="SensorEntity">The entity owning the sensor shape, or <see langword="null"/> when its body has none.</param>
/// <param name="VisitorEntity">The entity that entered or left the sensor, or <see langword="null"/> when its body has none.</param>
/// <param name="SensorShapeId">The sensor shape.</param>
/// <param name="VisitorShapeId">The visiting shape.</param>
public readonly record struct SensorEventData(
    SensorEventType Type,
    Entity? SensorEntity,
    Entity? VisitorEntity,
    B2ShapeId SensorShapeId,
    B2ShapeId VisitorShapeId)
{
    /// <summary>The body the sensor shape belongs to; the default id when the shape was destroyed in the step.</summary>
    public B2BodyId SensorBodyId => b2Shape_IsValid(SensorShapeId) ? b2Shape_GetBody(SensorShapeId) : default;

    /// <summary>The body the visiting shape belongs to; the default id when an end event names a shape destroyed in the step.</summary>
    public B2BodyId VisitorBodyId => b2Shape_IsValid(VisitorShapeId) ? b2Shape_GetBody(VisitorShapeId) : default;
}
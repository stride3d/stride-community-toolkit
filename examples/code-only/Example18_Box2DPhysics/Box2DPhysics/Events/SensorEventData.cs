using Box2D.NET;
using Stride.Engine;

namespace Example18_Box2DPhysics.Box2DPhysics.Events;

public struct SensorEventData
{
    public SensorEventType Type;
    public Entity SensorEntity;
    public Entity VisitorEntity;
    public B2ShapeId SensorShapeId;
    public B2ShapeId VisitorShapeId;
}
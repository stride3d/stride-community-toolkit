using Box2D.NET;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example18_Box2DPhysics;

// These are the improvements that should be added to Box2DSimulation.cs
// They are separated here to avoid compilation issues with the example

#region Contact Event System

public interface IContactEventHandler
{
    void OnContactEvent(ContactEventData eventData);
}

public interface ISensorEventHandler
{
    void OnSensorEvent(SensorEventData eventData);
}

public enum ContactEventType
{
    BeginTouch,
    EndTouch,
    Hit
}

public enum SensorEventType
{
    BeginTouch,
    EndTouch
}

public struct ContactEventData
{
    public ContactEventType Type;
    public Entity EntityA;
    public Entity EntityB;
    public B2ShapeId ShapeIdA;
    public B2ShapeId ShapeIdB;
    public int ContactId; // Using int instead of B2ContactId for now
    public Vector2 Point;
    public Vector2 Normal;
    public float ApproachSpeed;
}

public struct SensorEventData
{
    public SensorEventType Type;
    public Entity SensorEntity;
    public Entity VisitorEntity;
    public B2ShapeId SensorShapeId;
    public B2ShapeId VisitorShapeId;
}

#endregion

#region Simulation Update System

public interface ISimulationUpdate
{
    void SimulationUpdate(Box2DSimulation simulation, float deltaTime);
    void AfterSimulationUpdate(Box2DSimulation simulation, float deltaTime);
}

#endregion

#region Collision Filtering

/// <summary>
/// Simple collision matrix for filtering collisions between different groups
/// </summary>
public class CollisionMatrix
{
    private readonly Dictionary<(int groupA, int groupB), bool> _collisionTable = new();

    public void SetCollision(int groupA, int groupB, bool canCollide)
    {
        _collisionTable[(Math.Min(groupA, groupB), Math.Max(groupA, groupB))] = canCollide;
    }

    public bool CanCollide(int groupA, int groupB)
    {
        var key = (Math.Min(groupA, groupB), Math.Max(groupA, groupB));
        return _collisionTable.TryGetValue(key, out var canCollide) ? canCollide : true; // Default to true
    }
}

#endregion
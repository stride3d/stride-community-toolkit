using Box2D.NET;
using Stride.Core.Mathematics;
using Stride.Engine;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Example18_Box2DPhysics;

public class Box2DSimulation : IDisposable
{
    private B2WorldId _worldId;
    private readonly Dictionary<B2BodyId, Entity> _bodyToEntity = [];
    private readonly Dictionary<Entity, B2BodyId> _entityToBody = [];

    // Fixed timestep like Bepu
    private TimeSpan _fixedTimeStep = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
    private TimeSpan _remainingUpdateTime;
    private readonly int _subStepCount = 4;

    public bool Enabled { get; set; } = true;
    public float TimeScale { get; set; } = 1f;
    public int MaxStepsPerFrame { get; set; } = 3;

    public Box2DSimulation()
    {
        var worldDef = b2DefaultWorldDef();
        worldDef.gravity = new B2Vec2(0.0f, -10.0f);
        _worldId = b2CreateWorld(ref worldDef);
    }

    public B2BodyId CreateDynamicBody(Entity entity, Vector3 position)
    {
        var bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Vec2(position.X, position.Y);

        var bodyId = b2CreateBody(_worldId, ref bodyDef);

        // Register the mapping
        _bodyToEntity[bodyId] = entity;
        _entityToBody[entity] = bodyId;

        return bodyId;
    }

    public void Update(TimeSpan elapsed)
    {
        if (!Enabled) return;

        _remainingUpdateTime += TimeScale == 1f ? elapsed : elapsed * TimeScale;

        for (int stepCount = 0; _remainingUpdateTime >= _fixedTimeStep && stepCount < MaxStepsPerFrame;
             stepCount++, _remainingUpdateTime -= _fixedTimeStep)
        {
            float timeStep = (float)_fixedTimeStep.TotalSeconds;

            // Step the Box2D world
            b2World_Step(_worldId, timeStep, _subStepCount);

            // Sync transforms from physics to Stride entities
            SyncTransformsFromPhysics();
        }
    }

    private void SyncTransformsFromPhysics()
    {
        foreach (var kvp in _bodyToEntity)
        {
            var bodyId = kvp.Key;
            var entity = kvp.Value;

            var position = b2Body_GetPosition(bodyId);
            var rotation = b2Body_GetRotation(bodyId);

            // Update Stride entity transform
            entity.Transform.Position = new Vector3(position.X, position.Y, 0f);
            entity.Transform.Rotation = Quaternion.RotationZ(b2Rot_GetAngle(rotation));
        }
    }

    public void RemoveBody(Entity entity)
    {
        if (_entityToBody.TryGetValue(entity, out var bodyId))
        {
            b2DestroyBody(bodyId);
            _entityToBody.Remove(entity);
            _bodyToEntity.Remove(bodyId);
        }
    }

    public B2WorldId GetWorldId() => _worldId;

    public Entity? GetEntity(B2BodyId bodyId)
    {
        return _bodyToEntity.TryGetValue(bodyId, out var entity) ? entity : null;
    }

    public List<B2BodyId> GetAllBodyIds() => _bodyToEntity.Keys.ToList();

    /// <summary>
    /// Tests if a point overlaps with any physics shape in the world.
    /// </summary>
    /// <param name="point">The point to test</param>
    /// <param name="querySize">Half-extent of the query box around the point</param>
    /// <returns>The body ID that was hit, or null if nothing was hit</returns>
    public B2BodyId? OverlapPoint(Vector2 point, float querySize = 0.1f)
    {
        // Create a small AABB (Axis-Aligned Bounding Box) around the clicked point
        var lower = new B2Vec2(point.X - querySize, point.Y - querySize);
        var upper = new B2Vec2(point.X + querySize, point.Y + querySize);
        var box = new B2AABB { lowerBound = lower, upperBound = upper };

        // Store the result of the query
        B2BodyId? hitBodyId = null;

        // Perform the overlap query
        b2World_OverlapAABB(_worldId, box, b2DefaultQueryFilter(), QueryCallback, null);

        return hitBodyId;

        // Function to be called for each shape that overlaps the AABB
        bool QueryCallback(B2ShapeId shapeId, object userData)
        {
            var bodyId = b2Shape_GetBody(shapeId);

            // Test if the point is inside the shape
            bool overlap = b2Shape_TestPoint(shapeId, new B2Vec2(point.X, point.Y));

            if (overlap)
            {
                hitBodyId = bodyId;

                return false; // Stop the query, we found what we're looking for
            }

            return true; // Continue the query
        }
    }

    public void Dispose()
    {
        if (_worldId.index1 != 0) // Check if world is valid
        {
            b2DestroyWorld(_worldId);
        }
    }
}

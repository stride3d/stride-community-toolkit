using Box2D.NET;
using Stride.Core.Mathematics;
using Stride.Engine;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Distances;
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

    // Contact and sensor event system
    private readonly List<IContactEventHandler> _contactEventHandlers = [];
    private readonly List<ISensorEventHandler> _sensorEventHandlers = [];
    public bool EnableContactEvents { get; set; } = true;
    public bool EnableHitEvents { get; set; } = true;
    public bool EnableSensorEvents { get; set; } = true;

    // Physics properties
    public Vector2 Gravity
    {
        get
        {
            var gravity = b2World_GetGravity(_worldId);
            return new Vector2(gravity.X, gravity.Y);
        }
        set => b2World_SetGravity(_worldId, new B2Vec2(value.X, value.Y));
    }

    public void RegisterContactEventHandler(IContactEventHandler handler)
    {
        if (!_contactEventHandlers.Contains(handler))
            _contactEventHandlers.Add(handler);
    }

    public void UnregisterContactEventHandler(IContactEventHandler handler)
    {
        _contactEventHandlers.Remove(handler);
    }

    public void RegisterSensorEventHandler(ISensorEventHandler handler)
    {
        if (!_sensorEventHandlers.Contains(handler))
            _sensorEventHandlers.Add(handler);
    }

    public void UnregisterSensorEventHandler(ISensorEventHandler handler)
    {
        _sensorEventHandlers.Remove(handler);
    }

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

    public B2BodyId CreateKinematicBody(Entity entity, Vector3 position)
    {
        var bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_kinematicBody;
        bodyDef.position = new B2Vec2(position.X, position.Y);

        var bodyId = b2CreateBody(_worldId, ref bodyDef);

        _bodyToEntity[bodyId] = entity;
        _entityToBody[entity] = bodyId;

        return bodyId;
    }

    public B2BodyId CreateStaticBody(Entity entity, Vector3 position)
    {
        var bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_staticBody;
        bodyDef.position = new B2Vec2(position.X, position.Y);

        var bodyId = b2CreateBody(_worldId, ref bodyDef);

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

            // Process contact and sensor events
            if (EnableContactEvents || EnableHitEvents)
                ProcessContactEvents();
            if (EnableSensorEvents)
                ProcessSensorEvents();

            // Sync transforms from physics to Stride entities
            SyncTransformsFromPhysics();
        }
    }

    private void ProcessContactEvents()
    {
        if (_contactEventHandlers.Count == 0) return;

        // Get contact events from Box2D.NET
        var contactEvents = b2World_GetContactEvents(_worldId);

        // Process begin touch events
        for (int i = 0; i < contactEvents.beginCount; i++)
        {
            ref var evt = ref contactEvents.beginEvents[i];
            var entityA = GetEntity(b2Shape_GetBody(evt.shapeIdA));
            var entityB = GetEntity(b2Shape_GetBody(evt.shapeIdB));

            if (entityA != null && entityB != null)
            {
                var eventData = new ContactEventData
                {
                    Type = ContactEventType.BeginTouch,
                    EntityA = entityA,
                    EntityB = entityB,
                    ShapeIdA = evt.shapeIdA,
                    ShapeIdB = evt.shapeIdB,
                    Point = Vector2.Zero, // Contact events don't have point data
                    Normal = Vector2.Zero,
                    ApproachSpeed = 0f
                };

                foreach (var handler in _contactEventHandlers)
                {
                    handler.OnContactEvent(eventData);
                }
            }
        }

        // Process end touch events
        for (int i = 0; i < contactEvents.endCount; i++)
        {
            ref var evt = ref contactEvents.endEvents[i];
            var entityA = GetEntity(b2Shape_GetBody(evt.shapeIdA));
            var entityB = GetEntity(b2Shape_GetBody(evt.shapeIdB));

            if (entityA != null && entityB != null)
            {
                var eventData = new ContactEventData
                {
                    Type = ContactEventType.EndTouch,
                    EntityA = entityA,
                    EntityB = entityB,
                    ShapeIdA = evt.shapeIdA,
                    ShapeIdB = evt.shapeIdB,
                    Point = Vector2.Zero,
                    Normal = Vector2.Zero,
                    ApproachSpeed = 0f
                };

                foreach (var handler in _contactEventHandlers)
                {
                    handler.OnContactEvent(eventData);
                }
            }
        }

        // Process hit events
        for (int i = 0; i < contactEvents.hitCount; i++)
        {
            ref var evt = ref contactEvents.hitEvents[i];
            var entityA = GetEntity(b2Shape_GetBody(evt.shapeIdA));
            var entityB = GetEntity(b2Shape_GetBody(evt.shapeIdB));

            if (entityA != null && entityB != null)
            {
                var eventData = new ContactEventData
                {
                    Type = ContactEventType.Hit,
                    EntityA = entityA,
                    EntityB = entityB,
                    ShapeIdA = evt.shapeIdA,
                    ShapeIdB = evt.shapeIdB,
                    Point = new Vector2(evt.point.X, evt.point.Y),
                    Normal = new Vector2(evt.normal.X, evt.normal.Y),
                    ApproachSpeed = evt.approachSpeed
                };

                foreach (var handler in _contactEventHandlers)
                {
                    handler.OnContactEvent(eventData);
                }
            }
        }
    }

    private void ProcessSensorEvents()
    {
        if (_sensorEventHandlers.Count == 0) return;

        // Get sensor events from Box2D.NET
        var sensorEvents = b2World_GetSensorEvents(_worldId);

        // Process sensor begin events
        for (int i = 0; i < sensorEvents.beginCount; i++)
        {
            ref var evt = ref sensorEvents.beginEvents[i];
            var sensorEntity = GetEntity(b2Shape_GetBody(evt.sensorShapeId));
            var visitorEntity = GetEntity(b2Shape_GetBody(evt.visitorShapeId));

            if (sensorEntity != null && visitorEntity != null)
            {
                var eventData = new SensorEventData
                {
                    Type = SensorEventType.BeginTouch,
                    SensorEntity = sensorEntity,
                    VisitorEntity = visitorEntity,
                    SensorShapeId = evt.sensorShapeId,
                    VisitorShapeId = evt.visitorShapeId
                };

                foreach (var handler in _sensorEventHandlers)
                {
                    handler.OnSensorEvent(eventData);
                }
            }
        }

        // Process sensor end events
        for (int i = 0; i < sensorEvents.endCount; i++)
        {
            ref var evt = ref sensorEvents.endEvents[i];
            var sensorEntity = GetEntity(b2Shape_GetBody(evt.sensorShapeId));
            var visitorEntity = GetEntity(b2Shape_GetBody(evt.visitorShapeId));

            if (sensorEntity != null && visitorEntity != null)
            {
                var eventData = new SensorEventData
                {
                    Type = SensorEventType.EndTouch,
                    SensorEntity = sensorEntity,
                    VisitorEntity = visitorEntity,
                    SensorShapeId = evt.sensorShapeId,
                    VisitorShapeId = evt.visitorShapeId
                };

                foreach (var handler in _sensorEventHandlers)
                {
                    handler.OnSensorEvent(eventData);
                }
            }
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
        b2World_OverlapAABB(_worldId, box, b2DefaultQueryFilter(), (shapeId, userData) =>
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
        }, null);

        return hitBodyId;
    }

    /// <summary>
    /// Performs a raycast and returns the first hit
    /// </summary>
    /// <param name="origin">Ray origin</param>
    /// <param name="direction">Ray direction (normalized)</param>
    /// <param name="maxDistance">Maximum distance to cast</param>
    /// <returns>First hit or null if no hit</returns>
    public RaycastHit? Raycast(Vector2 origin, Vector2 direction, float maxDistance)
    {
        var start = new B2Vec2(origin.X, origin.Y);
        var translation = new B2Vec2(direction.X * maxDistance, direction.Y * maxDistance);

        var result = b2World_CastRayClosest(_worldId, start, translation, b2DefaultQueryFilter());

        if (result.hit)
        {
            var bodyId = b2Shape_GetBody(result.shapeId);

            return new RaycastHit
            {
                Entity = GetEntity(bodyId),
                BodyId = bodyId,
                ShapeId = result.shapeId,
                Point = new Vector2(result.point.X, result.point.Y),
                Normal = new Vector2(result.normal.X, result.normal.Y),
                Distance = maxDistance * result.fraction,
                Fraction = result.fraction
            };
        }

        return null;
    }

    /// <summary>
    /// Performs a raycast and returns all hits along the ray
    /// </summary>
    /// <param name="origin">Ray origin</param>
    /// <param name="direction">Ray direction (normalized)</param>
    /// <param name="maxDistance">Maximum distance to cast</param>
    /// <returns>List of all hits along the ray</returns>
    public List<RaycastHit> RaycastAll(Vector2 origin, Vector2 direction, float maxDistance)
    {
        var hits = new List<RaycastHit>();
        var start = new B2Vec2(origin.X, origin.Y);
        var translation = new B2Vec2(direction.X * maxDistance, direction.Y * maxDistance);

        // For multiple hits, we need to use the callback version
        b2World_CastRay(_worldId, start, translation, b2DefaultQueryFilter(),
            (shapeId, point, normal, fraction, userData) =>
            {
                var bodyId = b2Shape_GetBody(shapeId);
                hits.Add(new RaycastHit
                {
                    Entity = GetEntity(bodyId),
                    BodyId = bodyId,
                    ShapeId = shapeId,
                    Point = new Vector2(point.X, point.Y),
                    Normal = new Vector2(normal.X, normal.Y),
                    Distance = maxDistance * fraction,
                    Fraction = fraction
                });
                return 1.0f; // Continue collecting all hits
            }, null);

        return hits;
    }

    /// <summary>
    /// Overlaps an AABB and returns all bodies within it
    /// </summary>
    public List<B2BodyId> OverlapAABB(Vector2 lowerBound, Vector2 upperBound)
    {
        var bodies = new List<B2BodyId>();
        var box = new B2AABB { lowerBound = new B2Vec2(lowerBound.X, lowerBound.Y), upperBound = new B2Vec2(upperBound.X, upperBound.Y) };

        b2World_OverlapAABB(_worldId, box, b2DefaultQueryFilter(), (shapeId, userData) =>
        {
            var bodyId = b2Shape_GetBody(shapeId);
            if (!bodies.Contains(bodyId))
            {
                bodies.Add(bodyId);
            }

            return true;
        }, null);

        return bodies;
    }

    /// <summary>
    /// Overlaps a circle and returns all bodies within it
    /// </summary>
    public List<B2BodyId> OverlapCircle(Vector2 center, float radius)
    {
        var bodies = new List<B2BodyId>();
        var circle = new B2Circle(new B2Vec2(center.X, center.Y), radius);
        var proxy = b2MakeProxy(circle.center, 1, circle.radius);

        b2World_OverlapShape(_worldId, ref proxy, b2DefaultQueryFilter(),
            (shapeId, userData) =>
            {
                var bodyId = b2Shape_GetBody(shapeId);
                if (!bodies.Contains(bodyId))
                {
                    bodies.Add(bodyId);
                }
                return true;
            }, null);

        return bodies;
    }

    public void Dispose()
    {
        if (_worldId.index1 != 0) // Check if world is valid
        {
            b2DestroyWorld(_worldId);
        }
    }
}

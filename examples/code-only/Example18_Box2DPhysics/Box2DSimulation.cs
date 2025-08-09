using Box2D.NET;
using Example18_Box2DPhysics.Reusable.Core; // new reusable core wrappers
using Example18_Box2DPhysics.Reusable.Events; // event router extraction
using Example18_Box2DPhysics.Reusable.Queries; // for PhysicsQueries2D extraction
using Stride.Core.Mathematics;
using Stride.Engine;
using static Box2D.NET.B2Worlds;

namespace Example18_Box2DPhysics;

public class Box2DSimulation : IDisposable
{
    // Underlying reusable world + bridge (extraction in progress)
    private readonly PhysicsWorld2D _world;
    private readonly Box2DStrideBridge _bridge;

    // Stepping configuration now owned directly by PhysicsWorld2D
    public bool Enabled { get; set; } = true;
    public float TimeScale { get => _world.TimeScale; set => _world.TimeScale = value; }
    public int MaxStepsPerFrame { get => _world.MaxStepsPerFrame; set => _world.MaxStepsPerFrame = value; }
    public int TargetHz { get => _world.TargetHz; set => _world.TargetHz = value; }

    // Contact and sensor event system (delegated to router)
    private readonly PhysicsEventRouter2D _eventRouter = new();
    public bool EnableContactEvents { get; set; } = true;
    public bool EnableHitEvents { get; set; } = true;
    public bool EnableSensorEvents { get; set; } = true;

    public Vector2 Gravity
    {
        get
        {
            var gravity = b2World_GetGravity(_world.WorldId);
            return new Vector2(gravity.X, gravity.Y);
        }
        set => b2World_SetGravity(_world.WorldId, new B2Vec2(value.X, value.Y));
    }

    public void RegisterContactEventHandler(IContactEventHandler handler) => _eventRouter.RegisterContactEventHandler(handler); // preserved API
    public void UnregisterContactEventHandler(IContactEventHandler handler) => _eventRouter.UnregisterContactEventHandler(handler);
    public void RegisterSensorEventHandler(ISensorEventHandler handler) => _eventRouter.RegisterSensorEventHandler(handler);
    public void UnregisterSensorEventHandler(ISensorEventHandler handler) => _eventRouter.UnregisterSensorEventHandler(handler);

    public Box2DSimulation()
    {
        _world = new PhysicsWorld2D();
        _bridge = new Box2DStrideBridge(_world);
    }

    public B2BodyId CreateDynamicBody(Entity entity, Vector3 position) => _bridge.CreateBody(entity, position, B2BodyType.b2_dynamicBody);
    public B2BodyId CreateKinematicBody(Entity entity, Vector3 position) => _bridge.CreateBody(entity, position, B2BodyType.b2_kinematicBody);
    public B2BodyId CreateStaticBody(Entity entity, Vector3 position) => _bridge.CreateBody(entity, position, B2BodyType.b2_staticBody);

    public void Update(TimeSpan elapsed)
    {
        if (!Enabled) return;

        // Delegate fixed-step accumulation to PhysicsWorld2D; process events per fixed step
        _world.Step((float)elapsed.TotalSeconds, _ =>
        {
            if (EnableContactEvents || EnableHitEvents)
                ProcessContactEvents();
            if (EnableSensorEvents)
                ProcessSensorEvents();
            _bridge.SyncTransformsFromPhysics();
        });
    }

    private void ProcessContactEvents() => _eventRouter.ProcessContacts(_world.WorldId, id => GetEntity(id), EnableContactEvents, EnableHitEvents);

    private void ProcessSensorEvents() => _eventRouter.ProcessSensors(_world.WorldId, id => GetEntity(id), EnableSensorEvents);

    public void RemoveBody(Entity entity)
    {
        _bridge.RemoveBody(entity);
    }

    public B2WorldId GetWorldId() => _world.WorldId;

    public Entity? GetEntity(B2BodyId bodyId)
    {
        return _bridge.GetEntity(bodyId);
    }

    public List<B2BodyId> GetAllBodyIds() => _bridge.Bodies.ToList();

    /// <summary>
    /// Tests if a point overlaps with any physics shape in the world.
    /// </summary>
    /// <param name="point">The point to test</param>
    /// <param name="querySize">Half-extent of the query box around the point</param>
    /// <returns>The body ID that was hit, or null if nothing was hit</returns>
    public B2BodyId? OverlapPoint(Vector2 point, float querySize = 0.1f)
        => PhysicsQueries2D.OverlapPoint(_world.WorldId, point, querySize);

    /// <summary>
    /// Performs a raycast and returns the first hit
    /// </summary>
    /// <param name="origin">Ray origin</param>
    /// <param name="direction">Ray direction (normalized)</param>
    /// <param name="maxDistance">Maximum distance to cast</param>
    /// <returns>First hit or null if no hit</returns>
    public RaycastHit? Raycast(Vector2 origin, Vector2 direction, float maxDistance)
    {
        // Delegated to generic query helper
        var hitResult = PhysicsQueries2D.RaycastClosest(_world.WorldId, origin, direction, maxDistance);

        if (!hitResult.hit) return null;

        return new RaycastHit
        {
            Entity = GetEntity(hitResult.bodyId),
            BodyId = hitResult.bodyId,
            ShapeId = hitResult.shapeId,
            Point = hitResult.point,
            Normal = hitResult.normal,
            Distance = maxDistance * hitResult.fraction,
            Fraction = hitResult.fraction
        };
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
        var rawHits = PhysicsQueries2D.RaycastAll(_world.WorldId, origin, direction, maxDistance);
        var converted = new List<RaycastHit>(rawHits.Count);

        foreach (var h in rawHits)
        {
            converted.Add(new RaycastHit
            {
                Entity = GetEntity(h.BodyId),
                BodyId = h.BodyId,
                ShapeId = h.ShapeId,
                Point = h.Point,
                Normal = h.Normal,
                Distance = maxDistance * h.Fraction,
                Fraction = h.Fraction
            });
        }

        return converted;
    }

    /// <summary>
    /// Overlaps an AABB and returns all bodies within it
    /// </summary>
    public List<B2BodyId> OverlapAABB(Vector2 lowerBound, Vector2 upperBound)
        => PhysicsQueries2D.OverlapAABB(_world.WorldId, lowerBound, upperBound);

    /// <summary>
    /// Overlaps a circle and returns all bodies within it
    /// </summary>
    public List<B2BodyId> OverlapCircle(Vector2 center, float radius)
        => PhysicsQueries2D.OverlapCircle(_world.WorldId, center, radius);

    public void Dispose()
    {
        _world.Dispose();
    }
}
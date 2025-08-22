using Box2D.NET;
using Example18_Box2DPhysics.Box2DPhysics.Core; // new reusable core wrappers
using Example18_Box2DPhysics.Box2DPhysics.Events; // event router extraction
using Example18_Box2DPhysics.Box2DPhysics.Queries; // for PhysicsQueries2D extraction
using Stride.Core.Mathematics;
using Stride.Engine;
using static Box2D.NET.B2Worlds;

namespace Example18_Box2DPhysics.Box2DPhysics;

/// <summary>
/// High-level integration façade between Box2D.NET and Stride entities for the example.
/// Wraps a reusable <c>PhysicsWorld2D</c> plus an entity/body bridge and exposes
/// a simplified stepping, query and event API. Intended to guide future extraction
/// into a toolkit library (mirroring the design of BepuSimulation in Stride).
/// </summary>
public class Box2DSimulation : IDisposable
{
    // Underlying reusable world + bridge (extraction in progress)
    private readonly PhysicsWorld2D _world;
    private readonly Box2DStrideBridge _bridge;

    // Stepping configuration now owned directly by PhysicsWorld2D
    /// <summary>Whether simulation stepping and event processing should occur.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>Multiplier applied to incoming delta time before fixed-step accumulation.</summary>
    public float TimeScale { get => _world.TimeScale; set => _world.TimeScale = value; }
    /// <summary>Maximum number of fixed steps processed per frame (-1 for unlimited) to avoid spiral-of-death.</summary>
    public int MaxStepsPerFrame { get => _world.MaxStepsPerFrame; set => _world.MaxStepsPerFrame = value; }
    /// <summary>Target simulation frequency in hertz used to derive the fixed step size.</summary>
    public int TargetHz { get => _world.TargetHz; set => _world.TargetHz = value; }

    // Contact and sensor event system (delegated to router)
    private readonly PhysicsEventRouter2D _eventRouter = new();

    /// <summary>Enable dispatch of begin/end contact events.</summary>
    public bool EnableContactEvents { get; set; } = true;

    /// <summary>Enable dispatch of hit (post-solve / impact style) events.</summary>
    public bool EnableHitEvents { get; set; } = true;

    /// <summary>Enable dispatch of sensor overlap events.</summary>
    public bool EnableSensorEvents { get; set; } = true;

    /// <summary>World gravity applied to dynamic bodies.</summary>
    public Vector2 Gravity
    {
        get
        {
            var gravity = b2World_GetGravity(_world.WorldId);

            return new Vector2(gravity.X, gravity.Y);
        }

        set => b2World_SetGravity(_world.WorldId, new B2Vec2(value.X, value.Y));
    }

    /// <summary>Registers a contact event handler.</summary>
    public void RegisterContactEventHandler(IContactEventHandler handler) => _eventRouter.RegisterContactEventHandler(handler); // preserved API

    /// <summary>Unregisters a contact event handler.</summary>
    public void UnregisterContactEventHandler(IContactEventHandler handler) => _eventRouter.UnregisterContactEventHandler(handler);

    /// <summary>Registers a sensor event handler.</summary>
    public void RegisterSensorEventHandler(ISensorEventHandler handler) => _eventRouter.RegisterSensorEventHandler(handler);

    /// <summary>Unregisters a sensor event handler.</summary>
    public void UnregisterSensorEventHandler(ISensorEventHandler handler) => _eventRouter.UnregisterSensorEventHandler(handler);

    /// <summary>Creates a new simulation instance with a fresh Box2D world.</summary>
    public Box2DSimulation()
    {
        _world = new PhysicsWorld2D();
        _bridge = new Box2DStrideBridge(_world);
    }

    /// <summary>Creates a dynamic body associated with the given entity at a world position.</summary>
    public B2BodyId CreateDynamicBody(Entity entity, Vector3 position)
        => _bridge.CreateBody(entity, position, B2BodyType.b2_dynamicBody);

    public B2BodyId CreateDynamicBody(Vector3 position)
        => _bridge.CreateBody(position, B2BodyType.b2_dynamicBody);

    /// <summary>Creates a kinematic body associated with the given entity at a world position.</summary>
    public B2BodyId CreateKinematicBody(Entity entity, Vector3 position)
        => _bridge.CreateBody(entity, position, B2BodyType.b2_kinematicBody);

    /// <summary>Creates a static body associated with the given entity at a world position.</summary>
    public B2BodyId CreateStaticBody(Entity entity, Vector3 position)
        => _bridge.CreateBody(entity, position, B2BodyType.b2_staticBody);

    /// <summary>Advances the simulation by the elapsed real time, executing zero or more fixed steps.</summary>
    /// <param name="elapsed">Frame time delta.</param>
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

    /// <summary>Removes the body associated with <paramref name="entity"/> from the world if present.</summary>
    public void RemoveBody(Entity entity)
    {
        _bridge.RemoveBody(entity);
    }

    /// <summary>Gets the underlying Box2D world id.</summary>
    public B2WorldId GetWorldId() => _world.WorldId;

    /// <summary>Gets the entity previously registered for the given body id (or null).</summary>
    public Entity? GetEntity(B2BodyId bodyId)
    {
        return _bridge.GetEntity(bodyId);
    }

    /// <summary>Returns a snapshot list of all body ids tracked by the bridge.</summary>
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

    /// <summary>Disposes underlying world resources.</summary>
    public void Dispose() => _world.Dispose();
}
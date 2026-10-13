using Box2D.NET;
using Stride.Core.Mathematics;
using Stride.Engine;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Worlds;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// High-level integration facade between Box2D.NET and Stride entities. Wraps a reusable
/// <see cref="PhysicsWorld2D"/> plus an entity/body bridge and exposes a simplified stepping,
/// query and event API, mirroring the role BepuSimulation plays for Bepu physics in Stride.
/// </summary>
public sealed class Box2DSimulation : IDisposable
{
    private readonly PhysicsWorld2D _world;
    private readonly Box2DStrideBridge _bridge;

    /// <summary>Whether simulation stepping and event processing should occur.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Multiplier applied to incoming delta time before fixed-step accumulation.</summary>
    public float TimeScale { get => _world.TimeScale; set => _world.TimeScale = value; }

    /// <summary>Maximum number of fixed steps processed per frame, to avoid the spiral of death.</summary>
    public int MaxStepsPerFrame { get => _world.MaxStepsPerFrame; set => _world.MaxStepsPerFrame = value; }

    /// <summary>Target simulation frequency in hertz used to derive the fixed step size.</summary>
    public int TargetHz { get => _world.TargetHz; set => _world.TargetHz = value; }

    // Contact and sensor event system (delegated to the router)
    private readonly PhysicsEventRouter2D _eventRouter = new();

    // Per-fixed-step handlers (the IBox2DSimulationUpdate pattern)
    private readonly List<IBox2DSimulationUpdate> _simulationUpdates = new();

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

    /// <summary>
    /// Registers a handler whose <see cref="IBox2DSimulationUpdate.SimulationUpdate"/> runs before and
    /// <see cref="IBox2DSimulationUpdate.AfterSimulationUpdate"/> after every fixed step; a handler is only registered once.
    /// </summary>
    /// <param name="handler">The handler to invoke around each fixed step.</param>
    public void RegisterSimulationUpdate(IBox2DSimulationUpdate handler)
    {
        if (!_simulationUpdates.Contains(handler)) _simulationUpdates.Add(handler);
    }

    /// <summary>Unregisters a simulation update handler.</summary>
    /// <param name="handler">The handler to remove.</param>
    public void UnregisterSimulationUpdate(IBox2DSimulationUpdate handler) => _simulationUpdates.Remove(handler);

    /// <summary>Registers a contact event handler.</summary>
    /// <param name="handler">The handler to receive contact events.</param>
    public void RegisterContactEventHandler(IContactEventHandler handler) => _eventRouter.RegisterContactEventHandler(handler);

    /// <summary>Unregisters a contact event handler.</summary>
    /// <param name="handler">The handler to remove.</param>
    public void UnregisterContactEventHandler(IContactEventHandler handler) => _eventRouter.UnregisterContactEventHandler(handler);

    /// <summary>Registers a sensor event handler.</summary>
    /// <param name="handler">The handler to receive sensor events.</param>
    public void RegisterSensorEventHandler(ISensorEventHandler handler) => _eventRouter.RegisterSensorEventHandler(handler);

    /// <summary>Unregisters a sensor event handler.</summary>
    /// <param name="handler">The handler to remove.</param>
    public void UnregisterSensorEventHandler(ISensorEventHandler handler) => _eventRouter.UnregisterSensorEventHandler(handler);

    /// <summary>Creates a new simulation instance with a fresh Box2D world.</summary>
    /// <param name="settings">Optional stepping and threading configuration; defaults to <see cref="PhysicsStepSettings"/> defaults.</param>
    public Box2DSimulation(PhysicsStepSettings? settings = null)
    {
        _world = new PhysicsWorld2D(settings);
        _bridge = new Box2DStrideBridge(_world);
        Joints = new SimulationJoints2D(_world, _bridge);
    }

    /// <summary>
    /// Joint factories with world-space anchors, for body ids or the entities this simulation
    /// created bodies for. See <see cref="Joints2D"/> for the joint types and their options.
    /// </summary>
    public SimulationJoints2D Joints { get; }

    /// <summary>
    /// A radial blast: every shape within <paramref name="radius"/> of <paramref name="position"/>
    /// receives an impulse away from it.
    /// </summary>
    /// <param name="position">World-space centre of the blast.</param>
    /// <param name="radius">Full-strength radius, in metres.</param>
    /// <param name="impulsePerLength">
    /// Impulse per metre of shape perimeter facing the blast, in newton-seconds per metre. A broad
    /// flat body therefore takes more than a small round one of the same mass, which is what makes
    /// the effect read as a pressure wave rather than a point kick.
    /// </param>
    /// <param name="falloff">Distance beyond the radius over which the impulse fades to nothing, in metres.</param>
    /// <param name="maskBits">Collision category mask of the shapes affected; everything by default.</param>
    public void Explode(Vector2 position, float radius, float impulsePerLength, float falloff = 0f, ulong maskBits = ulong.MaxValue)
    {
        var def = b2DefaultExplosionDef();

        def.position = new B2Vec2(position.X, position.Y);
        def.radius = radius;
        def.falloff = falloff;
        def.impulsePerLength = impulsePerLength;
        def.maskBits = maskBits;

        b2World_Explode(_world.WorldId, def);
    }

    /// <summary>Creates a dynamic body associated with the given entity at a world position.</summary>
    /// <param name="entity">The entity driven by the body.</param>
    /// <param name="position">Initial world position (Z is ignored).</param>
    /// <param name="rotation">Initial rotation in radians about the Z axis.</param>
    /// <returns>The id of the created body.</returns>
    public B2BodyId CreateDynamicBody(Entity entity, Vector3 position, float rotation = 0f)
        => _bridge.CreateBody(entity, position, B2BodyType.b2_dynamicBody, rotation);

    /// <summary>Creates a dynamic body without an associated entity at a world position.</summary>
    /// <param name="position">Initial world position (Z is ignored).</param>
    /// <param name="rotation">Initial rotation in radians about the Z axis.</param>
    /// <returns>The id of the created body.</returns>
    public B2BodyId CreateDynamicBody(Vector3 position, float rotation = 0f)
        => _bridge.CreateBody(position, B2BodyType.b2_dynamicBody, rotation);

    /// <summary>Creates a kinematic body associated with the given entity at a world position.</summary>
    /// <param name="entity">The entity driven by the body.</param>
    /// <param name="position">Initial world position (Z is ignored).</param>
    /// <param name="rotation">Initial rotation in radians about the Z axis.</param>
    /// <returns>The id of the created body.</returns>
    public B2BodyId CreateKinematicBody(Entity entity, Vector3 position, float rotation = 0f)
        => _bridge.CreateBody(entity, position, B2BodyType.b2_kinematicBody, rotation);

    /// <summary>Creates a static body associated with the given entity at a world position.</summary>
    /// <param name="entity">The entity driven by the body.</param>
    /// <param name="position">Initial world position (Z is ignored).</param>
    /// <param name="rotation">Initial rotation in radians about the Z axis.</param>
    /// <returns>The id of the created body.</returns>
    public B2BodyId CreateStaticBody(Entity entity, Vector3 position, float rotation = 0f)
        => _bridge.CreateBody(entity, position, B2BodyType.b2_staticBody, rotation);

    /// <summary>Creates a static body without an associated entity at a world position.</summary>
    /// <param name="position">Initial world position (Z is ignored).</param>
    /// <param name="rotation">Initial rotation in radians about the Z axis.</param>
    /// <returns>The id of the created body.</returns>
    public B2BodyId CreateStaticBody(Vector3 position, float rotation = 0f)
        => _bridge.CreateBody(position, B2BodyType.b2_staticBody, rotation);

    /// <summary>Creates a kinematic body without an associated entity at a world position.</summary>
    /// <param name="position">Initial world position (Z is ignored).</param>
    /// <param name="rotation">Initial rotation in radians about the Z axis.</param>
    /// <returns>The id of the created body.</returns>
    public B2BodyId CreateKinematicBody(Vector3 position, float rotation = 0f)
        => _bridge.CreateBody(position, B2BodyType.b2_kinematicBody, rotation);

    /// <summary>Advances the simulation by the elapsed real time, executing zero or more fixed steps.</summary>
    /// <param name="elapsed">Frame time delta.</param>
    public void Update(TimeSpan elapsed)
    {
        if (!Enabled) return;

        // Delegate fixed-step accumulation to PhysicsWorld2D; process events per fixed step
        _world.Step((float)elapsed.TotalSeconds,
            perFixedStep: dt =>
            {
                if (EnableContactEvents || EnableHitEvents)
                    ProcessContactEvents();
                if (EnableSensorEvents)
                    ProcessSensorEvents();
                _bridge.SyncTransformsFromPhysics();

                foreach (var handler in _simulationUpdates) handler.AfterSimulationUpdate(this, dt);
            },
            beforeFixedStep: dt =>
            {
                foreach (var handler in _simulationUpdates) handler.SimulationUpdate(this, dt);
            });
    }

    private void ProcessContactEvents() => _eventRouter.ProcessContacts(_world.WorldId, GetEntity, EnableContactEvents, EnableHitEvents);

    private void ProcessSensorEvents() => _eventRouter.ProcessSensors(_world.WorldId, GetEntity, EnableSensorEvents);

    /// <summary>Removes the body associated with <paramref name="entity"/> from the world if present.</summary>
    /// <param name="entity">The entity whose body should be removed.</param>
    public void RemoveBody(Entity entity)
    {
        _bridge.RemoveBody(entity);
    }

    /// <summary>Gets the underlying Box2D world id.</summary>
    /// <returns>The native world id.</returns>
    public B2WorldId GetWorldId() => _world.WorldId;

    /// <summary>Gets the entity previously registered for the given body id (or null).</summary>
    /// <param name="bodyId">The body to look up.</param>
    /// <returns>The associated entity, or null when the body has none.</returns>
    public Entity? GetEntity(B2BodyId bodyId)
    {
        return _bridge.GetEntity(bodyId);
    }

    /// <summary>Returns a snapshot list of all body ids tracked by the bridge.</summary>
    /// <returns>A new list with every tracked body id.</returns>
    public List<B2BodyId> GetAllBodyIds() => _bridge.Bodies.ToList();

    /// <summary>
    /// Tests if a point overlaps with any physics shape in the world.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <param name="querySize">Half-extent of the query box around the point.</param>
    /// <returns>The body id that was hit, or null if nothing was hit.</returns>
    public B2BodyId? OverlapPoint(Vector2 point, float querySize = 0.1f)
        => PhysicsQueries2D.OverlapPoint(_world.WorldId, point, querySize);

    /// <summary>
    /// Performs a raycast and returns the first hit.
    /// </summary>
    /// <param name="origin">Ray origin.</param>
    /// <param name="direction">Ray direction (normalized).</param>
    /// <param name="maxDistance">Maximum distance to cast.</param>
    /// <returns>First hit or null if no hit.</returns>
    public RaycastHit? Raycast(Vector2 origin, Vector2 direction, float maxDistance)
    {
        var hitResult = PhysicsQueries2D.RaycastClosest(_world.WorldId, origin, direction, maxDistance);

        if (!hitResult.hit) return null;

        return new RaycastHit(
            GetEntity(hitResult.bodyId),
            hitResult.bodyId,
            hitResult.shapeId,
            hitResult.point,
            hitResult.normal,
            maxDistance * hitResult.fraction,
            hitResult.fraction);
    }

    /// <summary>
    /// Performs a raycast and returns all hits along the ray.
    /// </summary>
    /// <param name="origin">Ray origin.</param>
    /// <param name="direction">Ray direction (normalized).</param>
    /// <param name="maxDistance">Maximum distance to cast.</param>
    /// <returns>List of all hits along the ray.</returns>
    public List<RaycastHit> RaycastAll(Vector2 origin, Vector2 direction, float maxDistance)
    {
        var rawHits = PhysicsQueries2D.RaycastAll(_world.WorldId, origin, direction, maxDistance);
        var converted = new List<RaycastHit>(rawHits.Count);

        foreach (var hit in rawHits)
        {
            converted.Add(new RaycastHit(
                GetEntity(hit.BodyId),
                hit.BodyId,
                hit.ShapeId,
                hit.Point,
                hit.Normal,
                maxDistance * hit.Fraction,
                hit.Fraction));
        }

        return converted;
    }

    /// <summary>
    /// Overlaps an AABB and returns all bodies within it.
    /// </summary>
    /// <param name="lowerBound">Lower corner of the AABB.</param>
    /// <param name="upperBound">Upper corner of the AABB.</param>
    /// <returns>List of body ids (no duplicates).</returns>
    public List<B2BodyId> OverlapAABB(Vector2 lowerBound, Vector2 upperBound)
        => PhysicsQueries2D.OverlapAABB(_world.WorldId, lowerBound, upperBound);

    /// <summary>
    /// Overlaps a circle and returns all bodies within it.
    /// </summary>
    /// <param name="center">World-space circle center.</param>
    /// <param name="radius">Circle radius.</param>
    /// <returns>List of body ids (no duplicates).</returns>
    public List<B2BodyId> OverlapCircle(Vector2 center, float radius)
        => PhysicsQueries2D.OverlapCircle(_world.WorldId, center, radius);

    /// <summary>
    /// Overlaps a circle and returns all bodies within it whose shapes pass <paramref name="filter"/>.
    /// </summary>
    /// <param name="center">World-space circle center.</param>
    /// <param name="radius">Circle radius.</param>
    /// <param name="filter">Category and mask bits the overlapped shapes must satisfy.</param>
    /// <returns>List of body ids (no duplicates).</returns>
    public List<B2BodyId> OverlapCircle(Vector2 center, float radius, B2QueryFilter filter)
        => PhysicsQueries2D.OverlapCircle(_world.WorldId, center, radius, filter);

    /// <summary>
    /// Sweeps a circle (or a point, when <paramref name="radius"/> is zero) along <paramref name="translation"/>
    /// and returns the closest thing it strikes.
    /// </summary>
    /// <param name="center">World-space centre of the circle at the start of the sweep.</param>
    /// <param name="radius">Circle radius; zero casts a point.</param>
    /// <param name="translation">The sweep, as a world-space displacement.</param>
    /// <param name="filter">Category and mask bits the struck shapes must satisfy.</param>
    /// <returns>The closest hit, or null when the sweep is clear.</returns>
    public ShapeCastHit? CastCircleClosest(Vector2 center, float radius, Vector2 translation, B2QueryFilter filter)
        => PhysicsQueries2D.CastCircleClosest(_world.WorldId, center, radius, translation, filter);

    /// <summary>
    /// Sweeps a line segment along <paramref name="translation"/> and returns the closest thing it strikes.
    /// </summary>
    /// <param name="a">One end of the segment at the start of the sweep, in world space.</param>
    /// <param name="b">The other end.</param>
    /// <param name="translation">The sweep, as a world-space displacement.</param>
    /// <param name="filter">Category and mask bits the struck shapes must satisfy.</param>
    /// <returns>The closest hit, or null when the sweep is clear.</returns>
    public ShapeCastHit? CastSegmentClosest(Vector2 a, Vector2 b, Vector2 translation, B2QueryFilter filter)
        => PhysicsQueries2D.CastSegmentClosest(_world.WorldId, a, b, translation, filter);

    /// <summary>Disposes underlying world resources.</summary>
    public void Dispose() => _world.Dispose();
}
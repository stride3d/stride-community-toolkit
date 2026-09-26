using Box2D.NET;
using Stride.Core.Mathematics;
using Stride.Engine;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Movers;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// A platformer character that is not a rigid body: a capsule the game moves itself, Quake style,
/// with Box2D asked only what it touches. Ported from the Box2D.NET samples' <c>Mover</c>
/// (MIT, (c) 2022 Erin Catto, (c) 2025 Choi Ikpil).
/// </summary>
/// <remarks>
/// <para>
/// Each step: ground friction and acceleration towards <see cref="Throttle"/> times
/// <see cref="MaxSpeed"/>, gravity, then a <em>pogo</em> - a shape cast straight down whose hit
/// distance drives a spring that floats the capsule at <see cref="PogoRestLength"/> above the
/// ground and tells it whether it is standing; then up to five rounds of collecting contact
/// planes (<c>b2World_CollideMover</c>), solving a translation that respects them
/// (<c>b2SolvePlanes</c>) and sweeping it (<c>b2World_CastMover</c>); finally the velocity is
/// clipped against those planes so it stops pushing into walls.
/// </para>
/// <para>
/// Register it with <see cref="Box2DSimulation.RegisterSimulationUpdate"/> and it steps itself
/// after every fixed physics step, so kinematic platforms and the mover agree on time. Set
/// <see cref="Throttle"/> and call <see cref="Jump"/> from your frame update; give it an
/// <see cref="Entity"/> and it writes the transform after each step.
/// </para>
/// <para>
/// The mover collides with shapes whose category bits pass <see cref="CollideFilter"/>. Shapes
/// carry the sample's categories by default: give the level <see cref="StaticCategory"/>, moving
/// platforms <see cref="DynamicCategory"/>, things the mover should kick but walk through
/// <see cref="DebrisCategory"/>. How hard a shape pushes back is per shape, through
/// <see cref="SetResponse"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var mover = new CharacterMover2D(new Vector2(2, 8)) { Entity = hero };
/// simulation.RegisterSimulationUpdate(mover);
///
/// // per frame
/// mover.Throttle = (input.IsKeyDown(Keys.D) ? 1 : 0) - (input.IsKeyDown(Keys.A) ? 1 : 0);
/// if (input.IsKeyPressed(Keys.Space)) mover.Jump();
/// </code>
/// </example>
public sealed class CharacterMover2D : IBox2DSimulationUpdate
{
    /// <summary>Category bit for the level: chains, floors, walls.</summary>
    public static readonly ulong StaticCategory = 0x0001;

    /// <summary>Category bit for movers; a mover overlaps others but does not sweep against them, which is what makes the push soft.</summary>
    public static readonly ulong MoverCategory = 0x0002;

    /// <summary>Category bit for moving obstacles the mover collides with and stands on: elevators, bridges.</summary>
    public static readonly ulong DynamicCategory = 0x0004;

    /// <summary>Category bit for loose things the mover passes through and can kick.</summary>
    public static readonly ulong DebrisCategory = 0x0008;

    private const int PlaneCapacity = 8;
    private const int MaxIterations = 5;
    private const float Tolerance = 0.01f;

    // One delegate for every mover, so collecting planes allocates nothing per step.
    private static readonly b2PlaneResultFcn PlaneCallback = CollectPlane;

    private readonly B2CollisionPlane[] _planes = new B2CollisionPlane[PlaneCapacity];
    private int _planeCount;
    private B2Vec2 _position;
    private B2Vec2 _velocity;
    private float _pogoVelocity;
    private bool _jumpRequested;

    /// <summary>
    /// Creates a mover standing at <paramref name="position"/>.
    /// </summary>
    /// <param name="position">World-space centre of the capsule.</param>
    /// <param name="halfHeight">Half the distance between the capsule's two centres.</param>
    /// <param name="radius">The capsule's radius.</param>
    public CharacterMover2D(Vector2 position, float halfHeight = 0.5f, float radius = 0.3f)
    {
        _position = new B2Vec2(position.X, position.Y);
        HalfHeight = halfHeight;
        Radius = radius;
        PogoRestLength = 3f * radius;
    }

    /// <summary>Half the distance between the capsule's two centres.</summary>
    public float HalfHeight { get; }

    /// <summary>The capsule's radius.</summary>
    public float Radius { get; }

    /// <summary>Walk input, -1 to 1: the fraction of <see cref="MaxSpeed"/> wanted, negative for left.</summary>
    public float Throttle { get; set; }

    /// <summary>World-space centre of the capsule.</summary>
    public Vector2 Position => new(_position.X, _position.Y);

    /// <summary>Current velocity, metres per second.</summary>
    public Vector2 Velocity => new(_velocity.X, _velocity.Y);

    /// <summary>Whether the pogo found ground under the feet in the last step.</summary>
    public bool IsOnGround { get; private set; }

    /// <summary>The contact planes found in the last step, for drawing.</summary>
    public ReadOnlySpan<B2CollisionPlane> Planes => _planes.AsSpan(0, _planeCount);

    /// <summary>How many plane-solver iterations the last step took in total.</summary>
    public int IterationsLastStep { get; private set; }

    /// <summary>Where the pogo cast started in the last step: the lower capsule centre.</summary>
    public Vector2 PogoOrigin { get; private set; }

    /// <summary>Where the pogo cast ended in the last step: the hit point's height, or the full reach when it missed.</summary>
    public Vector2 PogoEnd { get; private set; }

    /// <summary>Whether the pogo cast hit anything in the last step.</summary>
    public bool PogoHit { get; private set; }

    /// <summary>The entity whose transform follows the mover, if any.</summary>
    public Entity? Entity { get; set; }

    /// <summary>Top speed on the ground, metres per second.</summary>
    public float MaxSpeed { get; set; } = 6f;

    /// <summary>Below this speed the mover stops dead rather than creeping.</summary>
    public float MinSpeed { get; set; } = 0.1f;

    /// <summary>Below this speed ground friction removes a fixed amount per second instead of a fraction, so stopping is crisp.</summary>
    public float StopSpeed { get; set; } = 3f;

    /// <summary>Acceleration towards the wanted speed, as a multiple of <see cref="MaxSpeed"/> per second.</summary>
    public float Accelerate { get; set; } = 20f;

    /// <summary>Fraction of <see cref="Accelerate"/> available in the air.</summary>
    public float AirSteer { get; set; } = 0.2f;

    /// <summary>Ground friction, in units of 1 per second.</summary>
    public float Friction { get; set; } = 8f;

    /// <summary>Downward acceleration on the mover. Separate from the world's gravity, since the mover is not a body.</summary>
    public float Gravity { get; set; } = 30f;

    /// <summary>Upward speed given by <see cref="Jump"/>.</summary>
    public float JumpSpeed { get; set; } = 10f;

    /// <summary>Stiffness of the spring that floats the mover above the ground.</summary>
    public float PogoHertz { get; set; } = 5f;

    /// <summary>Damping ratio of that spring; under 1 gives a small bounce on landing.</summary>
    public float PogoDampingRatio { get; set; } = 0.8f;

    /// <summary>How far above the ground the lower capsule centre rests. Three radii by default.</summary>
    public float PogoRestLength { get; set; }

    /// <summary>Force pressed into whatever the mover stands on, newtons, so a bridge sags under it.</summary>
    public float GroundPushForce { get; set; } = 50f;

    /// <summary>The shape cast down to find the ground.</summary>
    public PogoShape PogoShape { get; set; } = PogoShape.Segment;

    /// <summary>What the mover collects contact planes from. Level, moving obstacles and other movers by default.</summary>
    public B2QueryFilter CollideFilter { get; set; } = new(MoverCategory, StaticCategory | DynamicCategory | MoverCategory);

    /// <summary>What the mover sweeps against. Not other movers, so two movers push each other softly rather than blocking.</summary>
    public B2QueryFilter CastFilter { get; set; } = new(MoverCategory, StaticCategory | DynamicCategory);

    /// <summary>What counts as ground for the pogo.</summary>
    public B2QueryFilter PogoFilter { get; set; } = new(MoverCategory, StaticCategory | DynamicCategory);

    /// <summary>World-space lower centre of the capsule.</summary>
    public Vector2 Bottom => new(_position.X, _position.Y - HalfHeight);

    /// <summary>World-space upper centre of the capsule.</summary>
    public Vector2 Top => new(_position.X, _position.Y + HalfHeight);

    /// <summary>
    /// Asks for a jump. Honoured at the next step if the mover is on the ground, then forgotten, so
    /// call it on the key press rather than while the key is held.
    /// </summary>
    public void Jump() => _jumpRequested = true;

    /// <summary>Moves the mover to <paramref name="position"/> and stops it.</summary>
    /// <param name="position">New world-space centre.</param>
    public void Teleport(Vector2 position)
    {
        _position = new B2Vec2(position.X, position.Y);
        _velocity = b2Vec2_zero;
        _pogoVelocity = 0f;
        IsOnGround = false;
        _planeCount = 0;
        SyncEntity();
    }

    /// <summary>
    /// Sets how <paramref name="shapeId"/> pushes a mover, in the shape's user data.
    /// </summary>
    /// <param name="shapeId">The shape.</param>
    /// <param name="maxPush">See <see cref="MoverShapeResponse.MaxPush"/>.</param>
    /// <param name="clipVelocity">See <see cref="MoverShapeResponse.ClipVelocity"/>.</param>
    public static void SetResponse(B2ShapeId shapeId, float maxPush, bool clipVelocity)
        => b2Shape_SetUserData(shapeId, B2UserData.Ref(new MoverShapeResponse(maxPush, clipVelocity)));

    /// <inheritdoc />
    public void SimulationUpdate(Box2DSimulation simulation, float deltaTime)
    {
        // Nothing before the step: the mover responds to where the bodies end up.
    }

    /// <inheritdoc />
    public void AfterSimulationUpdate(Box2DSimulation simulation, float deltaTime)
        => Step(simulation.GetWorldId(), deltaTime);

    /// <summary>
    /// Advances the mover by one step of <paramref name="deltaTime"/> seconds in
    /// <paramref name="worldId"/>. Called for you when registered with a simulation.
    /// </summary>
    /// <param name="worldId">The world to collide with.</param>
    /// <param name="deltaTime">Step length in seconds.</param>
    public void Step(B2WorldId worldId, float deltaTime)
    {
        if (deltaTime <= 0f) return;

        if (_jumpRequested)
        {
            if (IsOnGround)
            {
                _velocity.Y = JumpSpeed;
                IsOnGround = false;
            }

            _jumpRequested = false;
        }

        ApplyFriction(deltaTime);
        ApplyAcceleration(deltaTime);
        _velocity.Y -= Gravity * deltaTime;

        Pogo(worldId, deltaTime);

        var target = _position + deltaTime * _velocity + deltaTime * _pogoVelocity * new B2Vec2(0f, 1f);

        SolvePlanes(worldId, target);

        _velocity = b2ClipVector(_velocity, _planes, _planeCount);

        SyncEntity();
    }

    // Quake pmove friction: linear damping above StopSpeed, a fixed reduction below it, and a
    // dead stop below MinSpeed. Only on the ground; the air has no friction.
    private void ApplyFriction(float deltaTime)
    {
        var speed = b2Length(_velocity);

        if (speed < MinSpeed)
        {
            _velocity = b2Vec2_zero;
        }
        else if (IsOnGround)
        {
            var control = speed < StopSpeed ? StopSpeed : speed;
            var drop = control * Friction * deltaTime;
            var newSpeed = MathF.Max(0f, speed - drop);

            _velocity = (newSpeed / speed) * _velocity;
        }
    }

    private void ApplyAcceleration(float deltaTime)
    {
        var desiredSpeed = MathF.Min(MathF.Abs(Throttle), 1f) * MaxSpeed;
        var desiredDirection = new B2Vec2(MathF.Sign(Throttle), 0f);

        if (IsOnGround)
            _velocity.Y = 0f;

        var currentSpeed = b2Dot(_velocity, desiredDirection);
        var addSpeed = desiredSpeed - currentSpeed;

        if (addSpeed <= 0f) return;

        var steer = IsOnGround ? 1f : AirSteer;
        var accelSpeed = MathF.Min(steer * Accelerate * MaxSpeed * deltaTime, addSpeed);

        _velocity += accelSpeed * desiredDirection;
    }

    // The pogo: a cast down from the lower centre, a spring on how far the ground is from the rest
    // length, a push into whatever was hit. Also decides IsOnGround - but a mover still rising
    // does not snap to the ground it is leaving.
    private void Pogo(B2WorldId worldId, float deltaTime)
    {
        var rayLength = PogoRestLength + Radius;
        var origin = new Vector2(_position.X, _position.Y - HalfHeight);
        Vector2 translation;
        ShapeCastHit? hit;

        switch (PogoShape)
        {
            case PogoShape.Point:
                translation = new Vector2(0f, -rayLength);
                hit = PhysicsQueries2D.CastCircleClosest(worldId, origin, 0f, translation, PogoFilter);
                break;
            case PogoShape.Circle:
                var circleRadius = 0.5f * Radius;
                translation = new Vector2(0f, -rayLength + circleRadius);
                hit = PhysicsQueries2D.CastCircleClosest(worldId, origin, circleRadius, translation, PogoFilter);
                break;
            default:
                var half = new Vector2(0.75f * Radius, 0f);
                translation = new Vector2(0f, -rayLength);
                hit = PhysicsQueries2D.CastSegmentClosest(worldId, origin - half, origin + half, translation, PogoFilter);
                break;
        }

        PogoOrigin = origin;
        PogoHit = hit is not null;

        IsOnGround = IsOnGround ? hit is not null : hit is not null && _velocity.Y <= 0.01f;

        if (hit is not { } ground)
        {
            _pogoVelocity = 0f;
            PogoEnd = origin + translation;
            return;
        }

        var currentLength = ground.Fraction * rayLength;
        var offset = currentLength - PogoRestLength;

        _pogoVelocity = b2SpringDamper(PogoHertz, PogoDampingRatio, offset, _pogoVelocity, deltaTime);
        PogoEnd = origin + ground.Fraction * translation;

        b2Body_ApplyForce(ground.BodyId, new B2Vec2(0f, -GroundPushForce), new B2Vec2(ground.Point.X, ground.Point.Y), true);
    }

    // Collect planes, solve a translation that respects them, sweep it; repeat while the sweep
    // still moves us. Movers are never swept against, so they overlap and push each other softly.
    private void SolvePlanes(B2WorldId worldId, B2Vec2 target)
    {
        IterationsLastStep = 0;

        for (var iteration = 0; iteration < MaxIterations; iteration++)
        {
            _planeCount = 0;

            var capsule = new B2Capsule(
                new B2Vec2(_position.X, _position.Y - HalfHeight),
                new B2Vec2(_position.X, _position.Y + HalfHeight),
                Radius);

            b2World_CollideMover(worldId, capsule, CollideFilter, PlaneCallback, this);

            var result = b2SolvePlanes(target - _position, _planes, _planeCount);

            IterationsLastStep += result.iterationCount;

            var fraction = b2World_CastMover(worldId, capsule, result.translation, CastFilter);
            var delta = fraction * result.translation;

            _position += delta;

            if (b2LengthSquared(delta) < Tolerance * Tolerance)
                break;
        }
    }

    private static bool CollectPlane(B2ShapeId shapeId, ref B2PlaneResult planeResult, object context)
    {
        var self = (CharacterMover2D)context;
        var maxPush = float.MaxValue;
        var clipVelocity = true;

        if (b2Shape_GetUserData(shapeId).GetRef<MoverShapeResponse>() is { } response)
        {
            maxPush = response.MaxPush;
            clipVelocity = response.ClipVelocity;
        }

        if (self._planeCount < PlaneCapacity)
            self._planes[self._planeCount++] = new B2CollisionPlane(planeResult.plane, maxPush, 0f, clipVelocity);

        return true;
    }

    private void SyncEntity()
    {
        if (Entity is null) return;

        var z = Entity.Transform.Position.Z;

        Entity.Transform.Position = new Vector3(_position.X, _position.Y, z);
    }
}
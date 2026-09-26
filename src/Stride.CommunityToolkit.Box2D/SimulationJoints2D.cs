using Box2D.NET;
using Stride.Core.Mathematics;
using Stride.Engine;
using System.Diagnostics.CodeAnalysis;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// <see cref="Joints2D"/> for a <see cref="Box2DSimulation"/>: the same factories with the world
/// id filled in, and overloads that take the entities the simulation created bodies for.
/// Reached through <see cref="Box2DSimulation.Joints"/>.
/// </summary>
public sealed class SimulationJoints2D
{
    private readonly PhysicsWorld2D _world;
    private readonly Box2DStrideBridge _bridge;

    internal SimulationJoints2D(PhysicsWorld2D world, Box2DStrideBridge bridge)
    {
        _world = world;
        _bridge = bridge;
    }

    /// <inheritdoc cref="Joints2D.CreateRevolute"/>
    public B2JointId CreateRevolute(B2BodyId a, B2BodyId b, Vector2 worldPivot, RevoluteJointOptions? options = null)
        => Joints2D.CreateRevolute(_world.WorldId, a, b, worldPivot, options);

    /// <inheritdoc cref="Joints2D.CreateRevolute"/>
    public B2JointId CreateRevolute(Entity a, Entity b, Vector2 worldPivot, RevoluteJointOptions? options = null)
        => CreateRevolute(Body(a), Body(b), worldPivot, options);

    /// <inheritdoc cref="Joints2D.CreatePrismatic"/>
    public B2JointId CreatePrismatic(B2BodyId a, B2BodyId b, Vector2 worldPivot, Vector2 worldAxis, PrismaticJointOptions? options = null)
        => Joints2D.CreatePrismatic(_world.WorldId, a, b, worldPivot, worldAxis, options);

    /// <inheritdoc cref="Joints2D.CreatePrismatic"/>
    public B2JointId CreatePrismatic(Entity a, Entity b, Vector2 worldPivot, Vector2 worldAxis, PrismaticJointOptions? options = null)
        => CreatePrismatic(Body(a), Body(b), worldPivot, worldAxis, options);

    /// <inheritdoc cref="Joints2D.CreateWheel"/>
    public B2JointId CreateWheel(B2BodyId chassis, B2BodyId wheel, Vector2 worldPivot, Vector2 worldAxis, WheelJointOptions? options = null)
        => Joints2D.CreateWheel(_world.WorldId, chassis, wheel, worldPivot, worldAxis, options);

    /// <inheritdoc cref="Joints2D.CreateWheel"/>
    public B2JointId CreateWheel(Entity chassis, Entity wheel, Vector2 worldPivot, Vector2 worldAxis, WheelJointOptions? options = null)
        => CreateWheel(Body(chassis), Body(wheel), worldPivot, worldAxis, options);

    /// <inheritdoc cref="Joints2D.CreateDistance"/>
    public B2JointId CreateDistance(B2BodyId a, B2BodyId b, Vector2 worldAnchorA, Vector2 worldAnchorB, DistanceJointOptions? options = null)
        => Joints2D.CreateDistance(_world.WorldId, a, b, worldAnchorA, worldAnchorB, options);

    /// <inheritdoc cref="Joints2D.CreateDistance"/>
    public B2JointId CreateDistance(Entity a, Entity b, Vector2 worldAnchorA, Vector2 worldAnchorB, DistanceJointOptions? options = null)
        => CreateDistance(Body(a), Body(b), worldAnchorA, worldAnchorB, options);

    /// <inheritdoc cref="Joints2D.CreateWeld"/>
    public B2JointId CreateWeld(B2BodyId a, B2BodyId b, Vector2 worldPivot, WeldJointOptions? options = null)
        => Joints2D.CreateWeld(_world.WorldId, a, b, worldPivot, options);

    /// <inheritdoc cref="Joints2D.CreateWeld"/>
    public B2JointId CreateWeld(Entity a, Entity b, Vector2 worldPivot, WeldJointOptions? options = null)
        => CreateWeld(Body(a), Body(b), worldPivot, options);

    /// <inheritdoc cref="Joints2D.CreateMotor"/>
    public B2JointId CreateMotor(B2BodyId a, B2BodyId b, Vector2? worldPivot = null, MotorJointOptions? options = null)
        => Joints2D.CreateMotor(_world.WorldId, a, b, worldPivot, options);

    /// <inheritdoc cref="Joints2D.CreateMotor"/>
    public B2JointId CreateMotor(Entity a, Entity b, Vector2? worldPivot = null, MotorJointOptions? options = null)
        => CreateMotor(Body(a), Body(b), worldPivot, options);

    /// <inheritdoc cref="Joints2D.CreateFilter"/>
    public B2JointId CreateFilter(B2BodyId a, B2BodyId b) => Joints2D.CreateFilter(_world.WorldId, a, b);

    /// <inheritdoc cref="Joints2D.CreateFilter"/>
    public B2JointId CreateFilter(Entity a, Entity b) => CreateFilter(Body(a), Body(b));

    // Destroy, IsValid and GetAnchors need no world id, so they could be static; they stay instance
    // members so that simulation.Joints is the one place a caller reaches every joint operation.
    /// <inheritdoc cref="Joints2D.Destroy"/>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Part of the simulation.Joints facade; a static member would send the caller to Joints2D for the same call.")]
    public void Destroy(B2JointId joint, bool wakeBodies = true) => Joints2D.Destroy(joint, wakeBodies);

    /// <inheritdoc cref="Joints2D.IsValid"/>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Part of the simulation.Joints facade; a static member would send the caller to Joints2D for the same call.")]
    public bool IsValid(B2JointId joint) => Joints2D.IsValid(joint);

    /// <inheritdoc cref="Joints2D.GetAnchors"/>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Part of the simulation.Joints facade; a static member would send the caller to Joints2D for the same call.")]
    public (Vector2 A, Vector2 B) GetAnchors(B2JointId joint) => Joints2D.GetAnchors(joint);

    private B2BodyId Body(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return _bridge.GetBody(entity)
            ?? throw new InvalidOperationException($"'{entity.Name}' has no body in this simulation. Create one with CreateDynamicBody, CreateKinematicBody or CreateStaticBody first.");
    }
}
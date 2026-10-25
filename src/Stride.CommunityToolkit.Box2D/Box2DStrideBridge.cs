using Box2D.NET;
using Stride.Core.Mathematics;
using Stride.Engine;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Bridge responsible for mapping Box2D bodies to Stride entities and synchronizing transforms.
/// This isolates engine-specific concerns away from the engine-agnostic <see cref="PhysicsWorld2D"/>.
/// </summary>
/// <param name="world">The physics world bodies are created in.</param>
public sealed class Box2DStrideBridge(PhysicsWorld2D world)
{
    private readonly Dictionary<B2BodyId, Entity> _bodyToEntity = [];
    private readonly Dictionary<Entity, B2BodyId> _entityToBody = [];

    /// <summary>
    /// Creates a body of the given type at a world position, without an associated entity.
    /// </summary>
    /// <param name="position">Initial world position (Z is ignored).</param>
    /// <param name="type">The Box2D body type.</param>
    /// <param name="rotation">Initial rotation in radians about the Z axis.</param>
    /// <returns>The id of the created body.</returns>
    public B2BodyId CreateBody(Vector3 position, B2BodyType type, float rotation = 0f)
    {
        var bodyDef = b2DefaultBodyDef();

        bodyDef.type = type;
        bodyDef.position = new B2Vec2(position.X, position.Y);
        bodyDef.rotation = b2MakeRot(rotation);

        return b2CreateBody(world.WorldId, in bodyDef);
    }

    /// <summary>
    /// Creates a body of the given type at a world position and associates it with an entity,
    /// so <see cref="SyncTransformsFromPhysics"/> keeps the entity transform in step with the body.
    /// </summary>
    /// <param name="entity">The entity driven by the body.</param>
    /// <param name="position">Initial world position (Z is ignored).</param>
    /// <param name="type">The Box2D body type.</param>
    /// <param name="rotation">Initial rotation in radians about the Z axis.</param>
    /// <returns>The id of the created body.</returns>
    public B2BodyId CreateBody(Entity entity, Vector3 position, B2BodyType type, float rotation = 0f)
    {
        var bodyId = CreateBody(position, type, rotation);

        _bodyToEntity[bodyId] = entity;
        _entityToBody[entity] = bodyId;

        return bodyId;
    }

    /// <summary>
    /// Destroys the body associated with <paramref name="entity"/> and forgets the mapping, if present.
    /// </summary>
    /// <param name="entity">The entity whose body should be removed.</param>
    public void RemoveBody(Entity entity)
    {
        if (!_entityToBody.Remove(entity, out var bodyId)) return;

        b2DestroyBody(bodyId);
        _bodyToEntity.Remove(bodyId);
    }

    /// <summary>Gets the entity registered for the given body id, or null.</summary>
    /// <param name="bodyId">The body to look up.</param>
    public Entity? GetEntity(B2BodyId bodyId) => _bodyToEntity.TryGetValue(bodyId, out var entity) ? entity : null;

    /// <summary>Gets the body registered for the given entity, or null.</summary>
    /// <param name="entity">The entity to look up.</param>
    public B2BodyId? GetBody(Entity entity) => _entityToBody.TryGetValue(entity, out var bodyId) ? bodyId : null;

    /// <summary>All body ids currently mapped to an entity.</summary>
    public IEnumerable<B2BodyId> Bodies => _bodyToEntity.Keys;

    /// <summary>
    /// Synchronizes Stride entity transforms from physics body positions and rotations.
    /// Call after each fixed step.
    /// </summary>
    public void SyncTransformsFromPhysics()
    {
        // Body move events deliver exactly the bodies the simulation moved this step, transforms
        // included, as one contiguous array - the API Box2D provides precisely for engine sync.
        // The cost scales with movement, not population: a settled pile produces zero events,
        // where iterating every body and asking for its transform cost ~10x more while moving.
        var events = b2World_GetBodyEvents(world.WorldId);

        for (var i = 0; i < events.moveCount; i++)
        {
            ref var moveEvent = ref events.moveEvents[i];

            if (!_bodyToEntity.TryGetValue(moveEvent.bodyId, out var entity)) continue;

            var position = moveEvent.transform.p;

            entity.Transform.Position = new Vector3(position.X, position.Y, 0f);
            entity.Transform.Rotation = Quaternion.RotationZ(b2Rot_GetAngle(moveEvent.transform.q));
        }
    }
}
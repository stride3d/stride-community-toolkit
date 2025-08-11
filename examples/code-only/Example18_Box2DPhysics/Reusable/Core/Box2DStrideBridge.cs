using Box2D.NET;
using Stride.Core.Mathematics;
using Stride.Engine;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Types;

namespace Example18_Box2DPhysics.Reusable.Core;

/// <summary>
/// Bridge responsible for mapping Box2D bodies to Stride entities and synchronizing transforms.
/// This isolates engine-specific concerns away from the generic <see cref="PhysicsWorld2D"/>.
/// </summary>
public class Box2DStrideBridge
{
    private readonly PhysicsWorld2D _world;
    private readonly Dictionary<B2BodyId, Entity> _bodyToEntity = [];
    private readonly Dictionary<Entity, B2BodyId> _entityToBody = [];

    public Box2DStrideBridge(PhysicsWorld2D world)
    {
        _world = world;
    }

    public B2BodyId CreateBody(Entity entity, Vector3 position, B2BodyType type)
    {
        var bodyDef = b2DefaultBodyDef();
        bodyDef.type = type;
        bodyDef.position = new B2Vec2(position.X, position.Y);

        var bodyId = b2CreateBody(_world.WorldId, ref bodyDef);

        _bodyToEntity[bodyId] = entity;
        _entityToBody[entity] = bodyId;

        return bodyId;
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

    public Entity? GetEntity(B2BodyId bodyId) => _bodyToEntity.TryGetValue(bodyId, out var e) ? e : null;

    public B2BodyId? GetBody(Entity entity) => _entityToBody.TryGetValue(entity, out var id) ? id : null;

    public IEnumerable<B2BodyId> Bodies => _bodyToEntity.Keys;

    /// <summary>
    /// Synchronize Stride entity transforms from physics body positions and rotations.
    /// Call after each fixed step.
    /// </summary>
    public void SyncTransformsFromPhysics()
    {
        foreach (var item in _bodyToEntity)
        {
            var bodyId = item.Key;
            var entity = item.Value;
            var position = b2Body_GetPosition(bodyId);
            var rotation = b2Body_GetRotation(bodyId);

            entity.Transform.Position = new Vector3(position.X, position.Y, 0f);
            entity.Transform.Rotation = Quaternion.RotationZ(B2MathFunction.b2Rot_GetAngle(rotation));
        }
    }
}
using Box2D.NET;
using Stride.Core.Mathematics;
using Stride.Engine;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;
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

        for (int stepCount = 0;
             _remainingUpdateTime >= _fixedTimeStep && stepCount < MaxStepsPerFrame;
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

    public void Dispose()
    {
        if (_worldId.index1 != 0) // Check if world is valid
        {
            b2DestroyWorld(_worldId);
        }
    }
}

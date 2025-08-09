using Box2D.NET;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example18_Box2DPhysics.Reusable.Core;

/// <summary>
/// Component to manage Box2D body properties in Stride entities
/// </summary>
public class Box2DBodyComponent : EntityComponent
{
    public B2BodyId BodyId { get; set; }
    public B2BodyType BodyType { get; set; } = B2BodyType.b2_dynamicBody;
    public int CollisionGroup { get; set; } = 0;
    public bool IsSensor { get; set; } = false;

    // Physics properties
    public float Mass { get; set; } = 1.0f;
    public float Friction { get; set; } = 0.3f;
    public float Restitution { get; set; } = 0.0f;
    public float LinearDamping { get; set; } = 0.0f;
    public float AngularDamping { get; set; } = 0.0f;

    // Velocity (implementation would need proper Box2D.NET API integration)
    public Vector2 LinearVelocity { get; set; }
    public float AngularVelocity { get; set; }

    // Force and impulse application (implementation would need proper Box2D.NET API integration)
    public void ApplyForce(Vector2 force, Vector2? point = null)
    {
        // Implementation depends on actual Box2D.NET API
    }

    public void ApplyImpulse(Vector2 impulse, Vector2? point = null)
    {
        // Implementation depends on actual Box2D.NET API
    }

    public void ApplyTorque(float torque)
    {
        // Implementation depends on actual Box2D.NET API
    }
}
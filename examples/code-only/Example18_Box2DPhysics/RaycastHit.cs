using Box2D.NET;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example18_Box2DPhysics;

public struct RaycastHit
{
    public Entity? Entity;
    public B2BodyId BodyId;
    public B2ShapeId ShapeId;
    public Vector2 Point;
    public Vector2 Normal;
    public float Distance;
    public float Fraction;
}
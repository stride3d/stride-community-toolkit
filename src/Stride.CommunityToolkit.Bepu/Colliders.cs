using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// The Bepu collider that matches a primitive shape, for a caller who builds a body of their own, such as a
/// <see cref="CompoundCollider"/> of several pieces. <see cref="EntityExtensions.AddBepuPhysics(Stride.Engine.Entity, Primitive3D, BepuPhysicsOptions?)"/>
/// uses the same derivation for the body it attaches.
/// </summary>
public static class Colliders
{
    /// <summary>
    /// The Bepu collider that matches a flat <paramref name="shape"/>, extruded <paramref name="depth"/> along Z.
    /// </summary>
    /// <param name="shape">The 2D shape.</param>
    /// <param name="depth">The thickness of the collider along Z.</param>
    /// <returns>A collider the same size as the shape's model, to put into a <see cref="CompoundCollider"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="shape"/> is null.</exception>
    public static ColliderBase For(Primitive2D shape, float depth)
    {
        ArgumentNullException.ThrowIfNull(shape);

        return shape switch
        {
            Primitive2D.RectangleShape rectangle => new BoxCollider { Size = new Vector3(rectangle.Size.X, rectangle.Size.Y, depth) },
            // The RotationLocal needs to be initialized before the bounding shape is calculated.
            Primitive2D.CircleShape circle => new CylinderCollider
            {
                Radius = circle.Radius,
                Length = depth,
                RotationLocal = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90))
            },
            // A capsule's Length is the straight part between the two semicircles; the shape's Height is the whole thing. The
            // mesh decides the straight part, and its sliver for a height under two radii, so the two always agree
            Primitive2D.CapsuleShape capsule => new CapsuleCollider { Radius = capsule.Radius, Length = Capsule2DProceduralModel.StraightLength(capsule.Height, capsule.Radius) },
            Primitive2D.TriangleShape triangle => TriangularPrismCollider.Create(new Vector3(triangle.Size.X, triangle.Size.Y, depth)),
            Primitive2D.RegularPolygonShape polygon => PolygonCollider.Create(polygon, depth),
            Primitive2D.PolygonShape polygon => PolygonCollider.Create(polygon, depth),
            _ => throw new ArgumentException($"Unknown shape {shape.GetType().Name}.", nameof(shape))
        };
    }

    /// <summary>
    /// The Bepu collider that matches <paramref name="shape"/>.
    /// </summary>
    /// <param name="shape">The 3D shape.</param>
    /// <returns>A collider the same size as the shape's model, to put into a <see cref="CompoundCollider"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="shape"/> is null.</exception>
    public static ColliderBase For(Primitive3D shape)
    {
        ArgumentNullException.ThrowIfNull(shape);

        return shape switch
        {
            Primitive3D.BoxShape box => new BoxCollider { Size = box.Size },
            Primitive3D.SphereShape sphere => new SphereCollider { Radius = sphere.Radius },
            Primitive3D.CapsuleShape capsule => new CapsuleCollider { Radius = capsule.Radius, Length = capsule.Length },
            Primitive3D.CylinderShape cylinder => new CylinderCollider { Radius = cylinder.Radius, Length = cylinder.Height },
            Primitive3D.ConeShape cone => ConeCollider.Create(cone),
            Primitive3D.TorusShape torus => TorusCollider.Create(torus),
            // A plane has no thickness; a flat box the size of the plane is what a static ground needs
            Primitive3D.PlaneShape plane => new BoxCollider { Size = new Vector3(plane.Width, 0, plane.Length) },
            Primitive3D.TeapotShape teapot => TeapotCollider.Create(teapot),
            Primitive3D.TriangularPrismShape prism => TriangularPrismCollider.Create(prism),
            Primitive3D.TextShape text => TextCollider.Create(text),
            _ => throw new ArgumentException($"Unknown shape {shape.GetType().Name}.", nameof(shape))
        };
    }
}
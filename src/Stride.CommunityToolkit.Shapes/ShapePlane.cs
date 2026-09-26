using Stride.Core.Mathematics;
namespace Stride.CommunityToolkit.Shapes;

/// <summary>Where a shape's flat plane sits in the world.</summary>
internal readonly record struct ShapePlane(Vector3 Origin, Vector3 AxisX, Vector3 AxisY, PlaneMode Mode)
{
    /// <summary>The XY plane at a 2D position, the plane every 2D call draws in.</summary>
    internal static ShapePlane XY(Vector2 center)
        => new(new Vector3(center.X, center.Y, 0f), Vector3.UnitX, Vector3.UnitY, PlaneMode.Fixed);

    /// <summary>
    /// The plane a normal defines, with any two perpendicular unit axes spanning it. Which two
    /// only shows for shapes with an angular cut, so they are chosen so that angle 0 is world X
    /// both for a shape lying on the ground and for one standing in the XY plane, and the pair is
    /// right-handed about the normal so angles run counter-clockwise seen from its side.
    /// </summary>
    internal static ShapePlane FromNormal(Vector3 center, Vector3 normal)
    {
        var n = Vector3.Normalize(normal);

        // Cross with whichever world axis is least aligned, so the result is never degenerate
        var axisX = MathF.Abs(n.Y) < 0.9f
            ? Vector3.Normalize(Vector3.Cross(Vector3.UnitY, n))
            : Vector3.Normalize(Vector3.Cross(n, Vector3.UnitZ));

        var axisY = Vector3.Cross(n, axisX);

        return new ShapePlane(center, axisX, axisY, PlaneMode.Fixed);
    }
}
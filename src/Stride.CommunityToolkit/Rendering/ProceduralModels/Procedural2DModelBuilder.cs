using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

/// <summary>
/// Builds the procedural model of a <see cref="Primitive2D"/> shape: a flat mesh in the XY plane.
/// </summary>
public static class Procedural2DModelBuilder
{
    /// <summary>
    /// Builds the procedural model of <paramref name="shape"/>, with the shape's dimensions and tessellation.
    /// </summary>
    /// <param name="shape">The shape to build.</param>
    /// <returns>The procedural model; call <c>Generate</c> on it for the <see cref="Stride.Rendering.Model"/>.</returns>
    public static PrimitiveProceduralModelBase Build(Primitive2D shape)
    {
        ArgumentNullException.ThrowIfNull(shape);

        return shape switch
        {
            Primitive2D.RectangleShape rectangle => new RectangleProceduralModel { Size = rectangle.Size },
            Primitive2D.CircleShape circle => new CircleProceduralModel { Radius = circle.Radius, Tessellation = circle.Tessellation },
            Primitive2D.CapsuleShape capsule => new Capsule2DProceduralModel { Radius = capsule.Radius, TotalHeight = capsule.Height, Tessellation = capsule.Tessellation },
            Primitive2D.TriangleShape triangle => new TriangleProceduralModel { Size = triangle.Size },
            Primitive2D.RegularPolygonShape polygon => new PolygonProceduralModel { Radius = polygon.Radius, Sides = polygon.Sides },
            Primitive2D.PolygonShape polygon => new PolygonProceduralModel { Vertices = polygon.Vertices },
            _ => throw new ArgumentException($"Unknown shape {shape.GetType().Name}.", nameof(shape))
        };
    }
}
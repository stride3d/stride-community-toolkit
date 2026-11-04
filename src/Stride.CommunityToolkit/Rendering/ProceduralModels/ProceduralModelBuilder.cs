using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

/// <summary>
/// Builds the procedural model of a <see cref="Primitive3D"/> or <see cref="Primitive2D"/> shape.
/// </summary>
public static class ProceduralModelBuilder
{
    /// <summary>
    /// Builds the procedural model of <paramref name="shape"/>, with the shape's dimensions and tessellation.
    /// </summary>
    /// <param name="shape">The shape to build.</param>
    /// <returns>The procedural model; call <c>Generate</c> on it for the <see cref="Stride.Rendering.Model"/>.</returns>
    public static PrimitiveProceduralModelBase Build(Primitive3D shape)
    {
        ArgumentNullException.ThrowIfNull(shape);

        PrimitiveProceduralModelBase model = shape switch
        {
            Primitive3D.BoxShape box => new CubeProceduralModel { Size = box.Size },
            Primitive3D.SphereShape sphere => new SphereProceduralModel { Radius = sphere.Radius, Tessellation = sphere.Tessellation },
            Primitive3D.CapsuleShape capsule => new CapsuleProceduralModel { Radius = capsule.Radius, Length = capsule.Length, Tessellation = capsule.Tessellation },
            Primitive3D.CylinderShape cylinder => new CylinderProceduralModel { Radius = cylinder.Radius, Height = cylinder.Height, Tessellation = cylinder.Tessellation },
            Primitive3D.ConeShape cone => new ConeProceduralModel { Radius = cone.Radius, Height = cone.Height, Tessellation = cone.Tessellation },
            Primitive3D.TorusShape torus => new TorusProceduralModel { Radius = torus.Radius, Thickness = torus.Thickness, Tessellation = torus.Tessellation },
            Primitive3D.PlaneShape plane => new PlaneProceduralModel { Size = new Vector2(plane.Width, plane.Length), Tessellation = new Int2(plane.TessellationX, plane.TessellationY) },
            Primitive3D.TeapotShape teapot => new TeapotProceduralModel { Size = teapot.Size, Tessellation = teapot.Tessellation },
            Primitive3D.TriangularPrismShape prism => new TriangularPrismProceduralModel { Size = prism.Size },
            Primitive3D.TextShape text => new TextProceduralModel { Text = text.Content, Font = text.Font, Options = text.Options },
            _ => throw new ArgumentException($"Unknown shape {shape.GetType().Name}.", nameof(shape))
        };

        // One UV channel: the default of ten repeats the same coordinates ten times over and makes every vertex 84 bytes instead of 48
        model.NumberOfTextureCoordinates = 1;

        return model;
    }

    /// <summary>
    /// Builds the procedural model of a flat <paramref name="shape"/> in the XY plane, with the shape's dimensions and tessellation.
    /// </summary>
    /// <param name="shape">The shape to build.</param>
    /// <returns>The procedural model; call <c>Generate</c> on it for the <see cref="Stride.Rendering.Model"/>.</returns>
    public static PrimitiveProceduralModelBase Build(Primitive2D shape)
    {
        ArgumentNullException.ThrowIfNull(shape);

        PrimitiveProceduralModelBase model = shape switch
        {
            Primitive2D.RectangleShape rectangle => new RectangleProceduralModel { Size = rectangle.Size },
            Primitive2D.CircleShape circle => new CircleProceduralModel { Radius = circle.Radius, Tessellation = circle.Tessellation },
            Primitive2D.CapsuleShape capsule => new Capsule2DProceduralModel { Radius = capsule.Radius, Height = capsule.Height, Tessellation = capsule.Tessellation },
            Primitive2D.TriangleShape triangle => new TriangleProceduralModel { Size = triangle.Size },
            Primitive2D.RegularPolygonShape polygon => new PolygonProceduralModel { Radius = polygon.Radius, Sides = polygon.Sides },
            Primitive2D.PolygonShape polygon => new PolygonProceduralModel { Vertices = polygon.Vertices },
            _ => throw new ArgumentException($"Unknown shape {shape.GetType().Name}.", nameof(shape))
        };

        // One UV channel: the default of ten repeats the same coordinates ten times over and makes every vertex 84 bytes instead of 48
        model.NumberOfTextureCoordinates = 1;

        return model;
    }
}
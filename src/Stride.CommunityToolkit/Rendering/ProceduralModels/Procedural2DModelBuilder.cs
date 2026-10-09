using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

/// <summary>
/// A helper class for generating 2D procedural models based on a specified primitive model type and size.
/// </summary>
public static class Procedural2DModelBuilder
{
    /// <summary>
    /// Creates a flat 2D procedural model in the XY plane.
    /// </summary>
    /// <param name="type">The type of 2D primitive model to create (e.g., Circle, Square, Triangle).</param>
    /// <param name="size">
    /// Width and height; X is the radius for a circle, radius and total height for a capsule, radius and side count for a polygon. <see langword="null"/> uses the type's defaults.
    /// </param>
    /// <param name="depth">Not used.</param>
    /// <param name="vertices">Custom polygon vertices in the XY plane. Used only for <see cref="Primitive2DModelType.Polygon"/> and takes precedence over <paramref name="size"/>.</param>
    /// <returns>
    /// A <see cref="PrimitiveProceduralModelBase"/> object representing the generated 2D model.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when an unsupported <paramref name="type"/> is specified.</exception>
    public static PrimitiveProceduralModelBase Build(Primitive2DModelType type, Vector2? size = null, float depth = 0, Vector2[]? vertices = null)
        => type switch
        {
            Primitive2DModelType.Capsule => size is null ? new Capsule2DProceduralModel() : new() { Radius = size.Value.X, TotalHeight = size.Value.Y },
            Primitive2DModelType.Circle => new CircleProceduralModel() { Radius = size?.X ?? 0.5f },
            Primitive2DModelType.Polygon when vertices is { Length: > 0 } => new PolygonProceduralModel() { Vertices = vertices },
            Primitive2DModelType.Polygon => size is null ? new PolygonProceduralModel() : new() { Radius = size.Value.X, Sides = (int)size.Value.Y },
            Primitive2DModelType.Rectangle => new RectangleProceduralModel() { Size = size ?? new(0.5f, 1) },
            Primitive2DModelType.Square => new RectangleProceduralModel() { Size = size ?? new(1, 1) },
            Primitive2DModelType.Triangle => new TriangleProceduralModel() { Size = size ?? new(1, 1) },
            _ => throw new InvalidOperationException($"Unsupported Primitive2DModelType: {type}")
        };

}
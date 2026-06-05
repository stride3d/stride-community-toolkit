using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

/// <summary>
/// A helper class for generating 2D procedural models based on a specified primitive model type and size.
/// </summary>
public static class Procedural2DModelBuilder
{
    /// <summary>
    /// Generates a 2D procedural model based on the specified primitive model type, size, depth, and custom vertices.
    /// </summary>
    /// <param name="type">The type of 2D primitive model to create (e.g., Circle, Square, Triangle).</param>
    /// <param name="size">
    /// The size parameters for the model as a <see cref="Vector2"/>, where X and Y represent the dimensions. If null, default dimensions for the model type will be used.
    /// </param>
    /// <param name="depth">The depth of the 2D model, which affects its thickness in 3D space.</param>
    /// <param name="vertices">Custom polygon vertices in the XY plane. Used only for <see cref="Primitive2DModelType.Polygon"/> and takes precedence over <paramref name="size"/>.</param>
    /// <returns>
    /// A <see cref="PrimitiveProceduralModelBase"/> object representing the generated 2D model.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when an unsupported <paramref name="type"/> is specified.</exception>
    /// <remarks>
    /// This method creates different types of 2D procedural models (such as Rectangle, Circle, etc.) with the specified size and depth. The depth adds a third dimension to the 2D shape, turning it into a 3D object (e.g., a 2D rectangle becomes a 3D rectangular prism).
    /// </remarks>
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
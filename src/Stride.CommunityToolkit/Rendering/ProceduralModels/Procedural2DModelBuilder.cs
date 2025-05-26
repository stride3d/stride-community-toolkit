using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

/// <summary>
/// A helper class for generating 2D procedural models based on a specified primitive model type and size.
/// </summary>
public static class Procedural2DModelBuilder
{
    /// <summary>
    /// Generates a 2D procedural model based on the specified primitive model type, size, and depth.
    /// </summary>
    /// <param name="type">The type of 2D primitive model to create (e.g., Circle, Square, Triangle).</param>
    /// <param name="size">
    /// The size parameters for the model as a <see cref="Vector2"/>, where X and Y represent the dimensions.
    /// If null, default dimensions for the model type will be used.
    /// </param>
    /// <param name="depth">The depth of the 2D model, which affects its thickness in 3D space.</param>
    /// <returns>
    /// A <see cref="PrimitiveProceduralModelBase"/> object representing the generated 2D model.
    /// The dimensions of the model will be determined by the provided <paramref name="size"/> and <paramref name="depth"/>, or default dimensions if <paramref name="size"/> is null.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when an unsupported <paramref name="type"/> is specified.</exception>
    /// <remarks>
    /// This method creates different types of 2D procedural models (such as Rectangle, Circle, etc.) with the specified size and depth.
    /// The depth adds a third dimension to the 2D shape, turning it into a 3D object (e.g., a 2D rectangle becomes a 3D rectangular prism).
    /// </remarks>
    public static PrimitiveProceduralModelBase Build(Primitive2DModelType type, Vector2? size = null, float depth = 0)
        => type switch
        {
            Primitive2DModelType.Capsule => size is null ? new CapsuleProceduralModel() : new() { Radius = size.Value.X, Height = size.Value.Y },
            Primitive2DModelType.Circle => new CylinderProceduralModel() { Radius = size is null ? 0.5f : size.Value.X, Height = depth },
            Primitive2DModelType.Circle2D => new CircleProceduralModel() { Radius = size is null ? 0.5f : size.Value.X },
            //Primitive2DModelType.Polygon => size is null ? new PolygonProceduralModel() : new() { Radius = size.Value.X, Sides = (int)size.Value.Y },
            //Primitive2DModelType.Quad => size is null ? new QuadProceduralModel() : new() { Size = size.Value.XY() },
            Primitive2DModelType.Rectangle => new CubeProceduralModel() { Size = size is null ? new(2, 1, depth) : new(size.Value.X, size.Value.Y, depth) },
            Primitive2DModelType.Rectangle2D => new RectangleProceduralModel() { Size = size is null ? new(2, 1) : size.Value },
            Primitive2DModelType.Square => new CubeProceduralModel() { Size = size is null ? new(1, 1, depth) : new(size.Value.X, size.Value.Y, depth) },
            Primitive2DModelType.Square2D => new RectangleProceduralModel() { Size = size is null ? new(1, 1) : new(size.Value.X, size.Value.X) },
            Primitive2DModelType.Triangle => new TriangularPrismProceduralModel() { Size = size is null ? new(1, 1, depth) : new(size.Value.X, size.Value.Y, depth) },
            Primitive2DModelType.Triangle2D => new TriangleProceduralModel() { Size = size is null ? new(1, 1) : size.Value },
            _ => throw new InvalidOperationException($"Unsupported Primitive2DModelType: {type}")
        };
}
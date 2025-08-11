using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

/// <summary>
/// A helper class for generating 3D procedural models based on a specified primitive model type and size.
/// </summary>
public static class Procedural3DModelBuilder
{
    /// <summary>
    /// Generates a 3D procedural model based on the specified primitive model type and size.
    /// </summary>
    /// <param name="type">The type of 3D primitive model to create (e.g., Cube, Sphere, Capsule).</param>
    /// <param name="size">
    /// The size parameters for the model as a <see cref="Vector3"/>, where X, Y, and Z represent the dimensions.
    /// If null, default dimensions for the model type will be used.
    /// </param>
    /// <returns>
    /// A <see cref="PrimitiveProceduralModelBase"/> object representing the generated 3D model.
    /// The dimensions of the model will be determined by the provided <paramref name="size"/> or default dimensions if <paramref name="size"/> is null.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when an unsupported <paramref name="type"/> is specified.</exception>
    /// <remarks>
    /// This method allows for the creation of different types of primitive 3D models (such as Cube, Sphere, etc.) with the specified size.
    /// If no size is provided, default dimensions for each model type will be used.
    /// </remarks>
    public static PrimitiveProceduralModelBase Build(PrimitiveModelType type, Vector3? size = null)
        => type switch
        {
            PrimitiveModelType.Capsule => size is null ? new Stride.Rendering.ProceduralModels.CapsuleProceduralModel() : new() { Radius = size.Value.X, Length = size.Value.Y },
            PrimitiveModelType.Cone => size is null ? new ConeProceduralModel() : new() { Radius = size.Value.X, Height = size.Value.Y },
            PrimitiveModelType.Cube => size is null ? new CubeProceduralModel() : new() { Size = size.Value },
            PrimitiveModelType.Cylinder => size is null ? new CylinderProceduralModel() : new() { Radius = size.Value.X, Height = size.Value.Z },
            PrimitiveModelType.InfinitePlane => size is null ? new PlaneProceduralModel() : new() { Size = size.Value.XY() },
            PrimitiveModelType.Plane => size is null ? new PlaneProceduralModel() : new() { Size = size.Value.XY() },
            PrimitiveModelType.RectangularPrism => size is null ? new CubeProceduralModel() : new() { Size = size.Value },
            PrimitiveModelType.Sphere => size is null ? new SphereProceduralModel() : new() { Radius = size.Value.X },
            PrimitiveModelType.Teapot => size is null ? new TeapotProceduralModel() : new() { Size = size.Value.X },
            PrimitiveModelType.Torus => size is null ? new TorusProceduralModel() : new() { Radius = size.Value.X, Thickness = size.Value.Y },
            PrimitiveModelType.TriangularPrism => size is null ? new TriangularPrismProceduralModel() : new TriangularPrismProceduralModel() { Size = size is null ? Vector3.One : new(size.Value.X, size.Value.Y, size.Value.Z) },
            _ => throw new InvalidOperationException($"Unsupported PrimitiveModelType: {type}")
        };
}
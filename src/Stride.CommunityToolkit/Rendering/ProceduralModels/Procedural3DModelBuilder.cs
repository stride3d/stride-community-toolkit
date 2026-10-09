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
    /// The full size of a cube or prism, X and Z of a plane; otherwise X is the radius (the size for a teapot) and Y the length, height or thickness, except a cylinder, whose height is Z. <see langword="null"/> uses the type's defaults.
    /// </param>
    /// <returns>The procedural model.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an unsupported <paramref name="type"/> is specified.</exception>
    public static PrimitiveProceduralModelBase Build(PrimitiveModelType type, Vector3? size = null)
        => type switch
        {
            PrimitiveModelType.Capsule => size is null ? new CapsuleProceduralModel() : new() { Radius = size.Value.X, Length = size.Value.Y },
            PrimitiveModelType.Cone => size is null ? new ConeProceduralModel() : new() { Radius = size.Value.X, Height = size.Value.Y },
            PrimitiveModelType.Cube => size is null ? new CubeProceduralModel() : new() { Size = size.Value },
            PrimitiveModelType.Cylinder => size is null ? new CylinderProceduralModel() : new() { Radius = size.Value.X, Height = size.Value.Z },

            PrimitiveModelType.InfinitePlane => size is null ? new PlaneProceduralModel() : new() { Size = size.Value.XZ() },
            PrimitiveModelType.Plane => size is null ? new PlaneProceduralModel() : new() { Size = size.Value.XZ() },
            PrimitiveModelType.RectangularPrism => size is null ? new CubeProceduralModel() : new() { Size = size.Value },
            PrimitiveModelType.Sphere => size is null ? new SphereProceduralModel() : new() { Radius = size.Value.X },
            PrimitiveModelType.Teapot => size is null ? new TeapotProceduralModel() : new() { Size = size.Value.X },
            PrimitiveModelType.Torus => size is null ? new TorusProceduralModel() : new() { Radius = size.Value.X, Thickness = size.Value.Y },
            PrimitiveModelType.TriangularPrism => size is null ? new TriangularPrismProceduralModel() : new() { Size = size.Value },
            _ => throw new InvalidOperationException($"Unsupported PrimitiveModelType: {type}")
        };
}
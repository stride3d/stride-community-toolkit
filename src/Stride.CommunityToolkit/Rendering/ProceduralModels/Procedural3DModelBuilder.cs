using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

public static class Procedural3DModelBuilder
{
    /// <summary>
    /// Generates a procedural model based on the specified type and size.
    /// </summary>
    /// <param name="type">The type of primitive model to create.</param>
    /// <param name="size">The size parameters for the model, or null to use default size values. The dimensions in the Vector3 are used in the order X, Y, Z.</param>
    /// <returns>A primitive procedural model of the specified type, with dimensions specified by <paramref name="size"/> or default dimensions if <paramref name="size"/> is null.</returns>
    /// <remarks>
    /// If <paramref name="size"/> is null, default dimensions are used for the model.
    /// </remarks>
    public static PrimitiveProceduralModelBase Build(PrimitiveModelType type, Vector3? size = null)
        => type switch
        {
            PrimitiveModelType.Plane => size is null ? new PlaneProceduralModel() : new() { Size = size.Value.XY() },
            PrimitiveModelType.InfinitePlane => size is null ? new PlaneProceduralModel() : new() { Size = size.Value.XY() },
            PrimitiveModelType.Sphere => size is null ? new SphereProceduralModel() : new() { Radius = size.Value.X },
            PrimitiveModelType.Cube => size is null ? new CubeProceduralModel() : new() { Size = size.Value },
            PrimitiveModelType.Cylinder => size is null ? new CylinderProceduralModel() : new() { Radius = size.Value.X, Height = size.Value.Y },
            PrimitiveModelType.Torus => size is null ? new TorusProceduralModel() : new() { Radius = size.Value.X, Thickness = size.Value.Y },
            PrimitiveModelType.Teapot => size is null ? new TeapotProceduralModel() : new() { Size = size.Value.X },
            PrimitiveModelType.Cone => size is null ? new ConeProceduralModel() : new() { Radius = size.Value.X, Height = size.Value.Y },
            PrimitiveModelType.Capsule => size is null ? new CapsuleProceduralModel() : new() { Radius = size.Value.X, Length = size.Value.Y },
            _ => throw new InvalidOperationException($"Unsupported PrimitiveModelType: {type}")
        };

}

public static class Procedural2DModelBuilder
{
    /// <summary>
    /// Generates a procedural model based on the specified type and size.
    /// </summary>
    /// <param name="type">The type of primitive model to create.</param>
    /// <param name="size">The size parameters for the model, or null to use default size values. The dimensions in the Vector3 are used in the order X, Y, Z.</param>
    /// <returns>A primitive procedural model of the specified type, with dimensions specified by <paramref name="size"/> or default dimensions if <paramref name="size"/> is null.</returns>
    /// <remarks>
    /// If <paramref name="size"/> is null, default dimensions are used for the model.
    /// </remarks>
    public static PrimitiveProceduralModelBase Build(Primitive2DModelType type, Vector2? size = null, float depth = 0)
        => type switch
        {
            //Primitive2DModelType.Capsule => size is null ? new CapsuleProcedural2DModel() : new() { Radius = size.Value.X },
            Primitive2DModelType.Circle => new CylinderProceduralModel() { Radius = size is null ? 0.5f : size.Value.X, Height = depth },
            //Primitive2DModelType.Polygon => size is null ? new PolygonProceduralModel() : new() { Radius = size.Value.X, Sides = (int)size.Value.Y },
            //Primitive2DModelType.Quad => size is null ? new QuadProceduralModel() : new() { Size = size.Value.XY() },
            Primitive2DModelType.Rectangle => new CubeProceduralModel() { Size = size is null ? new(2, 1, depth) : new(size.Value.X, size.Value.Y, depth) },
            Primitive2DModelType.Square => new CubeProceduralModel() { Size = size is null ? new(1, 1, depth) : new(size.Value.X, size.Value.Y, depth) },
            //Primitive2DModelType.Square => size is null ? new SquareProceduralModel() : new() { Size = size.Value },
            Primitive2DModelType.Triangle => size is null ? new TriangularPrismProceduralModel() : new(),
            _ => throw new InvalidOperationException($"Unsupported Primitive2DModelType: {type}")
        };
}
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Builds the convex hull collider of a triangular prism, decomposed from the same mesh the model is built from,
/// and shares it between every prism of the same size.
/// </summary>
public static class TriangularPrismCollider
{
    /// <summary>
    /// Creates the collider of a prism whose triangle is <c>X</c> wide and <c>Y</c> tall, extruded <c>Z</c>.
    /// </summary>
    public static ConvexHullCollider Create(Vector3 size)
        => SharedHullCache.CreateCollider(
            nameof(TriangularPrismCollider), size.X, size.Y, size.Z,
            () => TriangularPrismProceduralModel.New(size).ToDecomposedHulls());

    /// <summary>
    /// Creates the collider of <paramref name="prism"/>.
    /// </summary>
    public static ConvexHullCollider Create(Primitive3D.TriangularPrismShape prism)
    {
        ArgumentNullException.ThrowIfNull(prism);

        return Create(prism.Size);
    }
}
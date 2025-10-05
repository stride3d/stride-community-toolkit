using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu.Extensions;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Bepu.Colliders;

/// <summary>
/// Provides helpers to construct Bepu <see cref="ConvexHullCollider"/> instances from a triangular prism primitive mesh.
/// </summary>
/// <remarks>
/// Builds a convex hull from a generated triangular prism mesh. If no explicit size is provided,
/// default values are taken from <see cref="TriangularPrismProceduralModel"/>.
/// The triangular cross-section lies in the X/Y plane and the prism extends along the Z axis (depth).
/// X specifies the base width of the triangle, Y specifies the triangle height, and Z specifies the prism depth.
/// </remarks>
/// <example>
/// <code>
/// // Create a collider using defaults from TriangularPrismProceduralModel
/// var collider = TriangularPrismCollider.Create(null);
///
/// // Or create a collider with explicit base width (X), height (Y), and depth (Z)
/// var collider2 = TriangularPrismCollider.Create(new Vector3(1.0f, 1.0f, 2.0f));
/// </code>
/// </example>
/// <seealso cref="ConvexHullCollider"/>
public static class TriangularPrismCollider
{
    /// <summary>
    /// Creates a Bepu <see cref="ConvexHullCollider"/> from a triangular prism primitive mesh.
    /// </summary>
    /// <param name="size">
    /// Optional size in world units: X = triangle base width, Y = triangle height, Z = prism depth.
    /// When <c>null</c>, defaults from <see cref="TriangularPrismProceduralModel"/> are used.
    /// </param>
    /// <returns>
    /// A <see cref="ConvexHullCollider"/> whose hull is computed from the generated triangular prism mesh.
    /// </returns>
    public static ConvexHullCollider Create(Vector3? size)
    {
        Vector3 validatedSize;

        if (size is null)
        {
            var prismModel = new TriangularPrismProceduralModel();

            validatedSize = prismModel.Size;
        }
        else
        {
            validatedSize = size.Value;
        }

        var meshData = TriangularPrismProceduralModel.New(validatedSize);

        return meshData.ToConvexHullColliderWithWelding();
    }
}
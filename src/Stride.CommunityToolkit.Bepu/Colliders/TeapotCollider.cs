using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu.Extensions;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Bepu.Colliders;

/// <summary>
/// Provides helpers to construct Bepu <see cref="ConvexHullCollider"/> instances from a teapot primitive mesh.
/// </summary>
/// <remarks>
/// Builds a convex hull from a generated teapot mesh. If no explicit size is provided,
/// default values are taken from <see cref="TeapotProceduralModel"/>.
/// The teapot mesh is generated with 16 radial segments via <see cref="GeometricPrimitive.Teapot"/>.
/// </remarks>
/// <example>
/// <code>
/// // Create a collider using defaults from TeapotProceduralModel
/// var collider = TeapotCollider.Create(null);
///
/// // Or create a collider with an explicit size (scale)
/// var collider2 = TeapotCollider.Create(0.75f);
/// </code>
/// </example>
/// <seealso cref="ConvexHullCollider"/>
/// <seealso cref="GeometricPrimitive.Teapot"/>
public static class TeapotCollider
{
    /// <summary>
    /// Creates a Bepu <see cref="ConvexHullCollider"/> from a teapot primitive mesh.
    /// </summary>
    /// <param name="size">
    /// Optional size (uniform scale) in world units. When <c>null</c>, defaults from <see cref="TeapotProceduralModel"/> are used.
    /// </param>
    /// <returns>
    /// A <see cref="ConvexHullCollider"/> whose hull is computed from the generated teapot mesh.
    /// </returns>
    public static ConvexHullCollider Create(float? size)
    {
        float validatedSize;

        if (size is null)
        {
            var teapotModel = new TeapotProceduralModel();

            validatedSize = teapotModel.Size;
        }
        else
        {
            validatedSize = size.Value;
        }

        var meshData = GeometricPrimitive.Teapot.New(size: validatedSize, 16);

        return meshData.ToConvexHullCollider();
    }
}
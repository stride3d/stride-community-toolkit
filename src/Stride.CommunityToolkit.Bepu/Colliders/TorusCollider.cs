using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu.Extensions;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Bepu.Colliders;

/// <summary>
/// Provides helpers to construct Bepu <see cref="ConvexHullCollider"/> instances from a torus primitive mesh.
/// </summary>
/// <remarks>
/// Builds a convex hull from a generated torus mesh. If no explicit radii are provided,
/// default values are taken from <see cref="TorusProceduralModel"/>.
/// The torus mesh is generated via <see cref="GeometricPrimitive.Torus"/>.
/// </remarks>
/// <example>
/// <code>
/// // Create a collider using defaults from TorusProceduralModel
/// var collider = TorusCollider.Create(null, null);
///
/// // Or create a collider with explicit major and minor radii
/// var collider2 = TorusCollider.Create(majorRadius: 1.25f, minorRadius: 0.35f);
/// </code>
/// </example>
/// <seealso cref="ConvexHullCollider"/>
/// <seealso cref="GeometricPrimitive.Torus"/>
public static class TorusCollider
{
    /// <summary>
    /// Creates a Bepu <see cref="ConvexHullCollider"/> from a torus primitive mesh.
    /// </summary>
    /// <param name="majorRadius">Optional major radius of the torus. When <c>null</c>, defaults from <see cref="TorusProceduralModel"/> are used.</param>
    /// <param name="minorRadius">Optional minor radius (thickness) of the torus. When <c>null</c>, defaults from <see cref="TorusProceduralModel"/> are used.</param>
    /// <returns>
    /// A <see cref="ConvexHullCollider"/> whose hull is computed from the generated torus mesh.
    /// </returns>
    public static ConvexHullCollider Create(float? majorRadius, float? minorRadius)
    {
        var validatedMajorRadius = 1f;
        var validatedMinorRadius = 0.5f;

        if (majorRadius is null)
        {
            var torusModel = new TorusProceduralModel();

            validatedMajorRadius = torusModel.Radius;
        }
        else
        {
            validatedMajorRadius = majorRadius.Value;
        }

        if (minorRadius is null)
        {
            var torusModel = new TorusProceduralModel();

            validatedMinorRadius = torusModel.Thickness;
        }
        else
        {
            validatedMinorRadius = minorRadius.Value;
        }

        var meshData = GeometricPrimitive.Torus.New(majorRadius: validatedMajorRadius, minorRadius: validatedMinorRadius);

        return meshData.ToConvexHullCollider();
    }
}
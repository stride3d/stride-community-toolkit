using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu.Extensions;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering.ProceduralModels;
using System.Numerics;

namespace Stride.CommunityToolkit.Bepu.Colliders;

/// <summary>
/// Provides helpers to construct Bepu <see cref="ConvexHullCollider"/> instances from a cone primitive mesh.
/// </summary>
/// <remarks>
/// Builds a convex hull from a generated cone mesh. If no explicit size is provided,
/// default values are taken from <see cref="ConeProceduralModel"/>.
/// The cone mesh is generated with 16 radial segments via <see cref="GeometricPrimitive.Cone"/>.
/// </remarks>
/// <example>
/// <code>
/// // Create a collider using defaults from ConeProceduralModel
/// var collider = ConeCollider.Create(null);
///
/// // Or create a collider with explicit radius (X) and height (Y); Z is unused
/// var collider2 = ConeCollider.Create(new Vector3(0.5f, 2.0f, 0f));
/// </code>
/// </example>
/// <seealso cref="ConvexHullCollider"/>
/// <seealso cref="GeometricPrimitive.Cone"/>
public static class ConeCollider
{
    /// <summary>
    /// Creates a Bepu <see cref="ConvexHullCollider"/> from a cone primitive mesh.
    /// </summary>
    /// <param name="size">
    /// Optional size in world units: X = radius, Y = height, Z is unused and ignored.
    /// When <c>null</c>, defaults from <see cref="ConeProceduralModel"/> are used.
    /// </param>
    /// <returns>
    /// A <see cref="ConvexHullCollider"/> whose hull is computed from the generated cone mesh.
    /// </returns>
    public static ConvexHullCollider Create(Vector3? size)
    {
        Vector3 validatedSize;

        if (size is null)
        {
            var coneModel = new ConeProceduralModel();

            validatedSize = new(coneModel.Radius, coneModel.Height, 1);
        }
        else
        {
            validatedSize = size.Value;
        }

        var meshData = GeometricPrimitive.Cone.New(radius: validatedSize.X, height: validatedSize.Y, 16);

        return meshData.ToConvexHullCollider();
    }
}
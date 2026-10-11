using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu.Extensions;
using Stride.Core.Mathematics;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Graphics.GeometricPrimitives;

namespace Stride.CommunityToolkit.Bepu.Colliders;

/// <summary>
/// Builds the convex hull collider of a cone, decomposed from the same mesh the model is built from, and shares
/// it between every cone of the same dimensions.
/// </summary>
public static class ConeCollider
{
    /// <summary>
    /// Creates the collider of a cone with the given base radius and height.
    /// </summary>
    public static ConvexHullCollider Create(float radius, float height)
        => SharedHullCache.CreateCollider(
            nameof(ConeCollider), radius, height, 0,
            () => GeometricPrimitive.Cone.New(radius: radius, height: height).ToDecomposedHulls());

    /// <summary>
    /// Creates the collider of <paramref name="cone"/>.
    /// </summary>
    public static ConvexHullCollider Create(Primitive3D.ConeShape cone)
    {
        ArgumentNullException.ThrowIfNull(cone);

        return Create(cone.Radius, cone.Height);
    }
}
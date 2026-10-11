using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu.Extensions;
using Stride.Core.Mathematics;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Graphics.GeometricPrimitives;

namespace Stride.CommunityToolkit.Bepu.Colliders;

/// <summary>
/// Builds the convex hull collider of a torus, decomposed from the same mesh the model is built from, and shares
/// it between every torus of the same dimensions.
/// </summary>
public static class TorusCollider
{
    /// <summary>
    /// Creates the collider of a torus: <paramref name="radius"/> from the centre to the middle of the tube, a tube
    /// <paramref name="thickness"/> across.
    /// </summary>
    public static ConvexHullCollider Create(float radius, float thickness)
        => SharedHullCache.CreateCollider(
            nameof(TorusCollider), radius, thickness, 0,
            () => GeometricPrimitive.Torus.New(majorRadius: radius, minorRadius: thickness).ToDecomposedHulls());

    /// <summary>
    /// Creates the collider of <paramref name="torus"/>.
    /// </summary>
    public static ConvexHullCollider Create(Primitive3D.TorusShape torus)
    {
        ArgumentNullException.ThrowIfNull(torus);

        return Create(torus.Radius, torus.Thickness);
    }
}
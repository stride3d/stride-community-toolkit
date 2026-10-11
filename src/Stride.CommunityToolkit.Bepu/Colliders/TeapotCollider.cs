using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu.Extensions;
using Stride.Core.Mathematics;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Graphics.GeometricPrimitives;

namespace Stride.CommunityToolkit.Bepu.Colliders;

/// <summary>
/// Builds the convex hull collider of the Utah teapot, decomposed from a coarse version of the model's mesh, and
/// shares it between every teapot of the same size.
/// </summary>
public static class TeapotCollider
{
    /// <summary>
    /// Creates the collider of a teapot scaled uniformly by <paramref name="size"/>.
    /// </summary>
    public static ConvexHullCollider Create(float size)
        => SharedHullCache.CreateCollider(
            nameof(TeapotCollider), size, 0, 0,
            () => GeometricPrimitive.Teapot.New(size: size, 16).ToDecomposedHulls());

    /// <summary>
    /// Creates the collider of <paramref name="teapot"/>.
    /// </summary>
    public static ConvexHullCollider Create(Primitive3D.TeapotShape teapot)
    {
        ArgumentNullException.ThrowIfNull(teapot);

        return Create(teapot.Size);
    }
}
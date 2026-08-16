using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.Core.Mathematics;
using Stride.Graphics;
using static Stride.BepuPhysics.Definitions.DecomposedHulls;

namespace Stride.CommunityToolkit.Bepu.Extensions;

/// <summary>
/// Helpers to build Bepu <see cref="ConvexHullCollider"/> instances from Stride procedural meshes.
/// </summary>
public static class ConvexHullColliderExtensions
{
    /// <summary>
    /// Builds a <see cref="ConvexHullCollider"/> from the raw mesh data.
    /// </summary>
    /// <param name="meshData">The mesh whose vertices define the hull.</param>
    /// <returns>A collider wrapping a freshly built hull.</returns>
    /// <remarks>
    /// Each call produces a new <see cref="DecomposedHulls"/>, and Stride keys its cache of built Bepu
    /// hulls on that instance, so calling this once per body builds one Bepu hull per body. Share a
    /// single <see cref="DecomposedHulls"/> instead when many bodies use the same shape - see
    /// <see cref="Colliders.SharedHullCache"/>.
    /// </remarks>
    public static ConvexHullCollider ToConvexHullCollider(this GeometricMeshData<VertexPositionNormalTexture> meshData)
        => new() { Hull = meshData.ToDecomposedHulls() };

    /// <summary>
    /// Extracts the hull data from the raw mesh data, without wrapping it in a collider.
    /// </summary>
    /// <param name="meshData">The mesh whose vertices define the hull.</param>
    /// <returns>The hull data, suitable for sharing across many colliders.</returns>
    /// <remarks>
    /// Useful when the same shape is used by many bodies: build the data once and assign the same
    /// instance to every <see cref="ConvexHullCollider.Hull"/>, so Stride builds and stores exactly one
    /// Bepu hull for all of them.
    /// </remarks>
    public static DecomposedHulls ToDecomposedHulls(this GeometricMeshData<VertexPositionNormalTexture> meshData)
    {
        ArgumentNullException.ThrowIfNull(meshData);

        var vertices = meshData.Vertices;
        var indices = meshData.Indices;

        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(indices);

        var points = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            points[i] = vertices[i].Position;
        }

        var uintIndices = new uint[indices.Length];
        for (int i = 0; i < indices.Length; i++)
        {
            uintIndices[i] = (uint)indices[i];
        }

        return new DecomposedHulls([new DecomposedMesh([new Hull(points, uintIndices)])]);
    }
}
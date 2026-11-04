using Stride.Graphics;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

/// <summary>
/// The hand-out of a cached mesh. <see cref="Stride.Rendering.ProceduralModels.PrimitiveProceduralModelBase.Generate(Stride.Core.IServiceRegistry, Stride.Rendering.Model)"/>
/// applies <c>LocalOffset</c> and <c>Scale</c> to the vertices in place, so a model whose settings would do that gets
/// a copy of the vertices and the cache keeps the original; every other model shares the cached instance.
/// </summary>
internal static class CachedMeshData
{
    /// <summary>
    /// The cached data itself, or a copy with its own vertex array when <paramref name="localOffset"/> or
    /// <paramref name="scale"/> would be applied to the vertices.
    /// </summary>
    /// <param name="cached">The mesh data from the cache.</param>
    /// <param name="localOffset">The model's <c>LocalOffset</c>.</param>
    /// <param name="scale">The model's <c>Scale</c>.</param>
    /// <returns>The mesh data the model may hand on to be mutated.</returns>
    internal static GeometricMeshData<VertexPositionNormalTexture> ForModel(GeometricMeshData<VertexPositionNormalTexture> cached, Vector3 localOffset, Vector3 scale)
    {
        if (localOffset == Vector3.Zero && scale == Vector3.One) return cached;

        return new GeometricMeshData<VertexPositionNormalTexture>([.. cached.Vertices], cached.Indices, cached.IsLeftHanded) { Name = cached.Name };
    }
}
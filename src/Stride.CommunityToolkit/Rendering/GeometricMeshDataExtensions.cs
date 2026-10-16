using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// Turns CPU mesh data into something a <see cref="Mesh"/> can draw.
/// </summary>
public static class GeometricMeshDataExtensions
{
    /// <summary>
    /// Uploads the vertices and indices to the GPU and wraps them in a <see cref="MeshDraw"/>.
    /// </summary>
    /// <param name="meshData">The mesh data, such as a procedural model's or <see cref="Text.TextMesh"/>'s.</param>
    /// <param name="device">The graphics device the buffers are created on.</param>
    /// <returns>A draw with a 32-bit index buffer, ready for a <see cref="Mesh"/>.</returns>
    /// <exception cref="ArgumentNullException">The mesh data or the device is null.</exception>
    /// <remarks>
    /// The caller owns the buffers: nothing tracks them, so dispose <see cref="MeshDraw.VertexBuffers"/>
    /// and <see cref="MeshDraw.IndexBuffer"/> when the mesh goes, or a mesh rebuilt every frame leaks a
    /// buffer pair per rebuild.
    /// </remarks>
    public static MeshDraw ToMeshDraw(this GeometricMeshData<VertexPositionNormalTexture> meshData, GraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(meshData);
        ArgumentNullException.ThrowIfNull(device);

        var vertexBuffer = Buffer.Vertex.New(device, meshData.Vertices);
        var indexBuffer = Buffer.Index.New(device, meshData.Indices);

        return new MeshDraw
        {
            PrimitiveType = PrimitiveType.TriangleList,
            DrawCount = meshData.Indices.Length,
            VertexBuffers = [new VertexBufferBinding(vertexBuffer, VertexPositionNormalTexture.Layout, meshData.Vertices.Length)],
            IndexBuffer = new IndexBufferBinding(indexBuffer, is32Bit: true, meshData.Indices.Length),
        };
    }
}
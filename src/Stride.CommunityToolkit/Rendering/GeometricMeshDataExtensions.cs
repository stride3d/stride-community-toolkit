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
    /// <exception cref="ArgumentException">The mesh data has no vertices or no indices: there is nothing to upload, and a GPU buffer cannot be empty. Remove the mesh instead.</exception>
    /// <remarks>
    /// The caller owns the buffers: nothing tracks them, so dispose <see cref="MeshDraw.VertexBuffers"/>
    /// and <see cref="MeshDraw.IndexBuffer"/> when the mesh goes, or a mesh rebuilt every frame leaks a
    /// buffer pair per rebuild. <see cref="ToMesh"/> adds the bounds a <see cref="MeshDraw"/> cannot carry.
    /// </remarks>
    public static MeshDraw ToMeshDraw(this GeometricMeshData<VertexPositionNormalTexture> meshData, GraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(meshData);
        ArgumentNullException.ThrowIfNull(device);

        if (meshData.Vertices.Length == 0 || meshData.Indices.Length == 0)
            throw new ArgumentException("The mesh data is empty, and a GPU buffer cannot be: when text or a shape has nothing to draw, remove its mesh rather than uploading it.", nameof(meshData));

        var vertexBuffer = Buffer.Vertex.New(device, meshData.Vertices);

        Buffer indexBuffer;

        try
        {
            indexBuffer = Buffer.Index.New(device, meshData.Indices);
        }
        catch
        {
            vertexBuffer.Dispose();
            throw;
        }

        return new MeshDraw
        {
            PrimitiveType = PrimitiveType.TriangleList,
            DrawCount = meshData.Indices.Length,
            VertexBuffers = [new VertexBufferBinding(vertexBuffer, VertexPositionNormalTexture.Layout, meshData.Vertices.Length)],
            IndexBuffer = new IndexBufferBinding(indexBuffer, is32Bit: true, meshData.Indices.Length),
        };
    }

    /// <summary>
    /// Uploads the mesh data and wraps it in a <see cref="Mesh"/> with its bounds, which the renderer uses
    /// to cull it when it is off screen; a <see cref="MeshDraw"/> alone has none and is never culled.
    /// </summary>
    /// <param name="meshData">The mesh data.</param>
    /// <param name="device">The graphics device the buffers are created on.</param>
    /// <param name="materialIndex">The index of the material the mesh uses in its model.</param>
    /// <returns>The mesh; the caller owns its draw's buffers, see <see cref="ToMeshDraw"/>.</returns>
    /// <exception cref="ArgumentNullException">The mesh data or the device is null.</exception>
    /// <exception cref="ArgumentException">The mesh data is empty.</exception>
    public static Mesh ToMesh(this GeometricMeshData<VertexPositionNormalTexture> meshData, GraphicsDevice device, int materialIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(meshData);

        var (box, sphere) = Bounds(meshData);

        return new Mesh { Draw = meshData.ToMeshDraw(device), MaterialIndex = materialIndex, BoundingBox = box, BoundingSphere = sphere };
    }

    /// <summary>
    /// The box and the sphere around the vertices.
    /// </summary>
    /// <param name="meshData">The mesh data.</param>
    /// <returns>The bounds; empty when there are no vertices.</returns>
    /// <exception cref="ArgumentNullException">The mesh data is null.</exception>
    public static (BoundingBox Box, BoundingSphere Sphere) Bounds(this GeometricMeshData<VertexPositionNormalTexture> meshData)
    {
        ArgumentNullException.ThrowIfNull(meshData);

        if (meshData.Vertices.Length == 0) return (BoundingBox.Empty, new BoundingSphere());

        var box = BoundingBox.Empty;

        foreach (var vertex in meshData.Vertices)
        {
            var position = vertex.Position;

            BoundingBox.Merge(ref box, ref position, out box);
        }

        var centre = box.Center;
        var radiusSquared = 0f;

        foreach (var vertex in meshData.Vertices) radiusSquared = MathF.Max(radiusSquared, (vertex.Position - centre).LengthSquared());

        return (box, new BoundingSphere(centre, MathF.Sqrt(radiusSquared)));
    }
}
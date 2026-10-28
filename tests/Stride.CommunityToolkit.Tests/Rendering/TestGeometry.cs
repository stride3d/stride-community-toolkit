using Stride.CommunityToolkit.Rendering.Text.Meshes;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// What the mesh and text tests reach for again and again: a square polygon, the embedded font's
/// bytes, and the width of one face of a lettering mesh.
/// </summary>
internal static class TestGeometry
{
    /// <summary>A counter-clockwise square with its lower-left corner at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    internal static Vector2[] Square(float x, float y, float size) => [new(x, y), new(x + size, y), new(x + size, y + size), new(x, y + size)];

    /// <summary>The embedded Open Sans, as a fresh array each call, for loading a second font from the same bytes.</summary>
    internal static byte[] DefaultFontBytes()
    {
        using var stream = typeof(MeshFont).Assembly.GetManifestResourceStream("Stride.CommunityToolkit.Rendering.Text.Fonts.OpenSans-Regular.ttf")!;
        var bytes = new byte[stream.Length];

        stream.ReadExactly(bytes);

        return bytes;
    }

    /// <summary>The X extent of the vertices a predicate picks out: a cap, a wall, a dot or a stem.</summary>
    internal static float Width(GeometricMeshData<VertexPositionNormalTexture> data, Func<VertexPositionNormalTexture, bool> face)
    {
        var picked = data.Vertices.Where(face).ToList();

        return picked.Max(v => v.Position.X) - picked.Min(v => v.Position.X);
    }
}
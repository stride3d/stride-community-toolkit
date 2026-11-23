using Stride.CommunityToolkit.Rendering.Text.Meshes;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// What the mesh and text tests reach for again and again: a square polygon and its reverse, the area
/// of a triangulation, the embedded font's bytes, and the width of one face of a lettering mesh.
/// </summary>
internal static class TestGeometry
{
    /// <summary>A counter-clockwise square with its lower-left corner at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    internal static Vector2[] Square(float x, float y, float size) => [new(x, y), new(x + size, y), new(x + size, y + size), new(x, y + size)];

    /// <summary>The same corners in the opposite order, which turns a counter-clockwise polygon clockwise.</summary>
    internal static Vector2[] Reversed(Vector2[] polygon) => [.. polygon.Reverse()];

    /// <summary>
    /// The area a triangulation covers, asserting on the way that every triangle has an area and winds
    /// counter-clockwise.
    /// </summary>
    internal static float AreaOf(IReadOnlyList<Vector2> corners, List<int> triangles)
    {
        var totalArea = 0f;

        for (var i = 0; i < triangles.Count; i += 3)
        {
            var a = corners[triangles[i]];
            var b = corners[triangles[i + 1]];
            var c = corners[triangles[i + 2]];
            var doubleArea = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

            Assert.True(doubleArea > 0, $"Triangle {i / 3} is degenerate or wound clockwise");

            totalArea += doubleArea * 0.5f;
        }

        return totalArea;
    }

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
using Stride.Core.Mathematics;
using Stride.Graphics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// What a closed lettering mesh must satisfy, asserted by the text mesh, bevel and review tests alike:
/// every triangle wound outwards, and every edge shared by two triangles.
/// </summary>
internal static class MeshAssertions
{
    /// <summary>
    /// Every triangle winds clockwise as seen from the side its normal points to, which is the side
    /// Stride draws: its geometric normal must point against its lighting normal, for every triangle
    /// with any area.
    /// </summary>
    internal static void AssertOutwardWinding(GeometricMeshData<VertexPositionNormalTexture> data)
    {
        var inward = 0;

        for (var i = 0; i < data.Indices.Length; i += 3)
        {
            var a = data.Vertices[data.Indices[i]];
            var b = data.Vertices[data.Indices[i + 1]];
            var c = data.Vertices[data.Indices[i + 2]];
            var geometric = Vector3.Cross(b.Position - a.Position, c.Position - b.Position);

            if (geometric.LengthSquared() < 1e-16f) continue;

            if (Vector3.Dot(geometric, a.Normal + b.Normal + c.Normal) >= 0f) inward++;
        }

        Assert.True(inward == 0, $"{inward} triangles wind the wrong way against their normals");
    }

    /// <summary>
    /// Every edge of a closed surface is shared by exactly two triangles, one in each direction. The
    /// caps share their corners and the walls own their vertices, so edges are matched by position,
    /// exactly: the parts are built from the same corners and the same depths, and a hair-wide gap
    /// between two of them is a gap. Two closed pieces that touch (the ogonek of an ą) share an edge,
    /// which then appears twice each way; what matters is that each way is matched by the other.
    /// </summary>
    internal static void AssertWatertight(GeometricMeshData<VertexPositionNormalTexture> data)
    {
        Assert.NotEmpty(data.Vertices);
        Assert.Equal(0, data.Indices.Length % 3);
        Assert.All(data.Indices, i => Assert.InRange(i, 0, data.Vertices.Length - 1));

        var edges = new Dictionary<(Vector3, Vector3), int>();

        for (var i = 0; i < data.Indices.Length; i += 3)
        {
            for (var k = 0; k < 3; k++)
            {
                var a = data.Vertices[data.Indices[i + k]].Position;
                var b = data.Vertices[data.Indices[i + (k + 1) % 3]].Position;

                edges[(a, b)] = edges.GetValueOrDefault((a, b)) + 1;
            }
        }

        foreach (var ((a, b), count) in edges)
        {
            Assert.True(count == edges.GetValueOrDefault((b, a)), $"edge {a} -> {b} appears {count} times and {edges.GetValueOrDefault((b, a))} times the other way: an open surface");
        }
    }
}
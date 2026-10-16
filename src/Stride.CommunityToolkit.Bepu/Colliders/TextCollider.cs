using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using static Stride.BepuPhysics.Definitions.DecomposedHulls;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Builds the collider of solid lettering: one convex hull per glyph, placed where the glyph sits in
/// the text, inside a single <see cref="ConvexHullCollider"/>.
/// </summary>
/// <remarks>
/// A hull fills a glyph's holes and its concave bays, so an O collides as a disc and a C as a D; for
/// letters that tumble and pile up that is right, and it is cheap. Lettering that must be walked into
/// or shot through its holes wants a <c>MeshCollider</c> on a static instead.
/// </remarks>
public static class TextCollider
{
    /// <summary>
    /// Creates the collider of <paramref name="text"/>.
    /// </summary>
    /// <param name="text">The lettering, as <c>Primitive3D.Text</c> makes it.</param>
    /// <returns>A collider with one hull per glyph.</returns>
    /// <exception cref="ArgumentNullException">The shape is null.</exception>
    /// <exception cref="ArgumentException">The text has no glyph with a shape: nothing to collide with.</exception>
    public static ConvexHullCollider Create(Primitive3D.TextShape text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return new ConvexHullCollider { Hull = CreateHulls(text.Font, text.Content, text.Options) };
    }

    /// <summary>
    /// The hulls of a string, one per glyph, for sharing between every body that shows the same lettering.
    /// </summary>
    /// <param name="font">The font.</param>
    /// <param name="text">The text.</param>
    /// <param name="options">Size, depth and layout, the same as the mesh's.</param>
    /// <returns>The hulls, placed where the glyphs sit.</returns>
    /// <exception cref="ArgumentNullException">The font or the text is null.</exception>
    /// <exception cref="ArgumentException">The text has no glyph with a shape.</exception>
    public static DecomposedHulls CreateHulls(MeshFont font, string text, TextMeshOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(text);

        var glyphs = TextMesh.CreateGlyphs(font, text, options);

        if (glyphs.Count == 0)
            throw new ArgumentException("The text has no glyph with a shape, so there is nothing to collide with.", nameof(text));

        var hulls = new Hull[glyphs.Count];

        for (var g = 0; g < glyphs.Count; g++)
        {
            var glyph = glyphs[g];
            var vertices = glyph.Mesh.Vertices;
            var points = new Vector3[vertices.Length];

            for (var i = 0; i < vertices.Length; i++) points[i] = vertices[i].Position + glyph.Position;

            var indices = new uint[glyph.Mesh.Indices.Length];

            for (var i = 0; i < indices.Length; i++) indices[i] = (uint)glyph.Mesh.Indices[i];

            hulls[g] = new Hull(points, indices);
        }

        return new DecomposedHulls([new DecomposedMesh(hulls)]);
    }
}
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Rendering.Text.Meshes;
using Stride.Core.Mathematics;
using Stride.Graphics;
using static Stride.BepuPhysics.Definitions.DecomposedHulls;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Builds the collider of solid lettering: one convex hull per glyph, placed where the glyph sits in
/// the text, inside a single <see cref="ConvexHullCollider"/>.
/// </summary>
/// <remarks>
/// <para>
/// A hull fills a glyph's holes and its concave bays, so an O collides as a disc and a C as a D; for
/// letters that tumble and pile up that is right, and it is cheap. Lettering that must be walked into
/// or shot through its holes wants a <c>MeshCollider</c> on a static instead.
/// </para>
/// <para>
/// The hulls of a string are kept with the font that shaped them, keyed by the text and the options,
/// so a word dropped a hundred times shares one set and Stride's own cache of built Bepu hulls hits.
/// Two fonts of the same name are two fonts: each has its own store, released with it. A store holds
/// 64 strings and starts again when a 65th arrives; a counter that changes every frame does not fill
/// it for ever.
/// </para>
/// <para>
/// A mass on the options is the word's: the toolkit divides it among the hulls, since Bepu adds a
/// hull's mass once per hull.
/// </para>
/// </remarks>
public static class TextCollider
{
    private const int Capacity = 64;

    private static readonly ConditionalWeakTable<MeshFont, ConcurrentDictionary<HullKey, DecomposedHulls>> Stores = new();

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
    /// The hulls of a string, one per glyph, shared between every body that shows the same lettering
    /// with the same options.
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

        options ??= new TextMeshOptions();

        var store = Stores.GetValue(font, _ => new ConcurrentDictionary<HullKey, DecomposedHulls>());
        var key = HullKey.For(text, options);

        if (store.TryGetValue(key, out var cached)) return cached;

        var hulls = Build(font, text, options);

        if (store.Count >= Capacity) store.Clear();

        return store.GetOrAdd(key, hulls);
    }

    /// <summary>
    /// How many hulls a collider holds: one per glyph for lettering, which is what a mass given for the
    /// whole word is divided by.
    /// </summary>
    /// <param name="collider">The collider.</param>
    /// <returns>The number of hulls, at least 1.</returns>
    /// <exception cref="ArgumentNullException">The collider is null.</exception>
    public static int HullCount(ConvexHullCollider collider)
    {
        ArgumentNullException.ThrowIfNull(collider);

        var count = 0;

        foreach (var mesh in collider.Hull.Meshes) count += mesh.Hulls.Length;

        return Math.Max(count, 1);
    }

    private static DecomposedHulls Build(MeshFont font, string text, TextMeshOptions options)
    {
        var glyphs = TextMesh.CreateGlyphs(font, text, options);

        if (glyphs.Count == 0)
            throw new ArgumentException("The text has no glyph with a shape, so there is nothing to collide with.", nameof(text));

        var hulls = new Hull[glyphs.Count];

        for (var g = 0; g < glyphs.Count; g++)
        {
            var glyph = glyphs[g];
            var vertices = glyph.Mesh.Vertices;

            // The mesh repeats a position for every wall and cap that meets there; the hull wants each
            // once, and the indices (which draw the debug shape) are remapped to the unique points
            var unique = new Dictionary<Vector3, uint>(vertices.Length);
            var remap = new uint[vertices.Length];
            var points = new List<Vector3>(vertices.Length / 3);

            for (var i = 0; i < vertices.Length; i++)
            {
                var position = vertices[i].Position + glyph.Position;

                if (!unique.TryGetValue(position, out var index))
                {
                    index = (uint)points.Count;
                    unique[position] = index;
                    points.Add(position);
                }

                remap[i] = index;
            }

            var indices = new uint[glyph.Mesh.Indices.Length];

            for (var i = 0; i < indices.Length; i++) indices[i] = remap[glyph.Mesh.Indices[i]];

            hulls[g] = new Hull([.. points], indices);
        }

        return new DecomposedHulls([new DecomposedMesh(hulls)]);
    }

    /// <summary>A snapshot of everything but the font that shapes the hulls; the options object itself can change after the call.</summary>
    private readonly record struct HullKey(string Text, float Size, float Depth, int CurveQuality, TextAnchor? Anchor, float LetterSpacing, float LineSpacing, TextAlignment Alignment, bool Kerning, float BevelSize, float BevelThickness, int BevelSegments)
    {
        internal static HullKey For(string text, TextMeshOptions options)
            => new(text, options.Size, options.Depth, options.CurveQuality, options.Anchor, options.LetterSpacing, options.LineSpacing, options.Alignment, options.Kerning,
                options.Bevel?.Size ?? 0f, options.Bevel?.Thickness ?? 0f, options.Bevel?.Segments ?? 0);
    }
}
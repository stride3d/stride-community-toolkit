using Stride.Graphics;
using Stride.Rendering;

namespace Stride.CommunityToolkit.Rendering.Utilities;

/// <summary>
/// Builds solid, extruded 3D letter and digit meshes from glyph outlines authored in code.
/// </summary>
/// <remarks>
/// <para>
/// This exists because Stride exposes no font glyph outlines at runtime, and the libraries that do
/// extract them go through DirectWrite, which is Windows-only - the wrong trade for a cross-platform
/// toolkit. Authoring each glyph as a set of small 2D polygons sidesteps fonts entirely: the outlines
/// here work on every platform Stride runs on, at the cost of supporting only the characters somebody
/// has drawn. See <see cref="SupportedCharacters"/> for what exists so far.
/// </para>
/// <para>
/// For ordinary text - labels, HUDs, signs - use <c>EntityTextComponent</c> or
/// <c>WorldTextComponent</c> instead; they render any string in a real font. This is for lettering
/// that is meant to be <em>geometry</em>: a title that catches the light, casts a shadow, tumbles
/// under physics, and takes a material like any other mesh.
/// </para>
/// <para>
/// A glyph is one or more simple polygons that touch along their edges but never overlap. That is
/// how holes work without hole-aware triangulation: an O is four bars meeting at their corners, an 8
/// is seven. Abutting pieces render seamlessly because their shared walls end up sealed inside the
/// solid; <em>overlapping</em> pieces would put two caps on the same plane and shimmer, which is why
/// the segment bands below share exact coordinates.
/// </para>
/// </remarks>
public static class LetterMeshFactory
{
    /// <summary>Stroke thickness of the glyphs, in glyph units.</summary>
    private const float Stroke = 0.22f;

    /// <summary>Width of a glyph box; glyphs are drawn 1 unit tall.</summary>
    private const float Width = 0.7f;

    // The segment grid every bar-built glyph is composed on. Sharing these exact values is what
    // guarantees pieces abut instead of overlapping.
    private const float TopY = 1f - Stroke;         // top bar: [TopY, 1]
    private const float MiddleTopY = 0.61f;         // middle bar: [MiddleY, MiddleTopY]
    private const float MiddleY = 0.39f;
    private const float BottomY = Stroke;           // bottom bar: [0, BottomY]
    private const float RightX = Width - Stroke;    // right column: [RightX, Width]

    /// <summary>
    /// Gets the characters that have an authored glyph. Space is also accepted and advances without
    /// drawing; lookups are case-insensitive.
    /// </summary>
    public static string SupportedCharacters => "0123456789-ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>
    /// Builds one mesh containing the given text as solid extruded glyphs.
    /// </summary>
    /// <param name="device">The graphics device the mesh buffers are created on.</param>
    /// <param name="text">The text to build. Every character must be in <see cref="SupportedCharacters"/>, or a space.</param>
    /// <param name="depth">Glyph depth along Z, in glyph units. Defaults to 0.25.</param>
    /// <param name="spacing">Gap between glyphs, in glyph units. Defaults to 0.15.</param>
    /// <param name="centerOrigin">
    /// When <see langword="true"/>, the whole string is centred on the entity origin instead of
    /// starting at it - which is what a physics body wants, so the collider and the visual share a
    /// centre and the mesh tumbles about its middle.
    /// </param>
    /// <returns>
    /// A mesh draw of the whole string, centred on Z = 0. With <paramref name="centerOrigin"/> false
    /// the baseline is at Y = 0 and the pen starts at X = 0; with it true the string's centre sits on
    /// the origin.
    /// </returns>
    /// <exception cref="ArgumentNullException">The device or text is null.</exception>
    /// <exception cref="ArgumentException">The text is empty, or contains a character with no authored glyph.</exception>
    /// <remarks>
    /// The caller owns the returned draw's GPU buffers, exactly as with
    /// <see cref="MeshBuilder.ToMeshDraw"/>.
    /// </remarks>
    public static MeshDraw CreateTextMeshDraw(GraphicsDevice device, string text, float depth = 0.25f, float spacing = 0.15f, bool centerOrigin = false)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
            throw new ArgumentException("Text must contain at least one character", nameof(text));

        using var builder = new MeshBuilder
        {
            IndexType = IndexingType.Int32,
            PrimitiveType = PrimitiveType.TriangleList,
        };

        var position = builder.WithPosition<Vector3>();
        var normal = builder.WithNormal<Vector3>();

        var origin = Vector2.Zero;

        if (centerOrigin)
        {
            var totalWidth = text.Length * Width + (text.Length - 1) * spacing;

            origin = new Vector2(-totalWidth / 2, -0.5f);
        }

        var penX = 0f;

        foreach (var character in text)
        {
            if (character == ' ')
            {
                penX += Width + spacing;
                continue;
            }

            if (!TryGetOutlines(character, out var outlines))
            {
                throw new ArgumentException(
                    $"No glyph is authored for '{character}'. Supported characters: {SupportedCharacters}",
                    nameof(text));
            }

            foreach (var outline in outlines)
            {
                builder.AddExtrudedPolygon(outline, depth, position, normal, origin + new Vector2(penX, 0));
            }

            penX += Width + spacing;
        }

        return builder.ToMeshDraw(device);
    }

    /// <summary>
    /// Returns the authored glyph for a character: one or more simple polygons in a 1-unit-tall box
    /// with the pen at the bottom-left.
    /// </summary>
    /// <param name="character">The character to look up. Case-insensitive.</param>
    /// <param name="outlines">The glyph's polygons, each with corners in order.</param>
    /// <returns><see langword="true"/> when the character has a glyph.</returns>
    public static bool TryGetOutlines(char character, out Vector2[][] outlines)
    {
        outlines = char.ToUpperInvariant(character) switch
        {
            '0' or 'O' => [Top(), Bottom(), LeftBar(BottomY, TopY), RightBar(BottomY, TopY)],
            '1' => [Rect(0.24f, 0, 0.46f, 1)],
            '2' => [Top(), RightBar(MiddleTopY, TopY), Middle(), LeftBar(BottomY, MiddleY), Bottom()],
            '3' => [Top(), RightBar(MiddleTopY, TopY), Middle(), RightBar(BottomY, MiddleY), Bottom()],
            '4' => [LeftBar(MiddleTopY, 1), Middle(), RightBar(MiddleTopY, 1), RightBar(0, MiddleY)],
            '5' => [Top(), LeftBar(MiddleTopY, TopY), Middle(), RightBar(BottomY, MiddleY), Bottom()],
            '6' => [Top(), LeftBar(MiddleTopY, TopY), Middle(), LeftBar(BottomY, MiddleY), RightBar(BottomY, MiddleY), Bottom()],
            '7' => [Top(), RightBar(0, TopY)],
            // B shares 8's glyph, as O shares 0's - the classic segment-display compromise
            '8' or 'B' => [Top(), Middle(), Bottom(), LeftBar(MiddleTopY, TopY), LeftBar(BottomY, MiddleY), RightBar(MiddleTopY, TopY), RightBar(BottomY, MiddleY)],
            '9' => [Top(), LeftBar(MiddleTopY, TopY), Middle(), RightBar(MiddleTopY, TopY), RightBar(BottomY, MiddleY), Bottom()],
            '-' => [Rect(0.08f, MiddleY, Width - 0.08f, MiddleTopY)],
            'A' => [Top(), Middle(), LeftBar(MiddleTopY, TopY), LeftBar(0, MiddleY), RightBar(MiddleTopY, TopY), RightBar(0, MiddleY)],
            'C' => [Top(), Bottom(), LeftBar(BottomY, TopY)],
            // D is an O with its right corners cut at 45 degrees, which is what tells them apart
            'D' => [Rect(0, 0, Stroke, 1), Rect(Stroke, TopY, 0.5f, 1), Rect(Stroke, 0, 0.5f, BottomY), RightBar(0.3f, 0.7f), CreateDTopCorner(), CreateDBottomCorner()],
            'E' => [Top(), Middle(), Bottom(), LeftBar(MiddleTopY, TopY), LeftBar(BottomY, MiddleY)],
            'F' => [Top(), Middle(), LeftBar(MiddleTopY, TopY), LeftBar(0, MiddleY)],
            'G' => [Top(), Bottom(), LeftBar(BottomY, TopY), RightBar(BottomY, MiddleY), Rect(0.35f, MiddleY, Width, MiddleTopY)],
            'H' => [Rect(0, 0, Stroke, 1), Rect(RightX, 0, Width, 1), Rect(Stroke, MiddleY, RightX, MiddleTopY)],
            'I' => [Top(), Bottom(), Rect(0.24f, BottomY, 0.46f, TopY)],
            'J' => [Bottom(), RightBar(BottomY, 1), LeftBar(BottomY, 0.45f)],
            'K' => [Rect(0, 0, Stroke, 1), CreateKUpperArm(), CreateKLowerArm()],
            'L' => [Bottom(), LeftBar(BottomY, 1)],
            'M' => [Rect(0, 0, Stroke, 1), Rect(RightX, 0, Width, 1), Rect(Stroke, 0.6f, RightX, 1)],
            'N' => [Rect(0, 0, Stroke, 1), Rect(RightX, 0, Width, 1), CreateNDiagonal()],
            'P' => [Rect(0, 0, Stroke, 1), Rect(Stroke, TopY, Width, 1), RightBar(MiddleTopY, TopY), Rect(Stroke, MiddleY, Width, MiddleTopY)],
            // Q is an O with a tail tucked inside the counter: a triangle abutting the bottom bar's
            // top edge and the right bar's left edge, so nothing overlaps
            'Q' => [Top(), Bottom(), LeftBar(BottomY, TopY), RightBar(BottomY, TopY), CreateQTail()],
            'R' => [Rect(0, 0, Stroke, 1), Rect(Stroke, TopY, Width, 1), RightBar(MiddleTopY, TopY), Rect(Stroke, MiddleY, Width, MiddleTopY), CreateRLeg()],
            'S' => [Top(), LeftBar(MiddleTopY, TopY), Middle(), RightBar(BottomY, MiddleY), Bottom()],
            'T' => [Top(), Rect(0.24f, 0, 0.46f, TopY)],
            'U' => [Bottom(), LeftBar(BottomY, 1), RightBar(BottomY, 1)],
            'V' => [CreateV()],
            // W is a U with a centre stem rising from the bottom bar - a solid bottom block, like
            // M's top one, made it indistinguishable from U
            'W' => [Bottom(), LeftBar(BottomY, 1), RightBar(BottomY, 1), Rect(0.24f, BottomY, 0.46f, 0.65f)],
            'X' => [CreateX()],
            'Y' => [CreateY()],
            'Z' => [CreateZ()],
            _ => [],
        };

        return outlines.Length > 0;
    }

    // Bars on the shared segment grid. Winding does not matter when authoring - the extruder
    // normalizes it - so these are all plain bottom-left-first rectangles.

    private static Vector2[] Top() => Rect(0, TopY, Width, 1);

    private static Vector2[] Middle() => Rect(0, MiddleY, Width, MiddleTopY);

    private static Vector2[] Bottom() => Rect(0, 0, Width, BottomY);

    private static Vector2[] LeftBar(float fromY, float toY) => Rect(0, fromY, Stroke, toY);

    private static Vector2[] RightBar(float fromY, float toY) => Rect(RightX, fromY, Width, toY);

    private static Vector2[] Rect(float x0, float y0, float x1, float y1) =>
        [new(x0, y0), new(x1, y0), new(x1, y1), new(x0, y1)];

    // The non-rectangular glyphs, sketched on grid paper. Each is a single simple polygon.

    /// <summary>R's diagonal leg, hanging from the middle bar down to the bottom-right corner.</summary>
    private static Vector2[] CreateRLeg() =>
        [new(0.30f, MiddleY), new(0.48f, MiddleY), new(Width, 0), new(0.52f, 0)];

    /// <summary>
    /// Q's tail: a triangle inside the counter, abutting the bottom bar's top edge and the right
    /// bar's left edge.
    /// </summary>
    private static Vector2[] CreateQTail() =>
        [new(0.28f, BottomY), new(RightX, 0.42f), new(RightX, BottomY)];

    /// <summary>
    /// D's angled top-right corner: a band joining the shortened top bar to the shortened right bar.
    /// </summary>
    private static Vector2[] CreateDTopCorner() =>
        [new(0.5f, TopY), new(0.5f, 1), new(Width, 0.7f), new(RightX, 0.7f)];

    /// <summary>D's angled bottom-right corner, mirroring the top one.</summary>
    private static Vector2[] CreateDBottomCorner() =>
        [new(0.5f, BottomY), new(RightX, 0.3f), new(Width, 0.3f), new(0.5f, 0)];

    /// <summary>
    /// K's upper arm: from the spine's midpoint up to the top-right. Both arms leave the spine at
    /// the same point, so they touch without overlapping.
    /// </summary>
    private static Vector2[] CreateKUpperArm() =>
        [new(Stroke, 0.5f), new(Stroke, 0.66f), new(Width - 0.28f, 1), new(Width, 1)];

    /// <summary>K's lower arm: from the spine's midpoint down to the bottom-right.</summary>
    private static Vector2[] CreateKLowerArm() =>
        [new(Stroke, 0.34f), new(Stroke, 0.5f), new(Width, 0), new(Width - 0.28f, 0)];

    /// <summary>
    /// N's diagonal: a band from the top of the left spine to the bottom of the right one.
    /// </summary>
    private static Vector2[] CreateNDiagonal() =>
        [new(Stroke, 0.6f), new(Stroke, 1), new(RightX, 0.4f), new(RightX, 0)];

    private static Vector2[] CreateV() =>
        [new(0.22f, 0), new(0.48f, 0), new(Width, 1), new(0.46f, 1), new(0.35f, 0.40f), new(0.24f, 1), new(0, 1)];

    private static Vector2[] CreateX()
    {
        // Two crossing diagonal bars: four feet on the top and bottom edges, four concave notches
        // where the bars meet
        const float foot = 0.30f;
        const float notch = 0.21f;
        const float waist = 0.145f;
        const float centreX = Width / 2;

        return
        [
            new(0, 0),
            new(foot, 0),
            new(centreX, 0.5f - notch),
            new(Width - foot, 0),
            new(Width, 0),
            new(centreX + waist, 0.5f),
            new(Width, 1),
            new(Width - foot, 1),
            new(centreX, 0.5f + notch),
            new(foot, 1),
            new(0, 1),
            new(centreX - waist, 0.5f),
        ];
    }

    private static Vector2[] CreateY()
    {
        // A stem up to the middle, then two arms reaching the top corners
        const float halfStroke = Stroke / 2;
        const float foot = 0.30f;
        const float centreX = Width / 2;
        const float fork = 0.45f;

        return
        [
            new(centreX - halfStroke, 0),
            new(centreX + halfStroke, 0),
            new(centreX + halfStroke, fork),
            new(Width, 1),
            new(Width - foot, 1),
            new(centreX, fork + Stroke),
            new(foot, 1),
            new(0, 1),
            new(centreX - halfStroke, fork),
        ];
    }

    private static Vector2[] CreateZ()
    {
        // Top bar, bottom bar, and the diagonal band joining the top-right to the bottom-left
        const float band = 0.30f;

        return
        [
            new(0, 0),
            new(Width, 0),
            new(Width, Stroke),
            new(band, Stroke),
            new(Width, 1 - Stroke),
            new(Width, 1),
            new(0, 1),
            new(0, 1 - Stroke),
            new(Width - band, 1 - Stroke),
            new(0, Stroke),
        ];
    }
}
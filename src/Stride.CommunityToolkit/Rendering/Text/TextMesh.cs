using System.Text;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// Builds solid 3D lettering from a <see cref="MeshFont"/>: each glyph's outline is triangulated,
/// holes and all, and extruded along Z with a front cap, a back cap and flat-shaded walls.
/// </summary>
/// <remarks>
/// <para>
/// The result is real geometry: it is lit and shadowed, takes any material, casts a shadow, and can
/// carry a physics body (<c>Primitive3D.Text</c> gives it one through <c>CreatePrimitive</c>). For
/// ordinary text, labels and HUDs, <see cref="EntityTextComponent"/> and <see cref="WorldTextComponent"/>
/// draw a font far more cheaply; this is for lettering that is meant to be a thing in the world.
/// </para>
/// <para>
/// Vertices carry a position, a normal and texture coordinates: the caps are mapped across the text's
/// block (0..1 left to right, 0..1 top to bottom), each wall runs 0..1 along its contour and 0..1 from
/// the front to the back, so a texture or a shader can follow the lettering.
/// </para>
/// <para>
/// Layout is by the font's metrics: the first baseline at y = 0, lines <see cref="MeshFont.LineHeight"/>
/// apart going down, the pen advancing by each glyph's advance plus the letter spacing, kerned when the
/// font allows. <see cref="TextMeshOptions.Anchor"/> moves the block so a chosen corner or centre sits at
/// the origin.
/// </para>
/// </remarks>
public static class TextMesh
{
    /// <summary>
    /// Builds the lettering as one mesh.
    /// </summary>
    /// <param name="font">The font.</param>
    /// <param name="text">The text; <c>\n</c> starts a new line. Characters the font lacks draw its "missing glyph" box.</param>
    /// <param name="options">Size, depth, layout; the defaults are one unit per EM and a depth of 0.25.</param>
    /// <returns>The mesh data; <see cref="GeometricMeshDataExtensions.ToMeshDraw"/> uploads it. Empty when the text has nothing to draw.</returns>
    /// <exception cref="ArgumentNullException">The font or the text is null.</exception>
    public static GeometricMeshData<VertexPositionNormalTexture> Create(MeshFont font, string text, TextMeshOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(text);

        options ??= new TextMeshOptions();

        var layout = Layout.Build(font, text, options);
        var emitter = new Emitter(options.Depth);
        var block = layout.Block;

        foreach (var placed in layout.Glyphs)
        {
            emitter.AddGlyph(placed.Glyph.Flatten(options.CurveQuality), placed.Pen, placed.Pen + layout.Shift, options.Size, block.Min, block.Max);
        }

        return emitter.ToMeshData($"Text '{text}'");
    }

    /// <summary>
    /// Builds the lettering and uploads it.
    /// </summary>
    /// <param name="device">The graphics device the buffers are created on.</param>
    /// <param name="font">The font.</param>
    /// <param name="text">The text; <c>\n</c> starts a new line.</param>
    /// <param name="options">Size, depth, layout.</param>
    /// <returns>A draw for a <see cref="Mesh"/>; the caller owns its buffers, see <see cref="GeometricMeshDataExtensions.ToMeshDraw"/>.</returns>
    /// <exception cref="ArgumentNullException">The device, the font or the text is null.</exception>
    public static MeshDraw CreateMeshDraw(GraphicsDevice device, MeshFont font, string text, TextMeshOptions? options = null)
        => Create(font, text, options).ToMeshDraw(device);

    /// <summary>
    /// Builds the lettering one glyph at a time, each centred on itself and told where it belongs: for a
    /// body per letter, or letters that move on their own.
    /// </summary>
    /// <param name="font">The font.</param>
    /// <param name="text">The text; <c>\n</c> starts a new line.</param>
    /// <param name="options">Size, depth, layout; the anchor applies to the whole block, as in <see cref="Create"/>.</param>
    /// <returns>One mesh per glyph that draws something, in text order; spaces are left out.</returns>
    /// <exception cref="ArgumentNullException">The font or the text is null.</exception>
    public static IReadOnlyList<GlyphMesh> CreateGlyphs(MeshFont font, string text, TextMeshOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(text);

        options ??= new TextMeshOptions();

        var layout = Layout.Build(font, text, options);
        var glyphs = new List<GlyphMesh>(layout.Glyphs.Count);

        foreach (var placed in layout.Glyphs)
        {
            var shape = placed.Glyph.Flatten(options.CurveQuality);

            if (shape.IsEmpty) continue;

            var origin = placed.Pen + layout.Shift;
            var centre = origin + (placed.Glyph.BoundsMin + placed.Glyph.BoundsMax) * 0.5f;
            var emitter = new Emitter(options.Depth);

            emitter.AddGlyph(shape, Vector2.Zero, origin - centre, options.Size, placed.Glyph.BoundsMin, placed.Glyph.BoundsMax);

            glyphs.Add(new GlyphMesh(placed.Glyph.Codepoint, new Vector3(centre * options.Size, 0f), emitter.ToMeshData($"Glyph '{char.ConvertFromUtf32(placed.Glyph.Codepoint)}'")));
        }

        return glyphs;
    }

    /// <summary>
    /// The box the lettering occupies, in world units, without building it: the font's ascent to its
    /// descent over every line, the widest line across, the depth through, moved by the anchor.
    /// </summary>
    /// <param name="font">The font.</param>
    /// <param name="text">The text.</param>
    /// <param name="options">Size, depth, layout.</param>
    /// <returns>The box.</returns>
    /// <exception cref="ArgumentNullException">The font or the text is null.</exception>
    public static BoundingBox Measure(MeshFont font, string text, TextMeshOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(text);

        options ??= new TextMeshOptions();

        var layout = Layout.Build(font, text, options);
        var min = (layout.Block.Min + layout.Shift) * options.Size;
        var max = (layout.Block.Max + layout.Shift) * options.Size;

        return new BoundingBox(new Vector3(min, -options.Depth * 0.5f), new Vector3(max, options.Depth * 0.5f));
    }

    /// <summary>A glyph and where its pen sits, in EM units before the anchor shift.</summary>
    private readonly record struct PlacedGlyph(GlyphOutline Glyph, Vector2 Pen);

    /// <summary>A box in EM units.</summary>
    private readonly record struct Block(Vector2 Min, Vector2 Max);

    /// <summary>Where every glyph goes: the pen positions, the block they make, and the shift the anchor asks for.</summary>
    private sealed class Layout
    {
        private Layout(List<PlacedGlyph> glyphs, Block block, Vector2 shift)
        {
            Glyphs = glyphs;
            Block = block;
            Shift = shift;
        }

        internal List<PlacedGlyph> Glyphs { get; }

        internal Block Block { get; }

        internal Vector2 Shift { get; }

        /// <summary>
        /// Lays the text out line by line: the pen runs right along each line by the glyphs' advances,
        /// kerning and spacing, lines go down by the line height, then shorter lines are pushed right by
        /// the alignment, and the anchor says how far the whole block moves.
        /// </summary>
        internal static Layout Build(MeshFont font, string text, TextMeshOptions options)
        {
            var lines = text.Replace("\r", "", StringComparison.Ordinal).Split('\n');
            var lineHeight = font.LineHeight * options.LineSpacing;
            var placed = new List<PlacedGlyph>(text.Length);
            var lineWidths = new float[lines.Length];
            var lineStarts = new int[lines.Length];

            // First pass: every glyph at its pen position, every line starting at x = 0
            for (var line = 0; line < lines.Length; line++)
            {
                var pen = 0f;
                var previous = -1;

                lineStarts[line] = placed.Count;

                foreach (var rune in lines[line].EnumerateRunes())
                {
                    // Tabs and the like have no glyph; a character beyond the basic plane is one rune, not two chars
                    if (Rune.IsControl(rune)) continue;

                    var glyph = font.GetGlyph(rune.Value);

                    if (options.Kerning && previous >= 0) pen += font.GetKerning(previous, rune.Value);

                    placed.Add(new PlacedGlyph(glyph, new Vector2(pen, -line * lineHeight)));

                    pen += glyph.Advance + options.LetterSpacing;
                    previous = rune.Value;
                }

                // The line ends with its last glyph, not with the spacing after it
                lineWidths[line] = previous >= 0 ? pen - options.LetterSpacing : 0f;
            }

            // Second pass: the alignment moves each line by what it lacks against the widest
            var width = lineWidths.Max();

            for (var line = 0; line < lines.Length; line++)
            {
                var offset = options.Alignment switch
                {
                    TextAlignment.Center => (width - lineWidths[line]) * 0.5f,
                    TextAlignment.Right => width - lineWidths[line],
                    _ => 0f,
                };

                if (offset == 0f) continue;

                var end = line + 1 < lines.Length ? lineStarts[line + 1] : placed.Count;

                for (var i = lineStarts[line]; i < end; i++) placed[i] = placed[i] with { Pen = placed[i].Pen + new Vector2(offset, 0f) };
            }

            var block = new Block(new Vector2(0f, -(lines.Length - 1) * lineHeight + font.Descent), new Vector2(width, font.Ascent));

            return new Layout(placed, block, ShiftFor(options.Anchor, block));
        }

        private static Vector2 ShiftFor(TextAnchor? anchor, Block block)
        {
            if (anchor is null) return Vector2.Zero;

            var x = anchor switch
            {
                TextAnchor.TopLeft or TextAnchor.MiddleLeft or TextAnchor.BottomLeft => block.Min.X,
                TextAnchor.TopRight or TextAnchor.MiddleRight or TextAnchor.BottomRight => block.Max.X,
                _ => (block.Min.X + block.Max.X) * 0.5f,
            };

            var y = anchor switch
            {
                TextAnchor.TopLeft or TextAnchor.TopCenter or TextAnchor.TopRight => block.Max.Y,
                TextAnchor.BottomLeft or TextAnchor.BottomCenter or TextAnchor.BottomRight => block.Min.Y,
                _ => (block.Min.Y + block.Max.Y) * 0.5f,
            };

            return new Vector2(-x, -y);
        }
    }

    /// <summary>Accumulates the caps and walls of extruded polygons into one vertex and index list.</summary>
    private sealed class Emitter
    {
        private readonly List<VertexPositionNormalTexture> _vertices = [];
        private readonly List<int> _indices = [];
        private readonly float _halfDepth;

        internal Emitter(float depth)
        {
            _halfDepth = depth * 0.5f;
        }

        /// <summary>
        /// Adds a flattened glyph: every polygon of it, placed at <paramref name="origin"/> and scaled by
        /// <paramref name="scale"/>, with cap texture coordinates spread over the box from
        /// <paramref name="uvMin"/> to <paramref name="uvMax"/>, in which the glyph sits at <paramref name="uvOrigin"/>.
        /// All of it in EM units apart from the scale.
        /// </summary>
        internal void AddGlyph(GlyphShape shape, Vector2 uvOrigin, Vector2 origin, float scale, Vector2 uvMin, Vector2 uvMax)
        {
            var offset = origin * scale;
            var uvSize = uvMax - uvMin;

            if (uvSize.X <= 0f) uvSize.X = 1f;
            if (uvSize.Y <= 0f) uvSize.Y = 1f;

            foreach (var polygon in shape.Polygons)
            {
                // Every corner of the polygon in the order the triangulation's indices expect: the outside, then each hole
                var corners = new List<Vector2>(polygon.Outer);

                foreach (var hole in polygon.Holes) corners.AddRange(hole);

                var triangles = EarClipping.Triangulate(polygon.Outer, polygon.Holes);

                // Front cap, facing +Z: the counter-clockwise triangulation must wind clockwise for the viewer there
                var front = _vertices.Count;

                foreach (var corner in corners) _vertices.Add(new VertexPositionNormalTexture(new Vector3(corner * scale + offset, _halfDepth), Vector3.UnitZ, CapUv(corner + uvOrigin, uvMin, uvSize)));

                for (var i = 0; i < triangles.Count; i += 3)
                {
                    _indices.Add(front + triangles[i + 2]);
                    _indices.Add(front + triangles[i + 1]);
                    _indices.Add(front + triangles[i]);
                }

                // Back cap, facing -Z, where the same order already reads clockwise
                var back = _vertices.Count;

                foreach (var corner in corners) _vertices.Add(new VertexPositionNormalTexture(new Vector3(corner * scale + offset, -_halfDepth), -Vector3.UnitZ, CapUv(corner + uvOrigin, uvMin, uvSize)));

                for (var i = 0; i < triangles.Count; i += 3)
                {
                    _indices.Add(back + triangles[i]);
                    _indices.Add(back + triangles[i + 1]);
                    _indices.Add(back + triangles[i + 2]);
                }

                AddWalls(polygon.Outer, offset, scale);

                foreach (var hole in polygon.Holes) AddWalls(hole, offset, scale);
            }
        }

        internal GeometricMeshData<VertexPositionNormalTexture> ToMeshData(string name)
            => new([.. _vertices], [.. _indices], isLeftHanded: false) { Name = name };

        private static Vector2 CapUv(Vector2 corner, Vector2 uvMin, Vector2 uvSize)
            => new((corner.X - uvMin.X) / uvSize.X, 1f - (corner.Y - uvMin.Y) / uvSize.Y);

        /// <summary>
        /// One flat-shaded quad per edge of a loop, with its own four vertices so the edges stay crisp.
        /// The loop is counter-clockwise for an outside and clockwise for a hole, so the same rule,
        /// rotate the edge to the right, points the wall away from the material either way.
        /// </summary>
        private void AddWalls(IReadOnlyList<Vector2> loop, Vector2 offset, float scale)
        {
            // The loop's length first, so u can run 0..1 around it
            var total = 0f;

            for (var i = 0; i < loop.Count; i++) total += (loop[(i + 1) % loop.Count] - loop[i]).Length();

            if (total <= 0f) return;

            var along = 0f;

            for (var i = 0; i < loop.Count; i++)
            {
                var from = loop[i];
                var to = loop[(i + 1) % loop.Count];
                var edge = to - from;
                var length = edge.Length();

                if (length < 1e-7f) continue;

                // The edge turned to the right is the wall's outward normal; u is where the edge sits
                // along the loop, and v runs from the front cap to the back
                var normal = new Vector3(edge.Y / length, -edge.X / length, 0f);
                var u0 = along / total;
                var u1 = (along + length) / total;
                var a = new Vector3(from * scale + offset, _halfDepth);
                var b = new Vector3(to * scale + offset, _halfDepth);
                var c = new Vector3(to * scale + offset, -_halfDepth);
                var d = new Vector3(from * scale + offset, -_halfDepth);
                var start = _vertices.Count;

                _vertices.Add(new VertexPositionNormalTexture(a, normal, new Vector2(u0, 0f)));
                _vertices.Add(new VertexPositionNormalTexture(b, normal, new Vector2(u1, 0f)));
                _vertices.Add(new VertexPositionNormalTexture(c, normal, new Vector2(u1, 1f)));
                _vertices.Add(new VertexPositionNormalTexture(d, normal, new Vector2(u0, 1f)));

                // Clockwise as seen from outside the wall, matching the caps
                _indices.Add(start);
                _indices.Add(start + 1);
                _indices.Add(start + 2);
                _indices.Add(start);
                _indices.Add(start + 2);
                _indices.Add(start + 3);

                along += length;
            }
        }
    }
}
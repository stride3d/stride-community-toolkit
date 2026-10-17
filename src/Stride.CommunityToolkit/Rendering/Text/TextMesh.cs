using System.Text;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// Builds solid 3D lettering from a <see cref="MeshFont"/>: each glyph's outline is triangulated,
/// holes and all, and extruded along Z with a front cap, a back cap and flat-shaded walls, with a
/// rounded edge between them when <see cref="TextMeshOptions.Bevel"/> asks for one.
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
        var emitter = new Emitter(options.Depth, options.Bevel);
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
            var emitter = new Emitter(options.Depth, options.Bevel);

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

    /// <summary>Accumulates the caps, bevels and walls of extruded polygons into one vertex and index list.</summary>
    private sealed class Emitter
    {
        private readonly List<VertexPositionNormalTexture> _vertices = [];
        private readonly List<int> _indices = [];
        private readonly float _halfDepth;
        private readonly TextBevel? _bevel;

        internal Emitter(float depth, TextBevel? bevel)
        {
            _halfDepth = depth * 0.5f;
            _bevel = bevel;
        }

        /// <summary>
        /// Adds a flattened glyph: every polygon of it, placed at <paramref name="origin"/> and scaled by
        /// <paramref name="scale"/>, with cap texture coordinates spread over the box from
        /// <paramref name="uvMin"/> to <paramref name="uvMax"/>, in which the glyph sits at <paramref name="uvOrigin"/>.
        /// All of it in EM units apart from the scale.
        /// </summary>
        internal void AddGlyph(GlyphShape shape, Vector2 uvOrigin, Vector2 origin, float scale, Vector2 uvMin, Vector2 uvMax)
        {
            var uv = new CapUv(uvOrigin, origin, scale, uvMin, uvMax);

            foreach (var polygon in shape.Polygons)
            {
                // The polygon in world units: the outside first, then each hole, which is the order the triangulation's indices expect
                var loops = new List<Vector2[]>(1 + polygon.Holes.Count) { Scale(polygon.Outer, origin, scale) };

                foreach (var hole in polygon.Holes) loops.Add(Scale(hole, origin, scale));

                if (_bevel is null)
                {
                    AddCaps(loops, _halfDepth, uv);

                    foreach (var loop in loops) AddWalls(loop, _halfDepth);
                }
                else
                {
                    AddBevelled(loops, _bevel, uv);
                }
            }
        }

        internal GeometricMeshData<VertexPositionNormalTexture> ToMeshData(string name)
            => new([.. _vertices], [.. _indices], isLeftHanded: false) { Name = name };

        private static Vector2[] Scale(IReadOnlyList<Vector2> loop, Vector2 origin, float scale)
        {
            var scaled = new Vector2[loop.Count];

            for (var i = 0; i < loop.Count; i++) scaled[i] = (loop[i] + origin) * scale;

            return scaled;
        }

        /// <summary>
        /// The front cap at +<paramref name="z"/> and the back cap at -<paramref name="z"/>, triangulated once
        /// and shared: the front reverses the counter-clockwise triangulation so it winds clockwise for the
        /// viewer at +Z, the back keeps it, where the same order already reads clockwise.
        /// </summary>
        private void AddCaps(List<Vector2[]> loops, float z, CapUv uv)
        {
            var corners = new List<Vector2>(loops.Sum(l => l.Length));

            foreach (var loop in loops) corners.AddRange(loop);

            // One triangulation serves both caps: the corners are the same, only z and the winding differ
            var triangles = EarClipping.Triangulate(loops[0], loops.Skip(1).ToList());
            var front = _vertices.Count;

            foreach (var corner in corners) _vertices.Add(new VertexPositionNormalTexture(new Vector3(corner, z), Vector3.UnitZ, uv.Of(corner)));

            for (var i = 0; i < triangles.Count; i += 3)
            {
                _indices.Add(front + triangles[i + 2]);
                _indices.Add(front + triangles[i + 1]);
                _indices.Add(front + triangles[i]);
            }

            // The back cap at -z, facing -Z, in the triangulation's own order
            var back = _vertices.Count;

            foreach (var corner in corners) _vertices.Add(new VertexPositionNormalTexture(new Vector3(corner, -z), -Vector3.UnitZ, uv.Of(corner)));

            for (var i = 0; i < triangles.Count; i += 3)
            {
                _indices.Add(back + triangles[i]);
                _indices.Add(back + triangles[i + 1]);
                _indices.Add(back + triangles[i + 2]);
            }
        }

        /// <summary>
        /// One flat-shaded quad per edge of a loop between +<paramref name="z"/> and -<paramref name="z"/>, with
        /// its own four vertices so the edges stay crisp. The loop is counter-clockwise for an outside and
        /// clockwise for a hole, so the same rule, rotate the edge to the right, points the wall away from the
        /// material either way. u runs 0..1 along the loop and v from the front to the back.
        /// </summary>
        private void AddWalls(Vector2[] loop, float z)
        {
            var total = 0f;

            for (var i = 0; i < loop.Length; i++) total += (loop[(i + 1) % loop.Length] - loop[i]).Length();

            if (total <= 0f) return;

            var along = 0f;

            for (var i = 0; i < loop.Length; i++)
            {
                var from = loop[i];
                var to = loop[(i + 1) % loop.Length];
                var edge = to - from;
                var length = edge.Length();

                if (length < 1e-7f) continue;

                var normal = new Vector3(edge.Y / length, -edge.X / length, 0f);

                var u0 = along / total;
                var u1 = (along + length) / total;

                AddQuad(
                    new Corner(new Vector3(from, z), normal, new Vector2(u0, 0f)),
                    new Corner(new Vector3(to, z), normal, new Vector2(u1, 0f)),
                    new Corner(new Vector3(to, -z), normal, new Vector2(u1, 1f)),
                    new Corner(new Vector3(from, -z), normal, new Vector2(u0, 1f)));

                along += length;
            }
        }

        /// <summary>
        /// The caps pulled in by the bevel, a ring of quads per step between the cap and the wall on each
        /// side, and the walls between the two bevels. The profile is a quarter ellipse: leaving the wall
        /// the surface still runs along the depth, meeting the cap it runs across it. A bevel too wide for
        /// the polygon folds the pulled-in cap over itself, which <see cref="LoopInset.Fits"/> catches; the size is
        /// halved for the polygon, up to six times, before the bevel is given up as an error.
        /// </summary>
        private void AddBevelled(List<Vector2[]> loops, TextBevel bevel, CapUv uv)
        {
            // The bevel cannot go deeper than the middle, where the front and the back bevels would meet
            var thickness = MathF.Min(bevel.Thickness, _halfDepth);
            var size = bevel.Size;
            var steps = bevel.Segments;

            for (var attempt = 0; ; attempt++)
            {
                // ring[k] is every loop pulled in by the k-th step; ring[0] is the loop itself, at the wall
                var rings = new Vector2[steps + 1][][];
                var angles = new float[steps + 1];

                for (var k = 0; k <= steps; k++)
                {
                    angles[k] = k / (float)steps * MathUtil.PiOverTwo;

                    var inset = size * (1f - MathF.Cos(angles[k]));

                    rings[k] = new Vector2[loops.Count][];

                    for (var l = 0; l < loops.Count; l++) rings[k][l] = inset == 0f ? loops[l] : LoopInset.Inset(loops[l], inset);
                }

                // The caps go first so a fold can be undone by cutting the lists back to where they were
                var innermost = rings[steps].ToList();
                var capsAt = _vertices.Count;
                var indicesAt = _indices.Count;

                try
                {
                    if (!LoopInset.Fits(loops, innermost)) throw new InvalidOperationException("The pulled-in cap folds over itself.");

                    AddCaps(innermost, _halfDepth, uv);
                }
                catch (Exception e) when (e is InvalidOperationException or ArgumentException)
                {
                    _vertices.RemoveRange(capsAt, _vertices.Count - capsAt);
                    _indices.RemoveRange(indicesAt, _indices.Count - indicesAt);

                    if (attempt == 6) throw new InvalidOperationException("The bevel is wider than the glyph's thinnest part, so the cap folds over itself; lower the bevel size.", e);

                    size *= 0.5f;

                    continue;
                }

                // The caps fit: the rings of quads from the wall to each cap, then the walls between the two bevels
                for (var l = 0; l < loops.Count; l++)
                {
                    for (var k = 0; k < steps; k++)
                    {
                        AddBevelRing(rings[k][l], rings[k + 1][l], thickness, angles[k], angles[k + 1], uv, front: true);
                        AddBevelRing(rings[k][l], rings[k + 1][l], thickness, angles[k], angles[k + 1], uv, front: false);
                    }

                    // A bevel as deep as half the depth leaves no wall between the two
                    if (_halfDepth - thickness > 1e-6f) AddWalls(loops[l], _halfDepth - thickness);
                }

                return;
            }
        }

        /// <summary>
        /// The quads between two steps of a bevel on one loop: <paramref name="outer"/> is the step nearer the
        /// wall, <paramref name="inner"/> the one nearer the cap. Flat around the loop like the walls, and
        /// rounded across the steps: a vertex's normal leans from the edge's outward direction towards the
        /// cap by its step's angle.
        /// </summary>
        private void AddBevelRing(Vector2[] outer, Vector2[] inner, float thickness, float angleOuter, float angleInner, CapUv uv, bool front)
        {
            var sign = front ? 1f : -1f;
            var zOuter = sign * (_halfDepth - thickness + thickness * MathF.Sin(angleOuter));
            var zInner = sign * (_halfDepth - thickness + thickness * MathF.Sin(angleInner));
            var count = outer.Length;

            for (var i = 0; i < count; i++)
            {
                var next = (i + 1) % count;
                var edge = outer[next] - outer[i];
                var length = edge.Length();

                if (length < 1e-7f && (inner[next] - inner[i]).Length() < 1e-7f) continue;
                if (length < 1e-7f) edge = inner[next] - inner[i];

                edge.Normalize();

                var outward = new Vector3(edge.Y, -edge.X, 0f);
                var normalOuter = outward * MathF.Cos(angleOuter) + new Vector3(0f, 0f, sign * MathF.Sin(angleOuter));
                var normalInner = outward * MathF.Cos(angleInner) + new Vector3(0f, 0f, sign * MathF.Sin(angleInner));

                // The higher-z pair first and the loop's direction left to right, which is clockwise from outside
                // on the front; on the back the outer step is the higher one
                var innerFrom = new Corner(new Vector3(inner[i], zInner), normalInner, uv.Of(inner[i]));
                var innerTo = new Corner(new Vector3(inner[next], zInner), normalInner, uv.Of(inner[next]));
                var outerFrom = new Corner(new Vector3(outer[i], zOuter), normalOuter, uv.Of(outer[i]));
                var outerTo = new Corner(new Vector3(outer[next], zOuter), normalOuter, uv.Of(outer[next]));

                if (front)
                {
                    AddQuad(innerFrom, innerTo, outerTo, outerFrom);
                }
                else
                {
                    AddQuad(outerFrom, outerTo, innerTo, innerFrom);
                }
            }
        }

        /// <summary>Four vertices of their own and two triangles: a, b, c, d clockwise as seen from outside.</summary>
        private void AddQuad(Corner a, Corner b, Corner c, Corner d)
        {
            var start = _vertices.Count;

            _vertices.Add(new VertexPositionNormalTexture(a.Position, a.Normal, a.Uv));
            _vertices.Add(new VertexPositionNormalTexture(b.Position, b.Normal, b.Uv));
            _vertices.Add(new VertexPositionNormalTexture(c.Position, c.Normal, c.Uv));
            _vertices.Add(new VertexPositionNormalTexture(d.Position, d.Normal, d.Uv));

            _indices.Add(start);
            _indices.Add(start + 1);
            _indices.Add(start + 2);
            _indices.Add(start);
            _indices.Add(start + 2);
            _indices.Add(start + 3);
        }

        /// <summary>One corner of a quad: where it is, which way its surface faces, and where it reads a texture.</summary>
        private readonly record struct Corner(Vector3 Position, Vector3 Normal, Vector2 Uv);

        /// <summary>
        /// Texture coordinates for a cap or bevel vertex from its world position: back to EM units, into the
        /// glyph's place in the block, then 0..1 across the block, v downwards.
        /// </summary>
        private readonly struct CapUv
        {
            private readonly Vector2 _uvOrigin;
            private readonly Vector2 _origin;
            private readonly float _scale;
            private readonly Vector2 _min;
            private readonly Vector2 _size;

            internal CapUv(Vector2 uvOrigin, Vector2 origin, float scale, Vector2 uvMin, Vector2 uvMax)
            {
                _uvOrigin = uvOrigin;
                _origin = origin;
                _scale = scale;
                _min = uvMin;
                _size = uvMax - uvMin;

                if (_size.X <= 0f) _size.X = 1f;
                if (_size.Y <= 0f) _size.Y = 1f;
            }

            internal Vector2 Of(Vector2 world)
            {
                var em = world / _scale - _origin + _uvOrigin;

                return new Vector2((em.X - _min.X) / _size.X, 1f - (em.Y - _min.Y) / _size.Y);
            }
        }
    }
}
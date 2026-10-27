using Stride.Graphics;
using Stride.Rendering;
using System.Text;

namespace Stride.CommunityToolkit.Rendering.Text.Meshes;

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
/// <para>
/// All the geometry is worked out in the font's EM units and scaled on the way out, so a word a
/// centimetre across is triangulated exactly as one a metre across. Within one build each distinct
/// glyph is prepared once, however often it occurs. A glyph whose contours cross or overlap, which a
/// few fonts have, is reported by font and code point rather than built wrong.
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
    /// <returns>The mesh data; <see cref="GeometricMeshDataExtensions.ToMesh"/> uploads it with bounds. Empty when the text has nothing to draw, which a GPU upload refuses.</returns>
    /// <exception cref="ArgumentNullException">The font or the text is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An option is not a positive finite number where it has to be.</exception>
    /// <exception cref="InvalidOperationException">A glyph cannot be built: its contours cross or overlap, or the bevel folds it; the message names the font and the code point.</exception>
    public static GeometricMeshData<VertexPositionNormalTexture> Create(MeshFont font, string text, TextMeshOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(text);

        options = Validate(options);

        var layout = Layout.Build(font, text, options);
        var prepared = new Dictionary<int, PreparedGlyph>();
        var emitter = new Emitter(options);

        foreach (var placed in layout.Glyphs)
        {
            emitter.Add(Prepare(font, placed.Glyph, options, prepared), placed.Pen, placed.Pen + layout.Shift, layout.Block.Min, layout.Block.Max);
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
    /// <returns>A draw for a <see cref="Mesh"/>; the caller owns its buffers, see <see cref="GeometricMeshDataExtensions.ToMeshDraw"/>. <see cref="CreateMesh"/> adds the bounds.</returns>
    /// <exception cref="ArgumentNullException">The device, the font or the text is null.</exception>
    /// <exception cref="ArgumentException">The text has nothing to draw (empty, spaces, line breaks): a GPU buffer cannot be empty, so remove the mesh instead.</exception>
    public static MeshDraw CreateMeshDraw(GraphicsDevice device, MeshFont font, string text, TextMeshOptions? options = null)
        => Create(font, text, options).ToMeshDraw(device);

    /// <summary>
    /// Builds the lettering, uploads it and wraps it in a <see cref="Mesh"/> with its bounds, which the
    /// renderer needs to cull it when it is off screen.
    /// </summary>
    /// <param name="device">The graphics device the buffers are created on.</param>
    /// <param name="font">The font.</param>
    /// <param name="text">The text; <c>\n</c> starts a new line.</param>
    /// <param name="options">Size, depth, layout.</param>
    /// <param name="materialIndex">The index of the material the mesh uses in its model.</param>
    /// <returns>The mesh; the caller owns its draw's buffers.</returns>
    /// <exception cref="ArgumentNullException">The device, the font or the text is null.</exception>
    /// <exception cref="ArgumentException">The text has nothing to draw.</exception>
    public static Mesh CreateMesh(GraphicsDevice device, MeshFont font, string text, TextMeshOptions? options = null, int materialIndex = 0)
        => Create(font, text, options).ToMesh(device, materialIndex);

    /// <summary>
    /// Builds the lettering as a complete <see cref="Model"/>: one mesh with its bounds, one material.
    /// </summary>
    /// <param name="device">The graphics device the buffers are created on.</param>
    /// <param name="material">The material the lettering is drawn with.</param>
    /// <param name="font">The font.</param>
    /// <param name="text">The text; <c>\n</c> starts a new line.</param>
    /// <param name="options">Size, depth, layout.</param>
    /// <returns>The model, ready for a <c>ModelComponent</c>; the caller owns the mesh's buffers.</returns>
    /// <exception cref="ArgumentNullException">The device, the material, the font or the text is null.</exception>
    /// <exception cref="ArgumentException">The text has nothing to draw.</exception>
    public static Model CreateModel(GraphicsDevice device, Material material, MeshFont font, string text, TextMeshOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(material);

        var mesh = CreateMesh(device, font, text, options);

        var model = new Model { new MaterialInstance { Material = material }, mesh };

        model.BoundingBox = mesh.BoundingBox;
        model.BoundingSphere = mesh.BoundingSphere;

        return model;
    }

    /// <summary>
    /// Builds the lettering one glyph at a time, each centred on itself and told where it belongs: for a
    /// body per letter, or letters that move on their own.
    /// </summary>
    /// <param name="font">The font.</param>
    /// <param name="text">The text; <c>\n</c> starts a new line.</param>
    /// <param name="options">Size, depth, layout; the anchor applies to the whole block, as in <see cref="Create"/>.</param>
    /// <returns>One mesh per glyph that draws something, in text order; spaces are left out.</returns>
    /// <exception cref="ArgumentNullException">The font or the text is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An option is not a positive finite number where it has to be.</exception>
    /// <exception cref="InvalidOperationException">A glyph cannot be built; the message names the font and the code point.</exception>
    public static IReadOnlyList<GlyphMesh> CreateGlyphs(MeshFont font, string text, TextMeshOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(text);

        options = Validate(options);

        var layout = Layout.Build(font, text, options);
        var prepared = new Dictionary<int, PreparedGlyph>();
        var glyphs = new List<GlyphMesh>(layout.Glyphs.Count);

        foreach (var placed in layout.Glyphs)
        {
            var glyph = Prepare(font, placed.Glyph, options, prepared);

            if (glyph.IsEmpty) continue;

            var origin = placed.Pen + layout.Shift;
            var centre = origin + (placed.Glyph.BoundsMin + placed.Glyph.BoundsMax) * 0.5f;
            var emitter = new Emitter(options);

            emitter.Add(glyph, Vector2.Zero, origin - centre, placed.Glyph.BoundsMin, placed.Glyph.BoundsMax);

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
    /// <exception cref="ArgumentOutOfRangeException">An option is not a positive finite number where it has to be.</exception>
    public static BoundingBox Measure(MeshFont font, string text, TextMeshOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(text);

        options = Validate(options);

        var layout = Layout.Build(font, text, options);
        var min = (layout.Block.Min + layout.Shift) * options.Size;
        var max = (layout.Block.Max + layout.Shift) * options.Size;

        return new BoundingBox(new Vector3(min, -options.Depth * 0.5f), new Vector3(max, options.Depth * 0.5f));
    }

    /// <summary>The options checked once, the same way for every entry point; the defaults when none were given.</summary>
    private static TextMeshOptions Validate(TextMeshOptions? options)
    {
        options ??= new TextMeshOptions();

        Positive(options.Size, nameof(options.Size));
        Positive(options.Depth, nameof(options.Depth));
        Positive(options.LineSpacing, nameof(options.LineSpacing));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.CurveQuality, nameof(options.CurveQuality));

        if (!float.IsFinite(options.LetterSpacing))
            throw new ArgumentOutOfRangeException(nameof(options.LetterSpacing), options.LetterSpacing, "The letter spacing must be a finite number.");

        return options;

        static void Positive(float value, string name)
        {
            if (!float.IsFinite(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(name, value, $"{name} must be a positive finite number.");
        }
    }

    /// <summary>
    /// The glyph's geometry in EM units, once per distinct glyph per build: its polygons flattened and
    /// checked, their caps triangulated, and the bevel's rings with a width the glyph can take.
    /// </summary>
    private static PreparedGlyph Prepare(MeshFont font, GlyphOutline glyph, TextMeshOptions options, Dictionary<int, PreparedGlyph> prepared)
    {
        if (prepared.TryGetValue(glyph.Codepoint, out var existing)) return existing;

        try
        {
            var result = PreparedGlyph.Build(glyph, options);

            prepared[glyph.Codepoint] = result;

            return result;
        }
        catch (Exception e) when (e is InvalidOperationException or InvalidDataException or ArgumentException)
        {
            throw new InvalidOperationException($"'{font.Name}' U+{glyph.Codepoint:X4} ('{char.ConvertFromUtf32(glyph.Codepoint)}') cannot be built: {e.Message}", e);
        }
    }

    /// <summary>A glyph and where its pen sits, in EM units before the anchor shift.</summary>
    private readonly record struct PlacedGlyph(GlyphOutline Glyph, Vector2 Pen);

    /// <summary>A box in EM units.</summary>
    private readonly record struct Block(Vector2 Min, Vector2 Max);

    /// <summary>Where every glyph goes: the pen positions, the block they make, and the shift the anchor asks for.</summary>
    private sealed record Layout(List<PlacedGlyph> Glyphs, Block Block, Vector2 Shift)
    {
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

        private static Vector2 ShiftFor(Anchor? anchor, Block block)
        {
            if (anchor is null) return Vector2.Zero;

            var x = anchor switch
            {
                Anchor.TopLeft or Anchor.MiddleLeft or Anchor.BottomLeft => block.Min.X,
                Anchor.TopRight or Anchor.MiddleRight or Anchor.BottomRight => block.Max.X,
                _ => (block.Min.X + block.Max.X) * 0.5f,
            };

            var y = anchor switch
            {
                Anchor.TopLeft or Anchor.TopCenter or Anchor.TopRight => block.Max.Y,
                Anchor.BottomLeft or Anchor.BottomCenter or Anchor.BottomRight => block.Min.Y,
                _ => (block.Min.Y + block.Max.Y) * 0.5f,
            };

            return new Vector2(-x, -y);
        }
    }

    /// <summary>
    /// One polygon of a glyph ready to extrude, in EM units: its loops (the outside counter-clockwise,
    /// then the holes clockwise), the triangles of its cap, and with a bevel the rings each loop steps
    /// through from the wall to the pulled-in cap, whose triangles are <see cref="CapTriangles"/>.
    /// </summary>
    /// <param name="Loops">The loops at the wall.</param>
    /// <param name="Rings">With a bevel, ring k for every loop, ring 0 being <paramref name="Loops"/>; without, one ring.</param>
    /// <param name="CapTriangles">The cap's triangles, indices into the innermost ring's corners, outside first then the holes.</param>
    private sealed record PreparedPolygon(Vector2[][] Loops, Vector2[][][] Rings, List<int> CapTriangles);

    /// <summary>A glyph's polygons prepared, and the bevel width in EM units they accepted.</summary>
    /// <param name="Polygons">The polygons, each with its loops, rings and cap triangles.</param>
    /// <param name="BevelSizeEm">The bevel's reach across the cap in EM units, after any shrinking the glyph needed; 0 without a bevel.</param>
    private sealed record PreparedGlyph(List<PreparedPolygon> Polygons, float BevelSizeEm)
    {
        internal bool IsEmpty => Polygons.Count == 0;

        internal static PreparedGlyph Build(GlyphOutline glyph, TextMeshOptions options)
        {
            var shape = glyph.Flatten(options.CurveQuality);
            var loops = new List<Vector2[][]>(shape.Polygons.Count);

            foreach (var polygon in shape.Polygons)
            {
                var set = new Vector2[1 + polygon.Holes.Count][];

                set[0] = [.. polygon.Outer];

                for (var h = 0; h < polygon.Holes.Count; h++) set[h + 1] = [.. polygon.Holes[h]];

                loops.Add(set);
            }

            if (options.Bevel is null)
            {
                var polygons = new List<PreparedPolygon>(loops.Count);

                foreach (var set in loops) polygons.Add(new PreparedPolygon(set, [set], Triangulate(set)));

                return new PreparedGlyph(polygons, 0f);
            }

            // The bevel's reach in EM units, halved for the whole glyph until every polygon takes it: a
            // dot and its stem get the same rounding, or the glyph is given up
            var sizeEm = options.Bevel.Size / options.Size;
            var steps = options.Bevel.Segments;

            for (var attempt = 0; ; attempt++)
            {
                var polygons = new List<PreparedPolygon>(loops.Count);
                var fits = true;

                foreach (var set in loops)
                {
                    var rings = new Vector2[steps + 1][][];

                    rings[0] = set;

                    for (var k = 1; k <= steps && fits; k++)
                    {
                        var inset = sizeEm * (1f - MathF.Cos(k / (float)steps * MathUtil.PiOverTwo));

                        rings[k] = new Vector2[set.Length][];

                        for (var l = 0; l < set.Length; l++) rings[k][l] = LoopInset.Inset(set[l], inset);

                        // Every step is checked against the one before, not only the last against the
                        // wall: a fold between two steps would pass a check of the ends alone
                        fits = LoopInset.Fits(rings[k - 1], rings[k]);
                    }

                    if (!fits) break;

                    polygons.Add(new PreparedPolygon(set, rings, Triangulate(rings[steps])));
                }

                if (fits) return new PreparedGlyph(polygons, sizeEm);

                if (attempt == 6) throw new InvalidOperationException("the bevel is wider than the glyph's thinnest part, so the cap would fold over itself; lower the bevel size");

                sizeEm *= 0.5f;
            }
        }

        private static List<int> Triangulate(Vector2[][] loops)
            => EarClipping.Triangulate(loops[0], loops.Skip(1).ToList());
    }

    /// <summary>Accumulates the caps, bevels and walls of prepared polygons into one vertex and index list, scaled and placed on the way in.</summary>
    private sealed class Emitter(TextMeshOptions options)
    {
        private readonly List<VertexPositionNormalTexture> _vertices = [];
        private readonly List<int> _indices = [];
        private readonly float _scale = options.Size;
        private readonly float _halfDepth = options.Depth * 0.5f;
        private readonly TextBevel? _bevel = options.Bevel;

        /// <summary>
        /// Adds a prepared glyph placed at <paramref name="origin"/>, with cap texture coordinates spread over
        /// the box from <paramref name="uvMin"/> to <paramref name="uvMax"/>, in which the glyph sits at
        /// <paramref name="uvOrigin"/>. Everything in EM units; the scale is applied here.
        /// </summary>
        internal void Add(PreparedGlyph glyph, Vector2 uvOrigin, Vector2 origin, Vector2 uvMin, Vector2 uvMax)
        {
            var placement = new Placement(origin, _scale, uvOrigin, uvMin, uvMax);

            foreach (var polygon in glyph.Polygons)
            {
                if (_bevel is null)
                {
                    AddCaps(polygon.Rings[0], polygon.CapTriangles, _halfDepth, placement);

                    foreach (var loop in polygon.Loops) AddWalls(loop, _halfDepth, placement);

                    continue;
                }

                // The bevel cannot go deeper than the middle, where the front and the back bevels meet. One
                // within a ten-thousandth of it is taken as reaching it: a wall thinner than that is left
                // out below, and the rings must then end exactly where the two sides meet, not a hair short
                var thickness = MathF.Min(_bevel.Thickness, _halfDepth);

                if (_halfDepth - thickness <= 1e-4f * _halfDepth) thickness = _halfDepth;

                var profile = new BevelProfile(glyph.BevelSizeEm * _scale, thickness, _bevel.Segments, _halfDepth);

                AddCaps(polygon.Rings[profile.Steps], polygon.CapTriangles, _halfDepth, placement);

                for (var l = 0; l < polygon.Loops.Length; l++)
                {
                    for (var k = 0; k < profile.Steps; k++)
                    {
                        AddBevelRing(polygon.Rings[k][l], polygon.Rings[k + 1][l], profile, k, placement, front: true);
                        AddBevelRing(polygon.Rings[k][l], polygon.Rings[k + 1][l], profile, k, placement, front: false);
                    }

                    // A bevel as deep as half the depth leaves no wall between the two
                    if (profile.Thickness < _halfDepth) AddWalls(polygon.Loops[l], _halfDepth - profile.Thickness, placement);
                }
            }
        }

        internal GeometricMeshData<VertexPositionNormalTexture> ToMeshData(string name)
            => new([.. _vertices], [.. _indices], isLeftHanded: false) { Name = name };

        /// <summary>
        /// The front cap at +<paramref name="z"/> and the back cap at -<paramref name="z"/> from the same
        /// corners and triangles: the front reverses the counter-clockwise triangulation so it winds
        /// clockwise for the viewer at +Z, the back keeps it, where the same order already reads clockwise.
        /// </summary>
        private void AddCaps(Vector2[][] loops, List<int> triangles, float z, Placement placement)
        {
            // The triangles index the corners of every loop in order: the outside, then each hole
            var corners = new List<Vector2>(loops.Sum(l => l.Length));

            foreach (var loop in loops) corners.AddRange(loop);

            // Front cap: one vertex per corner at +z, then the triangles reversed
            var front = _vertices.Count;

            foreach (var corner in corners) _vertices.Add(new VertexPositionNormalTexture(placement.World(corner, z), Vector3.UnitZ, placement.Uv(corner)));

            for (var i = 0; i < triangles.Count; i += 3)
            {
                _indices.Add(front + triangles[i + 2]);
                _indices.Add(front + triangles[i + 1]);
                _indices.Add(front + triangles[i]);
            }

            // Back cap: the same corners at -z, facing -Z, in the triangulation's own order
            var back = _vertices.Count;

            foreach (var corner in corners) _vertices.Add(new VertexPositionNormalTexture(placement.World(corner, -z), -Vector3.UnitZ, placement.Uv(corner)));

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
        private void AddWalls(Vector2[] loop, float z, Placement placement)
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
                    new Corner(placement.World(from, z), normal, new Vector2(u0, 0f)),
                    new Corner(placement.World(to, z), normal, new Vector2(u1, 0f)),
                    new Corner(placement.World(to, -z), normal, new Vector2(u1, 1f)),
                    new Corner(placement.World(from, -z), normal, new Vector2(u0, 1f)));

                along += length;
            }
        }

        /// <summary>
        /// The quads between step <paramref name="step"/> and the next of a bevel on one loop:
        /// <paramref name="outer"/> is the step nearer the wall, <paramref name="inner"/> the one nearer the
        /// cap. Flat around the loop like the walls, and rounded across the steps: a vertex's normal is the
        /// profile's, which is an ellipse, so it leans by the ellipse and not by a circle.
        /// </summary>
        private void AddBevelRing(Vector2[] outer, Vector2[] inner, BevelProfile profile, int step, Placement placement, bool front)
        {
            var sign = front ? 1f : -1f;
            var zOuter = sign * profile.Z(step);
            var zInner = sign * profile.Z(step + 1);
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
                var normalOuter = profile.Normal(outward, step, sign);
                var normalInner = profile.Normal(outward, step + 1, sign);

                // The higher-z pair first and the loop's direction left to right, which is clockwise from outside
                // on the front; on the back the outer step is the higher one
                var innerFrom = new Corner(placement.World(inner[i], zInner), normalInner, placement.Uv(inner[i]));
                var innerTo = new Corner(placement.World(inner[next], zInner), normalInner, placement.Uv(inner[next]));
                var outerFrom = new Corner(placement.World(outer[i], zOuter), normalOuter, placement.Uv(outer[i]));
                var outerTo = new Corner(placement.World(outer[next], zOuter), normalOuter, placement.Uv(outer[next]));

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
        /// The bevel's profile in world units: a quarter ellipse <see cref="Size"/> across the cap and
        /// <see cref="Thickness"/> through the depth, in <see cref="Steps"/> steps from the wall to the cap.
        /// </summary>
        private readonly record struct BevelProfile(float Size, float Thickness, int Steps, float HalfDepth)
        {
            /// <summary>The profile's angle at a step: 0 at the wall, a quarter turn at the cap.</summary>
            private float Angle(int step) => step / (float)Steps * MathUtil.PiOverTwo;

            /// <summary>The depth of a step on the front; negate for the back.</summary>
            internal float Z(int step) => HalfDepth - Thickness + Thickness * MathF.Sin(Angle(step));

            /// <summary>
            /// The normal at a step: the profile runs (size (1 - cos a), thickness sin a) in (inward, depth),
            /// its tangent is (size sin a, thickness cos a), and the normal perpendicular to that, pointing out
            /// of the material, is (thickness cos a, size sin a) normalised; <paramref name="sign"/> is the side.
            /// </summary>
            internal Vector3 Normal(Vector3 outward, int step, float sign)
            {
                var angle = Angle(step);
                var normal = outward * (Thickness * MathF.Cos(angle)) + new Vector3(0f, 0f, sign * Size * MathF.Sin(angle));

                normal.Normalize();

                return normal;
            }
        }

        /// <summary>
        /// Where a glyph sits: its origin in EM units and the scale to world units, and the texture box of
        /// the block it belongs to, in which it sits at the texture origin.
        /// </summary>
        private readonly struct Placement
        {
            private readonly Vector2 _origin;
            private readonly float _scale;
            private readonly Vector2 _uvOrigin;
            private readonly Vector2 _min;
            private readonly Vector2 _size;

            internal Placement(Vector2 origin, float scale, Vector2 uvOrigin, Vector2 uvMin, Vector2 uvMax)
            {
                _origin = origin;
                _scale = scale;
                _uvOrigin = uvOrigin;
                _min = uvMin;
                _size = uvMax - uvMin;

                if (_size.X <= 0f) _size.X = 1f;
                if (_size.Y <= 0f) _size.Y = 1f;
            }

            /// <summary>A corner in EM units placed and scaled into world units, at a depth.</summary>
            internal Vector3 World(Vector2 em, float z) => new((em + _origin) * _scale, z);

            /// <summary>Texture coordinates for a corner: into the glyph's place in the block, then 0..1 across it, v downwards.</summary>
            internal Vector2 Uv(Vector2 em)
            {
                var inBlock = em + _uvOrigin;

                return new Vector2((inBlock.X - _min.X) / _size.X, 1f - (inBlock.Y - _min.Y) / _size.Y);
            }
        }
    }
}
# 3D Text

`TextMesh` builds solid lettering from a real font: each glyph's outline is read from the font file,
triangulated, holes and all, and extruded along Z into a mesh with a front cap, a back cap and
flat-shaded walls. The result is geometry: it is lit and shadowed, takes any material, casts a
shadow, and can carry a physics body. For ordinary text, labels and HUDs, [Entity Text](entity-text.md)
and [World Text](world-text.md) draw a font far more cheaply; this is for lettering that is meant to be a
thing in the world, a title, a sign, a logo, a word that falls.

`MeshFont`, `TextMesh`, `TextMeshOptions` and `GlyphOutline` live in the
`Stride.CommunityToolkit.Rendering.Text` namespace; `Primitive3D.Text` in
`Stride.CommunityToolkit.Rendering.ProceduralModels`. The stages between the font file and
the mesh, and the files behind each, are on [How 3D text is built](text-3d-pipeline.md).

```csharp
var entity = new Entity("Title")
{
    new ModelComponent { Model = TextMesh.CreateModel(game.GraphicsDevice, game.CreateMaterial(Color.Gold), MeshFont.Default, "Hello") },
};
```

## Fonts

A `MeshFont` is a font file opened for its outlines, through the FreeType library the engine ships on
every platform, so TrueType, OpenType (CFF) and font collections all work.

- `MeshFont.Default` is an Open Sans Regular embedded in the toolkit (Apache 2.0): lettering works with
  no font file at hand, and gives the same shapes on every machine.
- `MeshFont.Load(path)` and `MeshFont.Load(bytes)` open any file; a collection (.ttc) holds several
  faces, `FaceCount` says how many and `Load(path, faceIndex)` picks one. Dispose the font when its
  outlines are no longer needed; glyphs are read once and cached, a font is safe to share between
  threads, and disposing it while another thread reads makes that reader throw rather than crash.
- `MeshFont.FromFamily("Segoe UI", FontStyle.Bold)` finds an installed family through `SystemFonts`,
  and is `null` when the machine has no such font, so a fallback reads `?? MeshFont.Default`. It opens
  the file's first face; a style kept as a later face of a collection needs `Load` with the index.

Outlines are in EM units: a capital letter is about 0.7 high, the baseline is y = 0, and
`MeshFont.LineHeight`, `Ascent` and `Descent` are the font's own. `font.GetGlyph('A')` returns the
`GlyphOutline`: contours as curves, the advance, the bounds; `Flatten()` turns them into polygons
with their holes sorted out, which is also what a ShapeBatch outline of a letter would draw.

Kerning comes from the font's legacy kern table only. Modern fonts keep it in GPOS, which FreeType
does not read without a shaping engine, so `GetKerning` is 0 for most fonts, including the default.
`TextMeshOptions.LetterSpacing` tunes the fit by hand.

## Options

| Option | Default | What it does |
|---|---|---|
| `Size` | 1 | World units per EM. A capital comes out about 0.7 of this tall. |
| `Depth` | 0.25 | How far the lettering extends along Z, centred on zero. |
| `Bevel` | `null` | A `TextBevel(size, thickness, segments)` rounds the edge between the caps and the walls, front and back; see below. |
| `CurveQuality` | 32 | Points a full turn of curve gets: a quarter-turn bowl gets a quarter of them. 16 is still smooth at a distance; 64 for a logo the camera gets close to. |
| `Anchor` | `null` | Which point of the text block sits at the origin. `null` is the typographic origin: the first baseline at y = 0, the pen at x = 0. The nine `TextAnchor` points use the font's ascent-to-descent block over every line, so the anchor does not move with the letters used. |
| `LetterSpacing` | 0 | Extra space after every glyph, in EM units; negative tightens. |
| `LineSpacing` | 1 | The distance between lines as a multiple of the font's line height. |
| `Alignment` | `Left` | How lines of different widths line up: `Left`, `Center`, `Right`. |
| `Kerning` | `true` | Apply the font's pair kerning where the font has a kern table. |

`\n` starts a new line. `TextMesh.Measure(font, text, options)` gives the block as a `BoundingBox`
without building the mesh.

```csharp
var options = new TextMeshOptions
{
    Size = 0.4f,
    Depth = 0.1f,
    Alignment = TextAlignment.Center,
    LineSpacing = 0.9f,
    Anchor = TextAnchor.TopCenter,
};

var draw = TextMesh.CreateMeshDraw(game.GraphicsDevice, MeshFont.Default, "two lines\nof text", options);
```

## Bevel

`TextBevel` rounds the edge between the caps and the walls on both sides: a quarter-ellipse fillet
that reaches `Size` in from the outline across the cap and `Thickness` from the cap towards the
middle, in `Segments` steps (1 is a chamfer, 3 reads as round, 6 is smooth up close). The walls stay
flat and the bevel is flat around the outline too, so the edges stay crisp; across the steps the
normals turn from the wall to the cap, which is what catches the light on a title.

```csharp
var options = new TextMeshOptions
{
    Size = 0.6f,
    Depth = 0.2f,
    Bevel = new TextBevel(size: 0.015f, thickness: 0.03f, segments: 4),
};
```

A bevel wider than a glyph's thinnest stem would fold the cap over itself. `TextMesh` checks every
step of the pulled-in outline (same way round, smaller, every edge still running its way, not
crossing itself or another) and halves the size for the whole glyph until every piece of it fits, so
a wide bevel on a thin font rounds less rather than breaking; the dot and the stem of an i get the
same rounding. The thickness is capped at half the depth; at that value the two bevels meet and there
is no wall.

## The mesh

`TextMesh.Create` returns `GeometricMeshData<VertexPositionNormalTexture>`, CPU data. `ToMesh(device)`
uploads it as a `Mesh` with its bounds, which the renderer needs to cull lettering that is off screen,
and `TextMesh.CreateMesh` and `CreateModel` do both in one call; `ToMeshDraw` and `CreateMeshDraw`
give the bare draw. Text with nothing to draw (empty, spaces, line breaks) gives empty CPU data and
refuses to upload, since a GPU buffer cannot be empty: when a value clears, remove the model.
Vertices carry a
position, a normal and texture coordinates: the caps are mapped across the text's block (0..1 left to
right, 0..1 top to bottom) and each wall runs 0..1 along its contour and 0..1 front to back, so a
texture or a shader can follow the lettering.

The caller owns the buffers of a draw. Lettering built once costs nothing after that; text that
changes must dispose the old buffers before the new mesh goes in, or it leaks a buffer pair per
rebuild. `E03_3D_MeshText` shows the pattern in its frame counter.

## A body per word, or per letter

`Primitive3D.Text` makes lettering a shape like any other, so `game.CreatePrimitive` gives it a material
and, with a physics engine chosen, a collider: one convex hull per glyph. A hull fills a glyph's holes
and bays, so an O collides as a disc, which is right for letters that tumble and pile up. A `Mass` on
the options is the word's, shared out among the hulls. The hulls of a string are kept, keyed by the
font, the text and the options, so a word dropped many times shares one set.

```csharp
var word = game.CreatePrimitive(Primitive3D.Text("Drop", size: 0.7f, depth: 0.3f), new Bepu3DPhysicsOptions { Material = material });
word.Transform.Position = new Vector3(0, 4, 0);
word.Scene = scene;
```

`TextMesh.CreateGlyphs` returns one mesh per glyph, each centred on itself and told where it sits in
the text, for a body or an animation per letter; `E20_3D_CubeCollapse` drops its GAME OVER that way.
Lettering that must be walked into or shot through its holes wants a `MeshCollider` on a static, see
[Physics extensions](../physics-extensions/index.md).

## Limits

- A mesh is for words and titles, not pages: a paragraph is thousands of triangles per line. Sprite
  fonts are for text.
- Two filled contours that cross or overlap (composite glyphs such as Ç, where the cedilla runs
  into the C) are built as two solids through each other, which the font's nonzero fill would merge;
  they look right from outside, and each piece is closed on its own. A contour that crosses itself
  is reported by font and code point, not mangled. A contour of a single point, which some fonts
  carry, is skipped.
- Every option must be a positive finite number where it is a size; a zero or negative size or depth
  is refused rather than built.
- Letters landing on letters are hull-against-hull pairs, and the Bepu build Stride 4.4 ships
  mishandles thin extruded hulls that overlap (reported upstream as bepuphysics2 issue 417; it can
  crash the narrow phase). A hull collider suits a word on its own; give a pile of letters a box per
  glyph instead, from the glyph meshes' bounds, as Cube Collapse does.
- No right-to-left, no wrapping, no text on a path.

## Examples

- [`E03_3D_MeshText`](../code-only/examples/letters-3d.md): the embedded font, an installed font, lines
  that align, a frame counter, and words with physics.
- [`E03_3D_TextShaders`](../code-only/examples/text-shaders.md): four materials on the same lettering,
  a gradient through the texture coordinates, a rim light node, a vertex-stage wave and a pixel-stage
  dissolve, each a shader class in the example's `Effects` folder.
- [`E07_3D_ProceduralGeometry`](../code-only/examples/procedural-geometry.md): lettering among other
  meshes built in code.
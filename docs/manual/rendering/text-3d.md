# 3D Text

`TextMesh` builds solid lettering from a real font: each glyph's outline is read from the font file,
triangulated, holes and all, and extruded along Z into a mesh with a front cap, a back cap and
flat-shaded walls. The result is geometry: it is lit and shadowed, takes any material, casts a
shadow, and can carry a physics body. For ordinary text, labels and HUDs, [Entity Text](entity-text.md)
and [World Text](world-text.md) draw a font far more cheaply; this is for lettering that is meant to be a
thing in the world, a title, a sign, a logo, a word that falls.

`MeshFont`, `TextMesh`, `TextMeshOptions` and `GlyphOutline` live in the
`Stride.CommunityToolkit.Rendering.Text` namespace; `Primitive3D.Text` in
`Stride.CommunityToolkit.Rendering.ProceduralModels`.

```csharp
var entity = new Entity("Title")
{
    new ModelComponent
    {
        Model = new Model
        {
            new MaterialInstance { Material = game.CreateMaterial(Color.Gold) },
            new Mesh { Draw = TextMesh.CreateMeshDraw(game.GraphicsDevice, MeshFont.Default, "Hello"), MaterialIndex = 0 },
        },
    },
};
```

## Fonts

A `MeshFont` is a font file opened for its outlines, through the FreeType library the engine ships on
every platform, so TrueType, OpenType (CFF) and font collections all work.

- `MeshFont.Default` is an Open Sans Regular embedded in the toolkit (Apache 2.0): lettering works with
  no font file at hand, and gives the same shapes on every machine.
- `MeshFont.Load(path)` and `MeshFont.Load(bytes)` open any file. Dispose the font when its outlines are
  no longer needed; glyphs are read once and cached, and a font is safe to share between threads.
- `MeshFont.FromFamily("Segoe UI", FontStyle.Bold)` finds an installed family through `SystemFonts`,
  and is `null` when the machine has no such font, so a fallback reads `?? MeshFont.Default`.

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

## The mesh

`TextMesh.Create` returns `GeometricMeshData<VertexPositionNormalTexture>`, CPU data; the
`ToMeshDraw(device)` extension uploads it, and `TextMesh.CreateMeshDraw` does both. Vertices carry a
position, a normal and texture coordinates: the caps are mapped across the text's block (0..1 left to
right, 0..1 top to bottom) and each wall runs 0..1 along its contour and 0..1 front to back, so a
texture or a shader can follow the lettering.

The caller owns the buffers of a draw. Lettering built once costs nothing after that; text that
changes must dispose the old buffers before the new mesh goes in, or it leaks a buffer pair per
rebuild. `E03_3D_MeshText` shows the pattern in its frame counter.

## A body per word, or per letter

`Primitive3D.Text` makes lettering a shape like any other, so `game.CreatePrimitive` gives it a material
and, with a physics engine chosen, a collider: one convex hull per glyph. A hull fills a glyph's holes
and bays, so an O collides as a disc, which is right for letters that tumble and pile up.

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
- Fonts whose contours overlap each other (some composite glyphs, variable fonts) are not
  triangulated; the glyph is reported, not mangled.
- No bevel yet, no right-to-left, no wrapping.

## Examples

- [`E03_3D_MeshText`](../code-only/examples/letters-3d.md): the embedded font, an installed font, lines
  that align, a frame counter, and a word with physics.
- [`E07_3D_ProceduralGeometry`](../code-only/examples/procedural-geometry.md): lettering among other
  meshes built in code.
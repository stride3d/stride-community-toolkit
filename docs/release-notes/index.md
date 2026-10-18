# Release Notes

Welcome to the Release Notes for the **Stride Community Toolkit**. This section aims to provide you with an organized, high-level summary of changes, enhancements, and fixes made in each version release. If you're looking to understand what has changed from one version to the next, you're in the right place.

## What to Expect

The Stride Community Toolkit is developed with rapid iteration in mind. It moves at a faster development pace compared to the Stride Game Engine. As a result, you should expect frequent updates that may introduce breaking changes. This fast-paced approach allows us to incorporate community feedback quickly and continue improving the toolkit.

## 1.0.0.0-preview.68

## What's Changed

### 💥 Breaking Changes

#### Lettering comes from fonts

`LetterMeshFactory`, with its 38 hand-authored glyphs, is gone; `TextMesh` builds solid lettering from any font, with the toolkit's embedded Open Sans as the default.

| Before | After | Note |
|---|---|---|
| `LetterMeshFactory.CreateTextMeshDraw(device, "XYZ")` | `TextMesh.CreateMeshDraw(device, MeshFont.Default, "XYZ")` | In `Stride.CommunityToolkit.Rendering.Text`. Any character the font has, upper and lower case, digits, punctuation. |
| `CreateTextMeshDraw(..., depth: 0.3f, spacing: 0.1f, centerOrigin: true)` | `new TextMeshOptions { Depth = 0.3f, LetterSpacing = 0.1f, Anchor = TextAnchor.MiddleCenter }` | `Size` is world units per EM; the old glyphs were one unit tall, a capital is now about 0.7 of `Size`. |
| `LetterMeshFactory.SupportedCharacters`, `TryGetOutlines` | `font.HasGlyph(c)`, `font.GetGlyph(c)` | A glyph the font lacks draws its "missing glyph" box instead of throwing. |

- `EarClipping.Triangulate(outer, holes)` is new; the one-polygon overload is unchanged. `MeshBuilder.AddExtrudedPolygon` is unchanged.

### 🎉 New Features

- `MeshFont`: a font file opened for its glyph outlines through the FreeType the engine ships, so TrueType, OpenType and collections work on every platform. `MeshFont.Default` is an embedded Open Sans Regular; `Load(path)`, `Load(bytes)` and `FromFamily(family, style)` open any other. Outlines come back in EM units as curves (`GlyphOutline`, `GlyphContour`, `OutlineSegment`) and flatten into polygons with their holes sorted out (`GlyphShape`).
- `TextMesh`: solid 3D text from a `MeshFont`. `Create` gives CPU mesh data, `CreateMeshDraw` uploads it, `CreateGlyphs` gives one centred mesh per glyph for a body or an animation per letter, `Measure` gives the block. Lines, alignment, letter and line spacing, curve quality, the nine-point anchor, and texture coordinates on caps and walls, through `TextMeshOptions`.
- `Primitive3D.Text`: lettering as a shape, so `CreatePrimitive` gives it a material and a collider of one convex hull per glyph, on Bepu (`TextCollider`) and on Bullet.
- `TextBevel`: a rounded edge between the caps and the walls of 3D text, front and back, through `TextMeshOptions.Bevel`; a bevel wider than a glyph's thinnest stem shrinks for that glyph instead of folding.
- `ToMeshDraw(device)`, `ToMesh(device)` and `Bounds()` on `GeometricMeshData<VertexPositionNormalTexture>`, for any procedural mesh data; `TextMesh.CreateMesh` and `CreateModel` give lettering with its bounds, so the renderer can cull it.

### 🐞 Bug Fixes

- `E20_3D_CubeCollapse`: the tower spread apart as it spawned. Since the scene setup chose Bepu (preview.67), `CreatePrimitive` gave every cube a dynamic body of its own on top of the spawner's `SlidingCubeComponent`, and the two pushed each other apart; the cube is now created with `IncludePhysics = false`.

### ✨ Enhancement

- `SceneSetup.None` still honours what is not scene: the physics engine, the game settings, the overlay and the profiler. `SceneSetup.None.UseBepu()` is a hand-built scene whose primitives and ground get Bepu bodies, the way a tutorial that adds the camera, light and ground line by line wants it; before, the engine chosen on it was silently dropped. It is a new instance each time.
- `GrabberScript` serves a 2D scene as it is: under an orthographic camera the pick ray runs along the view and a `Body2DComponent` is carried across its plane; the mouse wheel does nothing for a 2D body or under an orthographic camera, since depth there is the plane itself. `game.AddGrabber()` is the same call in 2D and 3D. The reach (`MaxDistance`) defaults to 100 instead of 50: a 2D camera stands 50 from its bodies, and the stress pile is viewed from 80.

### 📄 Docs

- The glossary is in the top navigation, and defines gizmo, mesh collider, glyph and EM.
- Physics extensions: a Shape queries section (`Overlap`, `OverlapInfo`, `SweepCast`, `SweepCastPenetrating`, what the distance means), and a Mesh colliders section, what a `MeshCollider` on a static keeps that a hull fills in, and what a mesh is not (a surface with no inside), measured in `E05_3D_MeshColliders`.
- A manual page for 3D text (`rendering/text-3d.md`); the MeshBuilder page points at it. A second page, `rendering/text-3d-pipeline.md`, walks the pipeline from the font file to the mesh, with the files behind each stage.

### 🎓 Examples

- `E03_3D_TextShaders`: four materials on the same solid lettering, a gradient texture through the caps' texture coordinates, a rim light as a shader-class colour node, a vertex-stage wave on `Global.Time` and a pixel-stage dissolve driven per frame, each shader in the example's `Effects` folder.
- `E03_3D_MeshText` is 3D text from a font: the embedded font, an installed font found by family, lines that align in one mesh, the frame counter, and words with physics through `Primitive3D.Text`, piled with a box per glyph. `E07_3D_ProceduralGeometry` and `E20_3D_CubeCollapse` use `TextMesh` for their lettering.
- `E05_3D_MeshColliders`: a convex hull against a mesh collider on the same ring model, with a ball resting on the hull's lid and falling through the mesh's hole; a mesh sphere with a ball outside and one inside shows that a mesh has no inside; a moving mesh ring lands on a mesh ring. The overlay prints where each dropped thing came to rest, measured, and P draws the colliders.
- `E05_3D_ShapeQueries`: the two questions a raycast cannot answer. A probe box follows the mouse over a field of bodies and lists what it holds through `Overlap`, a click tosses them; a ball is swept from a launcher through `SweepCast`, a ghost marks the first touch, and T throws a real ball, without gravity, to land where the ghost was.
- `E05_3D_Raycast`: T lists everything under the mouse, near to far, through `RayCastPenetrating`; three panels stand behind the sphere to be gone through.
- `E03_3D_MeshText` drops its words into a pile again, with a box per glyph and the last eight kept; this did not reach #450.
- A grabber in every Bepu example with bodies to pick up and a free left mouse button: `E01_2D_FallingShapes`, `E02_2D_Primitives`, `E04_2D_SpawnMenu`, `E05_2D_FallingShapes`, `E08_2D_DebugRender`, `E10_2D_StressPile`, `E01_3D_FallingShapes`, `E08_3D_CollidableGizmo` and `E08_3D_DebugRenderComponent`, one line each; the overlay says so where there is one.

### 🔧 Engineering

- The `text-3d` gold scene, the whole alphabet with a bevelled sample and a `Primitive3D.Text`, joins the gold images; the text pipeline is covered by `TextMeshTests`, `TextBevelTests`, `TextReviewTests` and `TextColliderTests`, every mesh checked watertight and outward-wound.
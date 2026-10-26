# Release Notes

Welcome to the Release Notes for the **Stride Community Toolkit**. This section aims to provide you with an organized, high-level summary of changes, enhancements, and fixes made in each version release. If you're looking to understand what has changed from one version to the next, you're in the right place.

## What to Expect

The Stride Community Toolkit is developed with rapid iteration in mind. It moves at a faster development pace compared to the Stride Game Engine. As a result, you should expect frequent updates that may introduce breaking changes. This fast-paced approach allows us to incorporate community feedback quickly and continue improving the toolkit.

## 1.0.0.0-preview.69

### 💥 Breaking Changes

#### The Debug names go

Three families were named for debugging and are used by games: the on-screen text panel, the key-driven list, and the renderer that labels entities. Their names now say what they do. Namespaces are unchanged.

| Before | After | Note |
|---|---|---|
| `DebugOverlay`, `DebugOverlaySection`, `DebugOverlayFontFamily`, `DebugOverlayFontResolver` | `TextOverlay`, `TextOverlaySection`, `TextOverlayFontFamily`, `TextOverlayFontResolver` | `DebugOverlay.GetOrCreate(game)` becomes `TextOverlay.GetOrCreate(game)`; every member keeps its name. |
| `SceneSetup.DebugOverlay` | `SceneSetup.TextOverlay` | The setup flag that creates the overlay before the first frame. |
| `DebugTextDropdown`, `DebugTextDropdownItem` | `KeyMenu`, `KeyMenuItem` | A key picks an item; nothing drops down. Members unchanged. |
| `EntityDebugSceneRenderer`, `EntityDebugSceneRendererOptions`, `EntityDebugRendererExtensions` | `EntityInfoRenderer`, `EntityInfoRendererOptions`, `EntityInfoRendererExtensions` | It labels entities with their name and position. |
| `game.AddEntityDebugSceneRenderer(options)`, `compositor.AddEntityDebugRenderer(options)` | `game.AddEntityInfoRenderer(options)`, `compositor.AddEntityInfoRenderer(options)` | One name on both receivers. |
| `rendering/debug-overlay.md` | `rendering/text-overlay.md` | The manual page; the old address is gone. |

Unchanged, because they are debugging aids: `AddPhysicsDebug`, the collider gizmos, the `DebugShapes` package, `DebugRenderComponentScript`, and Stride's own `DebugTextSystem`.

#### Three namespaces for text

`Stride.CommunityToolkit.Rendering.Text` held three subjects: text drawn with a font, lettering built as meshes, and the overlay. Each now has a namespace and a folder of its own, so one `using` no longer brings in the lot.

| Before | After | Note |
|---|---|---|
| `using Stride.CommunityToolkit.Rendering.Text;` for `EntityTextComponent`, `WorldTextComponent`, their renderers, `TextElement`, `TextAnchor`, `TextPositionMode`, `SystemFonts` | unchanged | Text drawn with a font stays. |
| `using Stride.CommunityToolkit.Rendering.Text;` for `MeshFont`, `TextMesh`, `TextMeshOptions`, `TextBevel`, `GlyphMesh`, `GlyphOutline`, `GlyphContour`, `GlyphShape`, `GlyphPolygon`, `OutlineSegment`, `OutlineSegmentKind` | `using Stride.CommunityToolkit.Rendering.Text.Meshes;` | Lettering as meshes. `Primitive3D.Text` and `TextCollider` stay where they were. |
| `using Stride.CommunityToolkit.Rendering.Text;` for `TextOverlay`, `TextOverlaySection`, `TextOverlayFontFamily`, `TextOverlayFontResolver`, `KeyMenu`, `KeyMenuItem`, `EntityInfoRenderer`, `EntityInfoRendererOptions`, `AddEntityInfoRenderer()` | `using Stride.CommunityToolkit.Rendering.Overlay;` | The overlay and the entity labels. `TextElement`, which sections return, stays in `Rendering.Text`, so an overlay section usually needs both. |

The compiler names every call site that needs a new `using`; nothing else changes.

#### One body mode on the physics options

Four option types with three overlapping knobs (`IncludePhysics`, `Component`, `Mass`, and `Depth` on the 2D pair) are two, and the kind of physics a primitive gets is one property.

| Before | After | Note |
|---|---|---|
| `Primitive3DEntityOptions`, `Primitive2DEntityOptions`, `PrimitiveEntityOptionsBase` | `PrimitiveEntityOptions` | One core type for both dimensions. |
| `Bepu3DPhysicsOptions`, `Bepu2DPhysicsOptions` | `BepuPhysicsOptions` | One Bepu type; `Component`, `Mass` and the new `Interpolation` on it. |
| `Bullet3DPhysicsOptions`, `Bullet2DPhysicsOptions` | `BulletPhysicsOptions` | One Bullet type; `PhysicsComponent` has no default any more, the mode makes one. |
| `IncludePhysics = false` | `Body = BodyMode.None` | `Body` is `Dynamic` by default; `Static`, `Kinematic` and `None` are the other modes. |
| `Component = new StaticComponent { Collider = new CompoundCollider() }` | `Body = BodyMode.Static` | The same for Bullet's `StaticColliderComponent`. A supplied component still wins over the mode. |
| `Component = new BodyComponent { Kinematic = true, Collider = new CompoundCollider() }` | `Body = BodyMode.Kinematic` | |
| `Component = new BodyComponent { Collider = new CompoundCollider(), InterpolationMode = InterpolationMode.Interpolated }` | nothing | A body the options make is interpolated by default; `Interpolation = false` turns it off. A supplied component keeps its own setting. |
| `new Bepu2DPhysicsOptions { Depth = 4 }` | `Primitive2D.RegularPolygon(1, 5, depth: 4)` | The collider depth is on the 2D shape; every factory takes it last. |
| `PhysicsSetupBase.AddStatic(...)` | `AddBody(...)` with `Body = BodyMode.Static` | The ground helpers set the mode; a package implements `AddBody` for both shape kinds only. |
| the ground helpers wrote the default name and material into the options they were given | `options.WithBody(mode)` | The grounds build from a copy; the options you pass come back as they were. `WithBody` is public, a shallow copy of any options type. |

A supplied component together with `Body = BodyMode.None` throws, since the two contradict each other. `Mass` with `Static` is ignored with a line in the log, since a static has none.

## 1.0.0.0-preview.68

## What's Changed

### 💥 Breaking Changes

- Built against Stride 4.4.1 (was 4.4.0). What a game sees from the engine: transparent materials are brighter, since alpha is applied once; Direct3D 12 needs no Developer Mode and ships the Agility SDK in a `D3D12` folder beside the executable; every effect, shader and material compiles again once on the first run; `MaxStepPerFrame` is honoured; a hybrid laptop runs on its discrete GPU.

#### Lettering comes from fonts

`LetterMeshFactory`, with its 37 hand-authored glyphs, is gone; `TextMesh` builds solid lettering from any font, with the toolkit's embedded Open Sans as the default.

| Before | After | Note |
|---|---|---|
| `LetterMeshFactory.CreateTextMeshDraw(device, "XYZ")` | `TextMesh.CreateMeshDraw(device, MeshFont.Default, "XYZ")` | In `Stride.CommunityToolkit.Rendering.Text`. Any character the font has, upper and lower case, digits, punctuation. |
| `CreateTextMeshDraw(..., depth: 0.3f, spacing: 0.1f, centerOrigin: true)` | `new TextMeshOptions { Depth = 0.3f, LetterSpacing = 0.1f, Anchor = TextAnchor.MiddleCenter }` | `Size` is world units per EM; the old glyphs were one unit tall, a capital is now about 0.7 of `Size`. |
| `LetterMeshFactory.SupportedCharacters`, `TryGetOutlines` | `font.HasGlyph(c)`, `font.GetGlyph(c)` | A glyph the font lacks draws its "missing glyph" box instead of throwing. |

### 🎉 New Features

- `EarClipping.Triangulate(outer, holes)` is new; the one-polygon overload is unchanged. `MeshBuilder.AddExtrudedPolygon` is unchanged.
- `scene.GetComponentsInScene<T>()` and `game.GetComponentsInScene<T>()`, with `GetComponentInScene<T>()` for the first match: every component of a type across a scene, its children and child scenes, or across every scene the game has loaded. The scene-wide half of the FindObjectOfType answer.
- `game.Script.RunOnGameThread(action)` and `RunOnGameThreadAsync(function)`: hand work from any thread to the next update, with the result back as a task. Ends "Collection was modified" from network callbacks and finished downloads.
- `transform.RotateAround(point, axis, angle)` swings an entity about an axis through a point and turns it by the same angle; `transform.FaceDirection(direction)` turns the forward axis along a world direction, with the same `smooth` as `LookAt`. Both update the world matrix.
- `MeshFont`: a font file opened for its glyph outlines through the FreeType the engine ships, so TrueType, OpenType and collections work on every platform. `MeshFont.Default` is an embedded Open Sans Regular; `Load(path)`, `Load(bytes)` and `FromFamily(family, style)` open any other. Outlines come back in EM units as curves (`GlyphOutline`, `GlyphContour`, `OutlineSegment`) and flatten into polygons with their holes sorted out (`GlyphShape`).
- `TextMesh`: solid 3D text from a `MeshFont`. `Create` gives CPU mesh data, `CreateMeshDraw` uploads it, `CreateGlyphs` gives one centred mesh per glyph for a body or an animation per letter, `Measure` gives the block. Lines, alignment, letter and line spacing, curve quality, the nine-point anchor, and texture coordinates on caps and walls, through `TextMeshOptions`.
- `Primitive3D.Text`: lettering as a shape, so `CreatePrimitive` gives it a material and a collider of one convex hull per glyph, on Bepu (`TextCollider`) and on Bullet.
- `TextBevel`: a rounded edge between the caps and the walls of 3D text, front and back, through `TextMeshOptions.Bevel`; a bevel wider than a glyph's thinnest stem shrinks for that glyph instead of folding.
- Instancing needs one call, not three: `UseInstancing()` on the scene setup puts the render feature in the compositor from the first frame, and `AddInstancingBufferUpload` adds it itself. A master whose compositor has no render feature used to draw as one model with nothing said; the run loop now logs a warning naming the entity.
- `EntityInstancing` and `BufferedEntityInstancing` unregister an instance whose entity leaves the scene, as the engine's `InstanceComponent` does; a removed entity no longer draws on as a ghost until `Clear()`.
- `AddOrientationGizmo()`: the world axes in a corner of the screen as arrows with white letters, in the proportions of Game Studio's space marker, turning with the camera and ignoring depth; `OrientationGizmoRenderer` for the corner, size, colours and opacity. `AddGroundGizmo` stays the world-space marker of the origin.
- `AddPhysicsDebug(contactsVisible: true)` draws the solver's contact points and normals with the debug meshes, the debug view Stride 4.4.1 added; `DebugRenderComponentScript.ShowContacts` is the same switch on the component. `E05_3D_TriggerZones` turns it on.
- `ToMeshDraw(device)`, `ToMesh(device)` and `Bounds()` on `GeometricMeshData<VertexPositionNormalTexture>`, for any procedural mesh data; `TextMesh.CreateMesh` and `CreateModel` give lettering with its bounds, so the renderer can cull it.

### 🐞 Bug Fixes

- `E20_3D_CubeCollapse`: the tower spread apart as it spawned. Since the scene setup chose Bepu (preview.67), `CreatePrimitive` gave every cube a dynamic body of its own on top of the spawner's `SlidingCubeComponent`, and the two pushed each other apart; the cube is now created with `IncludePhysics = false`.

### ✨ Enhancement

- The console example launcher prints the selected example's title before starting it, so you can identify the example after entering its number.
- `SceneSetup.None` still honours what is not scene: the physics engine, the game settings, the overlay and the profiler. `SceneSetup.None.UseBepu()` is a hand-built scene whose primitives and ground get Bepu bodies, the way a tutorial that adds the camera, light and ground line by line wants it; before, the engine chosen on it was silently dropped. It is a new instance each time.
- `AddSceneRenderer` keeps a `Game` overload with the signature binaries built against preview.66 and earlier bind to, so `Gum.Stride` 2026.10.1.1, built against preview.63, runs again; source binds to the `IGame` overload as before.
- `E02_3D_MaterialPreview` runs on `SceneSetup.None` again: the default scene's ground, light and sky had crept under the thumbnail rig since the scene setup became data. The thumbnail camera has the controller; the editor's pose is the start and H brings it back.
- `GrabberScript` picks through trigger volumes: a body inside a zone can be grabbed, since a collidable whose handler says `NoContactResponse` does not stop the pick ray; `TryPick(segment, out hit)` is that pick, for a game that wants the same rule.
- `GrabberScript` serves a 2D scene as it is: under an orthographic camera the pick ray runs along the view and a `Body2DComponent` is carried across its plane; the mouse wheel does nothing for a 2D body or under an orthographic camera, since depth there is the plane itself. `game.AddGrabber()` is the same call in 2D and 3D. The reach (`MaxDistance`) defaults to 100 instead of 50: a 2D camera stands 50 from its bodies, and the stress pile is viewed from 80.

### 📄 Docs

- The glossary is in the top navigation, and defines gizmo, mesh collider, glyph and EM.
- FAQ: why a HUD never goes through the tone map (Stride's UI stage and the toolkit's overlays draw after the post effects), and the scene-wide search and game-thread helpers in the FindObjectOfType and background-thread answers. The entity extensions page gains a turning-and-moving section for the transform helpers.
- A manual page for the orientation gizmo (`rendering/orientation-gizmo.md`), with the two gizmos side by side.
- A correctness and verbosity sweep of the whole `docs/` tree against the code: stale physics plumbing (`CreatePrimitive` overloads that no longer exist, `SetupBase*`), wrong defaults, keys and paths, dead links, the superseded Windows package, the four packages missing from the libraries list, example pages whose code includes swallowed the metadata block or described code that had changed, and the story-led rendering pages (render to texture, GPU picking, MeshBuilder, TextureCanvas) rewritten as reference pages.
- Game Studio: a table of every toolkit component in the Add component menu, with its package, category and page.
- The examples pipeline's design-decisions page is gone from the contributing section; the three rules a contributor needs that were only there (why slugs carry no level, the metadata block last in the file, the navigation depth) are on the examples page and in the generator README.
- Every example page is now owned by the metadata block in its example: 24 hand-written pages became generated, and four keep a hand-written tail below the generated part (`generated: partial`) for a second file, a multi-file walkthrough or the SignalR how-to.
- Physics extensions: a section on the simulation clock (`TimeScale`, `Enabled`, `PoseGravity`, `MaxStepPerFrame`, `ISimulationUpdate`), a Triggers and contact events section (`Trigger`, `IContactHandler`, what the callbacks get and when they run), a Shape queries section (`Overlap`, `OverlapInfo`, `SweepCast`, `SweepCastPenetrating`, what the distance means), and a Mesh colliders section, what a `MeshCollider` on a static keeps that a hull fills in, and what a mesh is not (a surface with no inside), measured in `E05_3D_MeshColliders`.
- A manual page for 3D text (`rendering/text-3d.md`); the MeshBuilder page points at it. A second page, `rendering/text-3d-pipeline.md`, walks the pipeline from the font file to the mesh, with the files behind each stage.

### 🎓 Examples

- Cube Collapse orbits its camera with `RotateAround` and turns its falling letters with `FaceDirection`, in place of the quaternion maths each script carried.
- `E04_Myra_DraggableWindow` is back in the solution and the docs: Myra.Stride 1.6.8 carries the Stride 4.4 retarget, so it builds and runs again.
- `E04_Myra_ControlPanel`: Myra's widget set in one draggable window that drives a Stride scene. A menu bar, tabs, sliders for the light and the physics clock, a combo for gravity, check buttons for shadows and collider gizmos, a spin button and buttons that spawn bodies, a list view that selects and lights one, a dialog before clearing, tooltips and a status line.
- `E03_3D_TextShaders`: four materials on the same solid lettering, a gradient texture through the caps' texture coordinates, a rim light as a shader-class colour node, a vertex-stage wave on `Global.Time` and a pixel-stage dissolve driven per frame, each shader in the example's `Effects` folder.
- `E03_3D_MeshText` is 3D text from a font: the embedded font, an installed font found by family, lines that align in one mesh, the frame counter, and words with physics through `Primitive3D.Text`. `E07_3D_ProceduralGeometry` and `E20_3D_CubeCollapse` use `TextMesh` for their lettering.
- `E05_3D_MeshColliders`: a convex hull against a mesh collider on the same ring model, with a ball resting on the hull's lid and falling through the mesh's hole; a mesh sphere with a ball outside and one inside shows that a mesh has no inside; a moving mesh ring lands on a mesh ring. The overlay prints where each dropped thing came to rest, measured, and P draws the colliders.
- `E05_3D_ShapeQueries`: the two questions a raycast cannot answer. A probe box follows the mouse over a field of bodies and lists what it holds through `Overlap`, a click tosses them; a ball is swept from a launcher through `SweepCast`, a ghost marks the first touch, and T throws a real ball, without gravity, to land where the ghost was.
- `E05_3D_Raycast` casts two rays: the camera's finds the clicked point, and a second one from the sphere through that point lists everything in that direction through `RayCastPenetrating`, drawn as the line; panels stand around the sphere to be gone through.
- `E05_3D_TimeControl`: the simulation's own clock. `TimeScale` from 1/16x to 4x, `Enabled` to pause, `PoseGravity` cycled through Earth, Moon, none and upside down, a dropper on the game clock against a spinning bar on the physics clock, and an `ISimulationUpdate` component counting the steps.
- `E05_3D_TriggerZones`: reacting to contact. A `Trigger` zone with no model counts who enters and leaves, a solid plate with an `IContactHandler` counts what stands on it, and a crate measures each hit with `ComputeImpactForce`. Space drops balls, the grabber carries them.
- A sweep of the example comments: references to the helpers that `SceneSetup3D` replaced are corrected, comments that only restated the code and leftover commented-out code are gone, and the longest metadata descriptions are shorter.
- A grabber in every Bepu example with bodies to pick up and a free left mouse button: `E01_2D_FallingShapes`, `E02_2D_Primitives`, `E04_2D_SpawnMenu`, `E05_2D_FallingShapes`, `E08_2D_DebugRender`, `E10_2D_StressPile`, `E01_3D_FallingShapes`, `E08_3D_CollidableGizmo` and `E08_3D_DebugRenderComponent`, one line each; the overlay says so where there is one.

### 🔧 Engineering

- The `text-3d` gold scene, the whole alphabet with a bevelled sample and a `Primitive3D.Text`, joins the gold images; the text pipeline is covered by `TextMeshTests`, `TextBevelTests`, `TextReviewTests` and `TextColliderTests`, every mesh checked watertight and outward-wound.
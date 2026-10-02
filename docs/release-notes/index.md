# Release Notes

Welcome to the Release Notes for the **Stride Community Toolkit**. This section aims to provide you with an organized, high-level summary of changes, enhancements, and fixes made in each version release. If you're looking to understand what has changed from one version to the next, you're in the right place.

## What to Expect

The Stride Community Toolkit is developed with rapid iteration in mind. It moves at a faster development pace compared to the Stride Game Engine. As a result, you should expect frequent updates that may introduce breaking changes. This fast-paced approach allows us to incorporate community feedback quickly and continue improving the toolkit.

## 1.0.0.0-preview.66

<!-- If needed add more categories -->

## What's Changed

### 💥 Breaking Changes

- `AddBepu3DPhysics`, `AddBepu2DPhysics` and the Bepu `Create3DPrimitive` / `Create2DPrimitive` overloads no longer add the fitted collider to a compound collider that already holds a shape. Code that passed a placeholder such as `new CompoundCollider { Colliders = { new BoxCollider() } }` and relied on the fitted shape now gets only the placeholder, at its default size: pass an empty `CompoundCollider` instead.
### 🎉 New Features

- `Buffer.SetDataPinned(commandList, span)` in `Stride.CommunityToolkit.Graphics`: uploads a span with its memory pinned for the call, for data held in an array or a list.
- `MaterialDescriptors.Overlay(colour, intensity)` and `game.CreateOverlayMaterial(colour, intensity)`: an unlit translucent colour with the colour's alpha as the opacity, for zones, placement previews and markers over the scene.
- `TextureLoader.FromPixels(device, pixels, width, height, options)`: makes a texture from pixels computed in code, prepared by role and with mipmaps, like a loaded file.

### 🐞 Bug Fixes

- `AddBepu3DPhysics`, `AddBepu2DPhysics` and the `Create3DPrimitive` / `Create2DPrimitive` Bepu overloads added the collider fitted to the primitive even to a compound collider that already held the caller's own shape, so the body had two colliders and the mass of both. The fitted shape is now added to an empty compound only; a compound that holds a shape is kept as it is. This corrects `E05_3D_Grabber`, `E05_3D_Car` and `E05_3D_Cloth`, whose bodies were heavier than written.
- `ShapeBatch`, `BufferedEntityInstancing` and the charts' ribbon lines uploaded managed arrays through an engine path that, on Direct3D 11, does not pin them. A garbage collection during the upload could corrupt it. They now upload through the new `Buffer.SetDataPinned` extension.
- `ShapeBatch`: a `DrawLine` shorter than its width took the current fill instead of drawing solid.
- `ShapeBatch`: a circle of radius zero, or a polyline whose points coincide, uploaded a zero scale that the shader divided by. Such a shape is now a dot the width of its border. A scale of zero or less, or a value that is not a number, draws nothing.
- `ShapeBatch.DrawRectangle`: a corner radius above half the smaller side enlarged the rectangle. The radius is now limited to that half.
- `ShapeBatch`: dashes on space strokes (`DrawPolyline` and `DrawPixelPolyline` with 3D points) did not follow the display scale.
- `ShapeBatch`: a space stroke under an orthographic camera was clipped whole when its first point lay beyond the far plane.
- `ShapeBatch`: the dither added faint noise to pixels a shape left empty, such as the area around its outline or a dash gap.
- `RemoveShapeBatch`: the removed batch stayed registered as the default, so shape components kept drawing into it. It also no longer publishes the removed batch's pick records. `Chart.Dispose` removes its own batch the same way.
### 🎨 Rendering

- `ShapeBatch`: a shape is rasterized as its own bounding box instead of the square around its longest side, so the pixel shader no longer runs over the empty rest of the square. Long lines, polylines, narrow columns and wide panels draw several times faster; round shapes are unchanged. Small shapes also keep their full anti-aliased edge, which the old margin could clip.
- `ShapeBatch`: an open polyline is read from the caller's span with no scratch copy, and arc lengths are summed only for a dashed run of more than one piece.
- `ShapeBatch`: a textured batch generates its fill source once per frame instead of once per view, and not at all when it has nothing to draw.
### ✨ Enhancement

### 📄 Docs

- Materials manual: the overlay helper, the sample recipes, and a corrected remark on dithered shadows: `DitheredShadows = false` gives a full shadow, not none.
- Materials manual: texture node options, operators, occlusion, and remarks on multi-pass features, dithered shadows and two-sided lighting.
- ShapeBatch manual: new section on using a shader class as a fill.

### 🎓 Examples

- A sweep of every code-only example: typos, comments and metadata that contradicted the code, clearer names, dead code, and small bugs. The bugs: a collected coin drawn again in `E02_2D_EasingInGame`; degrees passed as radians in `E02_3D_Primitives`; a material appended per click in `E04_CubeClicker`; entities removed while enumerating in both `E04_StrideUI_DragAndDrop` examples; resets that built a second scene on top of the first in `E05_3D_Grabber` and `E05_3D_Constraints`; an extra capsule in `E05_2D_FallingShapes`; unequal masses in `E06_Box2D_Explosion`; normals that were not unit length in the `E07` cylinder and torus meshes; batches short by up to 69 bodies in both `E10_2D_StressPile` examples; a sound instance leaked per chime in `E12_Audio_Spatial`; a key bound twice in `E13_SignalR`; repeat-click on the left shift key only in `E20_3D_CubeCollapse`; and buffers or textures that were never released in five examples.
- `E05_3D_Car`: the ramp, the pillars and the crates collide as they are drawn. The pillars are now 3 m tall, as their colliders always were.
- `E11_2D_Charts` and `E11_3D_Charts`: the key help uses the overlay's key format.
- New example `E05_3D_CubeFountain`: a fountain of cubes, spheres and cylinders whose rate is counted in `ISimulationUpdate`, so it follows the simulation's time and slows or stops with `BepuSimulation.TimeScale`. Slow motion shows what `BodyComponent.InterpolationMode` is for, with a key to turn it off. A capped store of bodies is reused through `Teleport`, and one instancing master per shape draws them all.
- New example `E20_2D_Pong`: Pong in three small scripts with no assets and no physics engine. The paddles and the ball are `SyncScript`s, the referee is an `AsyncScript` that runs a match as one method, and the ball reports a point through an `EventKey`. The computer plays both sides until Space is pressed.
- `E02_3D_Material_Gallery`: new station **Sample recipes**, five tunings from the engine's sample materials: a brushed metal from a mirrored gloss map, a specular map reused as roughness, a weakened normal map, a neon sign as a masked emissive layer, a tinted prototyping grid. The **Transparency** station gained an overlay variation.
- `E02_3D_Material_Gallery`: new variations. **Albedo texture**: random coordinates, swizzle, fallback value. **Gloss and metal maps**: two maps packed into one texture. **Occlusion**: direct light and a cavity map. **Node arithmetic**: `Add` and `AddMath`. `--clean` hides the overlay.
- `E09_3D_Particles_Gallery`: six thruster stations, 33 to 38: **Kerosene engine**, **Engine cluster**, **Methane engine**, **Jet afterburner**, **Solid booster** and **Small thrusters**, three variations each. They use no shader of their own. The station **Rocket engine** is renamed **Landing burn**. `--clean` hides the overlay.
- `E09_3D_Particles_Gallery`: colour updaters now colour the particles. The gallery's materials did not read the particle colour, so colour curves had no effect.
- `E09_3D_Particles_Gallery`: the **Ribbon** station no longer shows lines at the ends of its path, and the **Landing burn** flame ends above the pad.
- `E03_2D_HUD`: reworked. One widget per file, three columns of panels in a frame, ten colour schemes on keys 1 to 9 and 0, and new contacts, target and power panels. Contacts, wing tiles, mode buttons and the power triangle respond to clicks through `ShapeBatch` picking. The panel background is a shader fill with two patterns, lines and squares, on key G. `--scheme N` starts in a scheme.
- `E02_3D_Material_Gallery`: the hair and subsurface scattering stations are always on and require a Stride build newer than 4.4.0-beta8. The `--engine-fix` switch is removed.
- `E11_3D_ShapeBatch_Gallery`: new station **Shader fill**, a fill computed by a shader class instead of a texture. The gallery picture has mipmaps.
- `E09_3D_Particles_Gallery`: new station **Shader node**, particles drawn by a shader class instead of a texture. `--station N --variation M` starts at a variation.

### 🔧 Engineering

- NDepend: the remaining issues are cleared. Members of internal types are declared `internal` or `private`, and two complex methods gained comments.
- NDepend critical rules: `DebugOverlay.Draw` is split into measure and draw steps, the galleries' index board is its own class (`GalleryBoard`), and three example helpers take a settings value instead of a long parameter list (`HudStyle` in `E03_2D_HUD`, `PlumeShape` and `SmokeLook` in `E09_3D_Particles_Gallery`). No public API changed.
- The **.NET Build Test** workflow runs on pull requests that touch `src`, the test project or the build files. It builds every library and runs the unit tests in Release, as the release workflow does. It also builds `Stride.CommunityToolkit.Effects`, which was missing from its list.
- Removed the repository's `Directory.Build.targets`, which replaced Stride's `StrideSortItems` task to silence CS0162. Stride builds newer than 4.4.0-beta8 do not raise the warning.
- `Directory.Build.local.props`: an optional, git-ignored file that overrides build properties locally, such as `StrideVersion` for a Stride build from source. See the contributing build page.

### 💪 Other Changes
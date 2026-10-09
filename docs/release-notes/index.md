# Release Notes

Welcome to the Release Notes for the **Stride Community Toolkit**. This section aims to provide you with an organized, high-level summary of changes, enhancements, and fixes made in each version release. If you're looking to understand what has changed from one version to the next, you're in the right place.

## What to Expect

The Stride Community Toolkit is developed with rapid iteration in mind. It moves at a faster development pace compared to the Stride Game Engine. As a result, you should expect frequent updates that may introduce breaking changes. This fast-paced approach allows us to incorporate community feedback quickly and continue improving the toolkit.

## 1.0.0.0-preview.66

<!-- If needed add more categories -->

## What's Changed

### 💥 Breaking Changes

- Built against Stride 4.4.0, the first stable 4.4 release (was 4.4.0-beta8).
- `ShapeBatch`: the default fill is solid. `Fill.Alpha` defaults to 1 (was 0.6), so a draw call with one colour draws a solid shape in that colour; it used to draw an outline around a dimmed, see-through inside. For the old look set `Fill.Alpha = ShapeFill.TestbedAlpha`. `Box2DDebugDraw` keeps that look through its new `FillAlpha` property. A `ShapeComponent` on a batch that never set its fill is solid too.
- `AddBepu3DPhysics`, `AddBepu2DPhysics` and the Bepu `Create3DPrimitive` / `Create2DPrimitive` overloads no longer add the fitted collider to a compound collider that already holds a shape. Code that passed a placeholder such as `new CompoundCollider { Colliders = { new BoxCollider() } }` and relied on the fitted shape now gets only the placeholder, at its default size: pass an empty `CompoundCollider` instead.

### 🎉 New Features

- `ShapeBatch`: 2D overloads that draw in the XY plane without plane axes: `DrawRectangle(Vector2 center, Vector2 size, color, cornerRadius, rotation)`, `DrawDisc(Vector2, radius, color)`, `DrawRing(Vector2, radius, color)`, `DrawLine(Vector2, Vector2, width, color)` and `DrawPixelLine(Vector2, Vector2, pixelWidth, color)`. The 2D examples and the HUD tutorial use them.
- `EntityTextComponent`: `EnableOutline`, `OutlineColor` and `OutlineWidth` draw an outline round every glyph, white and 1 pixel by default, for text that must stay readable over anything.
- `game.AddGrid()` in `Stride.CommunityToolkit.Shapes`: a reference grid with numbered lines that shows world coordinates or screen pixels. In the world it lies in the XY plane of a 2D scene and on the ground of a 3D one, with the axes in Game Studio's colours. The key G steps through off, world and screen; `ReferenceGrid.Cycle()`, `Visible` and `Space` do the same from code. It can write the coordinates under the mouse in both spaces.
- `Buffer.SetDataPinned(commandList, span)` in `Stride.CommunityToolkit.Graphics`: uploads a span with its memory pinned for the call, for data held in an array or a list.
- `MaterialDescriptors.Overlay(colour, intensity)` and `game.CreateOverlayMaterial(colour, intensity)`: an unlit translucent colour with the colour's alpha as the opacity, for zones, placement previews and markers over the scene.
- `TextureLoader.FromPixels(device, pixels, width, height, options)`: makes a texture from pixels computed in code, prepared by role and with mipmaps, like a loaded file.

### 🐞 Bug Fixes

- `Scene.GetCamera(name)` returned the last camera it looked at when no entity had the name. It now returns `null`, and it finds cameras on child entities at any depth.
- Bullet `Add2DGround`, `Add3DGround` and `AddInfinite3DGround`: passing any options made the ground a dynamic rigid body, because the options default to one. The ground is static unless the options set `PhysicsComponent`.
- `Random.NextDirection2D` and `NextDirection3D` returned directions only into the positive quadrant or octant. They now return every direction with equal likelihood.
- `E01_3D_BasicScene_FileBasedApp`: removed a `NuGet.config` that had been committed with a package source on a local drive. It sent every toolkit package to that path, so `ProgramSimple.cs` failed to restore with `NU1301` on any machine that did not already have the packages cached.
- `AddBepu3DPhysics`, `AddBepu2DPhysics` and the `Create3DPrimitive` / `Create2DPrimitive` Bepu overloads added the collider fitted to the primitive even to a compound collider that already held the caller's own shape, so the body had two colliders and the mass of both. The fitted shape is now added to an empty compound only; a compound that holds a shape is kept as it is. This corrects `E05_3D_Grabber`, `E05_3D_Car` and `E05_3D_Cloth`, whose bodies were heavier than written.
- `ShapeBatch`, `BufferedEntityInstancing` and the charts' ribbon lines uploaded managed arrays through an engine path that, on Direct3D 11, does not pin them. A garbage collection during the upload could corrupt it. They now upload through the new `Buffer.SetDataPinned` extension.
- `AddShapeBatch(afterPostEffects: true)`: two batches drawn after the post effects swapped their draw order from frame to frame, so overlapping shapes flickered. The toolkit's UI render stage had no sort mode. It now sorts back to front, and batches in one stage draw in the order they were added. The screen reference grid always draws over them.
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

### 📄 Docs

- Rendering manual: new page "Reference Grid".
- Manual: new page "Frequently asked questions", 52 short answers to the questions asked most often about building a Stride game from code, gathered from the old Stride forum before it closes and checked against Stride 4.4.
- Materials manual: a layer needs only `new Material { Descriptor = descriptor }`; compiling the layer on its own first is wasted work. The material gallery's Layers station builds its layers this way.
- Manual and API reference: a wording and accuracy pass. Stale statements are corrected against the code (screen positions start top left, the 2D camera pans with the right mouse button, the debug overlay font is 14 pixels, the text renderers add themselves, the chart grid has no texture), and history and repetition are removed.
- ShapeBatch manual: the draw state table lists the defaults, and a new section "Outline and fill" explains them. Corrected: the border is centred on a shape's edge, half inside and half outside.
- New tutorial "Build a simple HUD with ShapeBatch", for beginners: eight steps from an empty window to a HUD with a health bar, an energy dial, a crosshair, labels, a warning panel and a damage flash, ending with the HUD moved into a class. Every code block on the page is taken from the example it describes. The ShapeBatch manual page links to it.
- Create File-Based App: the page shows `ProgramSimple.cs` from the example folder instead of a copy, so the package version is kept in one place. It uses `1.0.0-preview.66`.
- The console launcher's menu shown in the examples pages is refreshed.
- Engine-only pages: the Stride version in the package lines is `4.4.0`, and `dotnet add package` for the Stride packages no longer needs `--prerelease`. The file-based page's last section is "See it in the repository", links to the two files, and says how the repository's `Directory.Build.targets` differs from the one the page has you write.
- Glossary: new entries **Playground**, **Body interpolation**, **Contact spring** and **Sleeping**; **Restitution / friction** now says how Bepu does both.
- Removed the empty "What's new in docs" page.
- Physics manual: new page "Bepu: Bounce and Friction", the four contact properties of a collidable, how a pair combines them, and measured rebounds.
- Materials manual: the overlay helper, the sample recipes, and a corrected remark on dithered shadows: `DitheredShadows = false` gives a full shadow, not none.
- Materials manual: texture node options, operators, occlusion, and remarks on multi-pass features, dithered shadows and two-sided lighting.
- ShapeBatch manual: new section on using a shader class as a fill.

### 🎓 Examples

- `E20_3D_CubeCollapse`: palettes Classic, Soft, High visibility, Examples (the examples' colour theme), Glass and Frosted glass (the material gallery's thin glass under its sun and key light). The colour palettes use the lit material and light of the Bepu playground plus a weak fill light, and no longer cast the studio key light's shadow. Scores pop up in the colour of the cleared cubes with a white outline, larger and rising higher. At game over the camera eases back so the menu is in view. A hovered lone cube no longer dims. `--palette N` starts on a palette.
- New example `E03_2D_HUD_Basics`: the program of the HUD tutorial. The keys 1 to 8 show the HUD as it stands after each step; step 8 draws it from `SimpleHud.cs` and `HudStyle.cs`.
- New examples `E01_2D_FallingShapes` and `E01_3D_FallingShapes`: the step after the basic scene. Thirty shapes of five kinds and five colours drop in a column and topple into a pile. The 2D and 3D versions use the same shapes and colours, and their code differs only in the scene setup, the primitive type and the material.
- New example `E05_3D_PhysicsMaterials`: seven identical balls dropped and six identical boxes pushed, on lanes that differ only in `SpringFrequency`, `SpringDampingRatio`, `FrictionCoefficient` and `MaximumRecoveryVelocity`. The overlay prints the rebound and the slide each lane measured.
- New example `E05_3D_CubeFountain`: a fountain of cubes, spheres and cylinders whose rate is counted in `ISimulationUpdate`, so it follows the simulation's time and slows or stops with `BepuSimulation.TimeScale`. Slow motion shows what `BodyComponent.InterpolationMode` is for, with a key to turn it off. A capped store of bodies is reused through `Teleport`, and one instancing master per shape draws them all.
- `E05_3D_CubeFountain`: the comment on `MaxStepPerFrame` says Stride 4.4.0 ignores the cap and Stride 4.5 applies it.
- New example `E20_2D_Pong`: Pong in three small scripts with no assets and no physics engine. The paddles and the ball are `SyncScript`s, the referee is an `AsyncScript` that runs a match as one method, and the ball reports a point through an `EventKey`. The computer plays both sides until Space is pressed.
- `E02_3D_Material_Gallery`: new station **Sample recipes**, five tunings from the engine's sample materials: a brushed metal from a mirrored gloss map, a specular map reused as roughness, a weakened normal map, a neon sign as a masked emissive layer, a tinted prototyping grid. The **Transparency** station gained an overlay variation.
- `E02_3D_Material_Gallery`: new variations. **Albedo texture**: random coordinates, swizzle, fallback value. **Gloss and metal maps**: two maps packed into one texture. **Occlusion**: direct light and a cavity map. **Node arithmetic**: `Add` and `AddMath`. `--clean` hides the overlay.
- `E09_3D_Particles_Gallery`: six thruster stations, 33 to 38: **Kerosene engine**, **Engine cluster**, **Methane engine**, **Jet afterburner**, **Solid booster** and **Small thrusters**, three variations each. They use no shader of their own. The station **Rocket engine** is renamed **Landing burn**. `--clean` hides the overlay.
- `E03_2D_HUD`: reworked. One widget per file, three columns of panels in a frame, ten colour schemes on keys 1 to 9 and 0, and new contacts, target and power panels. Contacts, wing tiles, mode buttons and the power triangle respond to clicks through `ShapeBatch` picking. The panel background is a shader fill with two patterns, lines and squares, on key G. `--scheme N` starts in a scheme.
- `E02_3D_Material_Gallery`: the hair and subsurface scattering stations are always on and require Stride 4.4.0-beta9 or later. The `--engine-fix` switch is removed.
- `E02_3D_Material_Gallery` and `E20_3D_CubeCollapse`: the thin-glass blend state workaround is removed. Stride 4.4.0-beta9 generates the transmittance pass with its blend state.
- `E11_3D_ShapeBatch_Gallery`: new station **Shader fill**, a fill computed by a shader class instead of a texture. The gallery picture has mipmaps.
- `E09_3D_Particles_Gallery`: new station **Shader node**, particles drawn by a shader class instead of a texture. `--station N --variation M` starts at a variation.
- `E04_ImGuiNet`: the orbiting cube was driven with `SetTargetPose` from a per-frame `Update`. It now sets a velocity, like `E02_3D_SyncScript`.
- `E05_3D_Cloth`: the solver comment had Bepu's `SolveDescription(8, 1)` backwards; it is eight velocity iterations in one substep, where the example runs eight substeps. The text now says so, and that either works.
- `E02_3D_SyncScript`: the script drove its kinematic body with `SetTargetPose` from a per-frame `Update`, the pattern the toolkit's guidance warns against. It now sets a velocity, like `E02_3D_GiveMeACube`, and starts on its circle.
- A sweep of every code-only example: typos, comments and metadata that contradicted the code, clearer names, dead code, and small bugs. The bugs: a collected coin drawn again in `E02_2D_EasingInGame`; degrees passed as radians in `E02_3D_Primitives`; a material appended per click in `E04_CubeClicker`; entities removed while enumerating in both `E04_StrideUI_DragAndDrop` examples; resets that built a second scene on top of the first in `E05_3D_Grabber` and `E05_3D_Constraints`; an extra capsule in `E05_2D_FallingShapes`; unequal masses in `E06_Box2D_Explosion`; normals that were not unit length in the `E07` cylinder and torus meshes; batches short by up to 69 bodies in both `E10_2D_StressPile` examples; a sound instance leaked per chime in `E12_Audio_Spatial`; a key bound twice in `E13_SignalR`; repeat-click on the left shift key only in `E20_3D_CubeCollapse`; and buffers or textures that were never released in five examples.
- `E05_3D_Car`: the ramp, the pillars and the crates collide as they are drawn. The pillars are now 3 m tall, as their colliders always were.
- `E11_2D_Charts` and `E11_3D_Charts`: the key help uses the overlay's key format.
- `E09_3D_Particles_Gallery`: colour updaters now colour the particles. The gallery's materials did not read the particle colour, so colour curves had no effect.
- `E09_3D_Particles_Gallery`: the **Ribbon** station no longer shows lines at the ends of its path, and the **Landing burn** flame ends above the pad.
- `E05_2D_FallingShapes`: its docs page moved from `falling-shapes-2d` to `falling-shapes-physics-2d` and it is titled "Falling Shapes with Physics Options (2D)". The old address now shows `E01_2D_FallingShapes`.
- Example titles: "Basic2D Scene" and "Basic3D Scene" are written "Basic 2D Scene" and "Basic 3D Scene" everywhere.
- `E01_2D_BasicScene_Bullet`: reset to the same scene as `E01_2D_BasicScene`, one capsule, so the pair differs only in the physics package, as the 3D pair does. It had grown into a column of 31 capsules with a profiler.

### 🔧 Engineering

- The five SixLabors.ImageSharp 3.1.12 advisories that come with Stride.Foundation are suppressed with `NuGetAuditSuppress` in the repository build, until Stride ships ImageSharp 4.1.2. Any new advisory still warns.
- `WindowsDpiManager`: Release builds no longer warn CS0168. The debug lines need no `#if DEBUG`, because `Debug.WriteLine` is already left out of Release builds.
- `Stride.CommunityToolkit.Shapes`: the source files are sorted into folders by what works together (`Batch`, `Style`, `Picking`, `Rendering`, `Components`, `Grid`). The namespace is unchanged.
- Removed two diagnostic probe projects, `_Temp2DProbe` and `_TempMemProbe`, from `examples/code-only`. They were never examples and were not in the solution.
- `Example.Common`: new `ColorTheme` and `ColorThemes.Default`, five named accents (blue, orange, red, green, purple) that read against both the 2D background and the 3D ground. The playgrounds use it.
- `Example_2D_Playground` and `Example_Bepu_Playground` are reduced to a matching pair of scratch scenes on Bepu, 2D and 3D, with the same five shapes and a reference grid: Space adds ten, X removes them, G steps the grid. The 2D one no longer uses Bullet. They stay out of the docs and the launcher.
- NDepend: the remaining issues are cleared. Members of internal types are declared `internal` or `private`, and two complex methods gained comments.
- NDepend critical rules: `DebugOverlay.Draw` is split into measure and draw steps, the galleries' index board is its own class (`GalleryBoard`), and three example helpers take a settings value instead of a long parameter list (`HudStyle` in `E03_2D_HUD`, `PlumeShape` and `SmokeLook` in `E09_3D_Particles_Gallery`). No public API changed.
- The **.NET Build Test** workflow runs on pull requests that touch `src`, the test project or the build files. It builds every library and runs the unit tests in Release, as the release workflow does. It also builds `Stride.CommunityToolkit.Effects`, which was missing from its list.
- Removed the repository's `Directory.Build.targets`, which replaced Stride's `StrideSortItems` task to silence CS0162. Stride 4.4.0-beta9 does not raise the warning.
- `Directory.Build.local.props`: an optional, git-ignored file that overrides build properties locally, such as `StrideVersion` for a Stride build from source. See the contributing build page.
# Release Notes

Welcome to the Release Notes for the **Stride Community Toolkit**. This section aims to provide you with an organized, high-level summary of changes, enhancements, and fixes made in each version release. If you're looking to understand what has changed from one version to the next, you're in the right place.

## What to Expect

The Stride Community Toolkit is developed with rapid iteration in mind. It moves at a faster development pace compared to the Stride Game Engine. As a result, you should expect frequent updates that may introduce breaking changes. This fast-paced approach allows us to incorporate community feedback quickly and continue improving the toolkit.

## 1.0.0.0-preview.65

## What's Changed

### 💥 Breaking Changes

- `game.CreateMaterial(color, metalness, glossiness)`: parameters renamed from `specular` and `microSurface`. The default metalness is 0 instead of 1, so a colour alone gives a matte material. Positional calls compile unchanged; named arguments need updating.
- `Easing` is one generic implementation: `Easing.Name<T>(T amount)` for any IEEE floating-point type replaces the `float` and `double` method pairs. `Easing.Ease` and `MathUtilEx.Interpolate` clamp time to [0, 1]; the curve methods stay unclamped.
- `WorldTextRenderer`, `EntityTextRenderer`, `ScreenTextDrawer` and `ScreenTextStyle` moved from `Stride.CommunityToolkit.Renderers` to `Stride.CommunityToolkit.Rendering.Text`.
- `ShapeComponent.Vertices` is a `List<Vector2>` instead of an array. Assign an array with a spread: `Vertices = [.. outline]`.
- `WorldTextComponent` and `EntityTextComponent`: `FadeStartDistance` and `MaxDistance` are `float`, with 0 meaning off, instead of `float?`.
- Box2D: `JointOptions2D` renamed to `JointOptionsBase`. `CharacterMover2D` category bits are `static readonly` instead of `const`.
- `ShapeBatchExtensions.EffectName` removed.
- `ShapeBatch` composites premultiplied in linear light on an sRGB target. Colours now match their sRGB values and look brighter than before. Border colour alpha applies, and a glow's alpha is its strength.
- The `Display` names of `ShapeComponent`, `WorldTextComponent` and `EntityTextComponent` lost their "(call Add…)" suffix.

### 🎉 New Features

- **New package `Stride.CommunityToolkit.Effects`**: toolkit features that ship a shader. Contains the `NightVision` and `Thermal` post-processing colour transforms and GPU picking.
- **GPU picking**: `game.AddGpuPicker()` reports the entity, mesh, material, instance index and surface point under a screen position. No colliders needed. The result arrives two frames after the request. `Continuous` follows the mouse and `Pickable` limits the pass to render groups.
- **Render to texture**: `game.AddRenderTextureCamera(entity, width, height)` draws a camera into a texture alongside the main view. HDR by default, with optional post effects per feed.
- **`MaterialDescriptors`**: the descriptors behind the material helpers: `Pbr`, `Textured`, `Emissive`, `Screen`, `Flat` and `Highlight`. Change one and compile it with `game.CreateMaterial(descriptor)`, which keeps the descriptor on the material.
- New material helpers: `CreateEmissiveMaterial`, `CreateTexturedMaterial` and `CreateScreenMaterial`.
- **`MaterialParameters`**: changes a compiled material at runtime without a rebuild. `Set` writes to every pass, `SetColor` converts to linear premultiplied, `SetTextureOffset` and `SetTextureScale` scroll and retile a texture, and `ColorKey`, `FloatKey` and `TextureKey` create named keys.
- **`MaterialStreamView`**: Game Studio's material view modes in code. `game.AddMaterialStreamView()` draws every mesh with one material stream as its colour: diffuse, colour base, specular, glossiness, tangent or world normal, occlusion, cavity or emissive.
- **`TextureLoader`**: loads image files by role, as the content pipeline would import them. `Color` is sRGB and premultiplied, `Data` is linear, `NormalMap` is linear with an optional green flip. Every texture gets a mipmap chain built on the CPU. `.dds` files pass through. `TexturePixels` exposes the pixel operations.
- **`HighlightShell`**: a hover or selection highlight made from a material and an entity, with no render feature or post effect. `Show(model)` follows an entity, `Show(model, world)` highlights one instance of an instanced model, `Hide()` removes it. Draws in render group 30 so a picker can exclude it.
- **`Tween`**: a duration, a curve and a loop mode (`None`, `Repeat`, `PingPong`) with `Start`, `Stop`, `Resume` and `Update`, plus `Lerp` and `Slerp` on the eased value.
- Easing: `EasingFunction` gains `SmoothStep` and `SmootherStep`. `EasingFunctionExtensions` adds `curve.Ease(t)` and `curve.Interpolate(start, end, t)`.
- `ShapeBatch` space polylines: `DrawPolyline` and `DrawPixelPolyline` take 3D points, in world or pixel width, with glow, dashes, opacity and per-fragment depth.
- `ShapeBatch.DepthFade`: fades a shape out over a world distance as it approaches scene geometry.
- `ShapeBatch` textured fill: `FillSource` takes any material `IComputeColor` node. `FillWith(texture)` covers the common case.
- `ShapeBatch.Screen`: draws shapes in window pixels from the same batch as world shapes. `Corner`, `ScreenSize` and `Viewport` place them.
- `ShapeComponent`, `WorldTextComponent` and `EntityTextComponent` draw in Game Studio's viewport and register their renderers themselves. The `AddShapeBatch`, `AddWorldTextRenderer` and `AddEntityTextRenderer` calls are now optional.
- `GrabberScript` and `Grabber2DScript` appear in Game Studio under Physics. `game.AddGrabber()` adds the Bepu grabber from code.
- `Box2DBodyComponent` can be added in Game Studio.

### 🐞 Bug Fixes

- Metals built in code rendered black. The material helpers now compile with the game's content manager, so the engine's environment lookup texture resolves.
- `ShapeBatch`: dashes on a ring seen in perspective stay in step, and dash length along lines follows foreshortening.
- `UseGameSettings` can raise the graphics profile of a code-only game.
- ImGui, ImGui.NET and DebugShapes decode their colours to linear for the sRGB backbuffer. DebugShapes no longer turns some colours into NaN.
- Text and debug renderers no longer throw when the compositor has no camera slot. Text renderers fall back to a system font when the built-in font is missing.
- `E04_ImGuiNet`: overlay margins follow the display scale.
- `E05_3D_Car`: `R` resets every body of the car, bodies interpolate between physics steps, the steering lock is 54 degrees, and the car waits for `W`.
- `E02_3D_EasingInGame`: the camera flight returns control to the camera controller when it lands.
- `E09_3D_Particles_Gallery`: fire textures were loaded with red and blue swapped.

### 🎨 Rendering

- `ShapeBatch` picking: `Tag` marks the shapes drawn while it is set, `TryPick` returns the topmost tagged shape under a screen position and `PickAll` returns every hit. A `Ray` overload is available. `ShapeHit` carries the tag, world point, local point, signed distance and depth. `ShapeComponent.Pickable` tags a component.
- `ShapeGlow.Strength` sets the strength of a glow in the outline colour, 0 to 1. `ShapeGlow.Additive` makes a glow add light.
- `ShapeBatch` reads shape records from a structured buffer and keeps points in their own buffer. A dither hides banding in glows.
- Batches share buffers per frame. `RemoveShapeBatch` removes a batch.
- `game.AddShapeBatch(afterPostEffects: true)` draws a batch after the post-processing chain, so its colours are not tone mapped.
- `ShapeBatch.WorldPerPixel(point)` returns how much world one pixel covers at a point.

### ✨ Enhancement

- `game.SetWindowSize(width, height)` sets the initial window size before `Run`.
- `game.SetDeterministic(step)` fixes the timestep with one update per draw, for replays, tests and captures.
- `TextElement` carries keys as data: `new("H", "Reset camera")`, `new(["Q", "E"], "Ascend / descend")`. `DebugOverlay` formats them through `KeyFormat`, `KeySeparator` and `KeyColor`.
- `DebugOverlay`: draws last in the frame, `SectionGap` sets the gap between sections, `SetPosition` places the overlay by pixel or corner, and `BlockBounds` reports where it was drawn. Collapsible titles read `[+] [F2] Camera controls`. Text is drawn on whole pixels.
- All examples use one layout for on-screen help: one key per line, keys first, status below.
- Custom renderers run inside GPU timing scopes and show by name in the profiler.
- `AddShapeBatch` registers the batch before the first frame.
- `SceneRendererRegistration` and `CompositorCameras` in `Rendering.Compositing`, for component libraries that should work in Game Studio.
- Toolkit scripts have display names and categories in Game Studio's Add-component list.

### 📄 Docs

- New manual pages: [Materials from code](../manual/rendering/materials.md), [render to texture](../manual/rendering/render-to-texture.md), [GPU picking](../manual/rendering/gpu-picking.md) and a [glossary](../manual/glossary.md) of about 140 terms.
- New contributing pages: [Shaders in a toolkit package](../contributing/toolkit/shaders.md) and [Making components work in Game Studio](../contributing/toolkit/game-studio-components.md).
- [Using toolkit components in Game Studio](../manual/game-studio.md) rewritten.
- ShapeBatch manual: colour space, blend state, glow alpha, space strokes, screen shapes and limits.
- The contributing build page covers testing packages on Linux from WSL and the clean-up scripts.
- Example pages say when an example needs a package that is not on NuGet yet.
- Example screenshots are named after the example slug.
- API docs include the Box2D and Effects packages.

### 🎓 Examples

- **Material Gallery** (`E02_3D_Material_Gallery`, new): the material system on 33 stations, all from code. Covers the PBR numbers, maps, node inputs, custom shader nodes and features, texture loading, runtime parameters and material swaps, transparency, glass, clear coat, cel shading, displacement, tessellation, layers, the highlight shell and Game Studio's Material Package in C#. The hair and subsurface scattering stations need an engine fix and are enabled with `--engine-fix`.
- **Material Preview** (`E02_3D_MaterialPreview`, new): Game Studio's material thumbnail rig in code. `G` switches to an in-game look with post effects and a skybox.
- **Particle Gallery** (`E09_3D_Particles_Gallery`, rewritten): 31 particle systems from code, from single building blocks to a campfire, fireworks, a tornado and a rocket engine.
- **ShapeBatch Gallery** (`E11_3D_ShapeBatch_Gallery`): one ShapeBatch feature per station, each a static method that can be copied into a game.
- **Easing Basics** (`E02_2D_EasingBasics`, new): the same motion written four ways, from a hand-written formula to a `Tween`.
- **Easing Cheat Sheet** (`E02_2D_Easing`, new): every easing curve on one screen, in a new Mathematics category.
- **Easing in a 3D Game** and **Easing in a 2D Game** (`E02_3D_EasingInGame`, `E02_2D_EasingInGame`, new): tweens working next to Bepu physics.
- **Render to Texture** (`E09_3D_RenderToTexture`, new): five cameras on monitors, including night vision and thermal.
- **GPU Picking** (`E09_3D_GpuPicking`, new): hover and select meshes and instances without colliders.
- **Compute Boids** (`E10_3D_ComputeBoids`, new): a flock steered by a compute shader and drawn as an instanced mesh.
- `E01_3D_BasicScene_EngineOnly` and `E01_3D_BasicScene_FileBasedApp/ProgramEngineOnly.cs` (new): a basic scene in Stride alone, without the toolkit.
- `E02_3D_Material` rebuilt around glossiness and metalness sweeps.
- Gallery projects renamed: `E02_3D_Material_Gallery`, `E09_3D_Particles_Gallery` and `E11_3D_ShapeBatch_Gallery`. Docs slugs are unchanged.
- The gallery frame moved to `Example.Common` as `Gallery<TStation>`: ring, labels, index board, camera flights and clickable pads. A station that throws leaves an empty pad with its message. `--station N --variation M` starts at a station.
- `E03_2D_Panels` grew to 24 stations and highlights the panel under the mouse. The HUD and SignalR examples use strokes and additive glows.
- `E06_Box2D`: shapes glow in their border colour. The rarer keys moved from `F1` to `Z`.
- Five Bepu examples use `game.AddGrabber()`. `E05_3D_Constraints_Simple` and `E08_2D_DebugRender` gained an overlay.
- Examples that load image files use `TextureLoader`.

### 🔧 Engineering

- Gold-image regression tests: `build/gold-images.cs` renders twelve scenes from `tests/Stride.CommunityToolkit.GoldScenes` on the WARP software renderer and compares them with `tests/gold`. The `gold-images.yml` workflow runs on pull requests that touch a renderer.
- New unit tests for easing, tweens, material descriptors and parameters, texture pixel operations, the highlight shell, stream views and ShapeBatch picking.
- Smaller build output: `build/HostRuntime.props` and `build/HostRuntime.targets` limit examples, tests and tools to the host platform. The test project's `bin` went from 495 MB to 89 MB and the launcher's from 561 MB to 28 MB.
- `delete-bin.bat` and `delete-bin-examples.bat` remove `bin` and `obj` folders.
- `build/pack-local.cs` writes `bin/packages/NuGet.wsl.config` for building against local packages inside WSL.
- The metadata generator writes CRLF without a final newline and rewrites only pages whose content changed.
- DPI awareness: examples call `WindowsDpiManager.EnablePerMonitorV2()`. Only `E08_DpiAware` keeps an `app.manifest`.
- NDepend reports zero issues across the solution.
- Audio playback tests skip on machines without an audio device, such as CI runners.
- GitHub code-quality findings triaged. `Tween` rejects a NaN or infinite duration, the Box2D task scheduler locks a private object, and gallery variations run through the station guard.
- Known issue: thin glass renders opaque on Stride 4.4 because of an engine bug. The material gallery restores the blend state after the material is generated.
- Known issue: after changing `StrideVersion`, delete an example's `obj/stride`, `obj/Debug/net10.0/stride` and `bin/.../data` folders, or shaders from the previous package are reused.

### 💪 Other Changes

- Examples' manifest and doc pages regenerated.

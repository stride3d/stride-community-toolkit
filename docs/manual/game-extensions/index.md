# Game Extensions

Extension methods for `Game` and `IGame`. They cover starting the game loop, assembling a scene from nothing, and the small conveniences - frame rate, materials, screenshots - that you would otherwise write once per project.

They all live in `Stride.CommunityToolkit.Engine`, so one `using` covers everything on `game.`. Some take `Game` and some take `IGame`; at a call site the difference does not show, because a `Game` is an `IGame`.

> [!TIP]
> If you are working code-only, [Code-Only Extensions](../code-only/extensions.md) is the shorter,
> curated list - the subset you actually reach for when there is no editor to hand you a scene.

## Starting the game

- [`Run()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Run(Stride.Engine.Game,System.Action{Stride.Engine.Scene},System.Action{Stride.Engine.Scene,Stride.Games.GameTime},Stride.Games.GameContext)) - Starts the game loop. `start` runs once the root scene exists, `update` runs every frame after it.
- [`Run()` with async `start`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Run(Stride.Engine.Game,System.Func{Stride.Engine.Scene,System.Threading.Tasks.Task},System.Action{Stride.Engine.Scene,Stride.Games.GameTime},Stride.Games.GameContext)) - The same, with a `start` that can `await` between steps. `update` begins only once that task completes.
- [`Exit()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Exit(Stride.Games.IGame)) - Closes the game from an `IGame` reference.
- [`SetWindowSize()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.SetWindowSize(Stride.Engine.Game,System.Int32,System.Int32)) - The size the window opens at, before `Run`; the engine's default is 1280 by 720.
- [`UseGameSettings()`](xref:Stride.CommunityToolkit.Engine.GameSettingsExtensions.UseGameSettings(Stride.Engine.Game,System.Action{Stride.Engine.Design.GameSettings})) - What a Game Studio project gets from its `GameSettings` asset, built in code and applied the same way: the configurations the audio, physics and navigation systems read (HRTF, physics defaults, navigation build settings, Bepu simulations), rendering settings (graphics profile, back buffer, colour space) and the shader compilation mode. Call it before `Run()`. Safe in a project that has the asset too: the asset wins, and the call only adds the configurations it lacks.

> [!NOTE]
> On the compilation mode, measured rather than assumed: on Direct3D 11 `Debug` (the engine's default for a game without settings) and `Release` produce identical shader bytecode, because the compiler only applies the optimisation level when debug information is off. `AppStore` is the one that differs - optimisation level 2 with the symbols stripped, so RenderDoc no longer shows shader source. Vulkan and Direct3D 12 ignore the mode. Bytecode is cached per mode, so a change means one full recompile on the next run.

## Scene setup shortcuts

One call instead of assembling the compositor, camera and lighting by hand.

- [`SetupBase2D()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.SetupBase2D(Stride.Engine.Game,System.Nullable{Stride.Core.Mathematics.Color})) - Adds a graphics compositor and a 2D camera.
- [`SetupBase3D()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.SetupBase3D(Stride.Engine.Game)) - Adds a graphics compositor, a 3D camera and a directional light.

The physics packages extend these into a full playable scene - see [Physics Extensions](../physics-extensions/index.md) for `SetupBase2DScene()` and `SetupBase3DScene()`.

## Graphics compositor

Nothing renders without a compositor. These add one, or hang extra renderers off the one you have.

- [`AddGraphicsCompositor()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddGraphicsCompositor(Stride.Engine.Game,System.Nullable{Stride.Core.Mathematics.Color},Stride.Graphics.MultisampleCount)) - Adds the default compositor, with post-processing enabled and an optional clear colour.
- [`Add2DGraphicsCompositor()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Add2DGraphicsCompositor(Stride.Engine.Game,System.Nullable{Stride.Core.Mathematics.Color},Stride.Graphics.MultisampleCount)) - The 2D configuration, without post-processing.
- [`AddSceneRenderer()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddSceneRenderer(Stride.Engine.Game,Stride.Rendering.Compositing.SceneRendererBase)) - Appends your own `SceneRendererBase` to the compositor's render chain.
- [`AddRootRenderFeature()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddRootRenderFeature(Stride.Engine.Game,Stride.Rendering.RootRenderFeature)) - Registers a `RootRenderFeature`, for drawing a component type the engine does not know about.
- [`AddParticleRenderer()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddParticleRenderer(Stride.Engine.Game)) - Adds the particle stages and features, which the default code-only compositor leaves out.
- [`ConfigurePostEffects()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.ConfigurePostEffects(Stride.Engine.Game,System.Action{Stride.Rendering.Images.PostProcessingEffects})) - Switches post effects on and tunes them. The default compositor has bloom, ambient occlusion, depth of field, screen-space reflections, fog, outline and FXAA all present but *disabled*, and only tone mapping in its colour transforms; vignette, film grain and dither are not there at all until added to `ColorTransforms.Transforms`. See the [Post Effects](../code-only/examples/post-effects.md) example.
- [`GetPostEffects()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.GetPostEffects(Stride.Engine.Game)) - The live `PostProcessingEffects`, or `null` for a compositor without any (the 2D one), for toggling an effect at runtime.

## Cameras

- [`Add2DCamera()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Add2DCamera(Stride.Engine.Game,System.String,System.Nullable{Stride.Core.Mathematics.Vector3},System.Nullable{Stride.Core.Mathematics.Vector3})) - Adds a 2D camera and binds it to the compositor's first camera slot.
- [`Add3DCamera()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Add3DCamera(Stride.Engine.Game,System.String,System.Nullable{Stride.Core.Mathematics.Vector3},System.Nullable{Stride.Core.Mathematics.Vector3},Stride.Engine.Processors.CameraProjectionMode)) - Adds a 3D camera, perspective by default.
- [`Add2DCameraController()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Add2DCameraController(Stride.Engine.Game,System.String,Stride.Input.Keys,System.Boolean)) - Makes the 2D camera pannable and zoomable, with an on-screen key reminder toggled by <kbd>F2</kbd>.
- [`Add3DCameraController()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Add3DCameraController(Stride.Engine.Game,System.Nullable{Stride.CommunityToolkit.Rendering.Text.DisplayPosition},System.String,Stride.Input.Keys,System.Boolean)) - Adds free-look movement. The <kbd>F2</kbd> panel also prints the camera's live position and rotation, which is the quickest way to find numbers worth hard-coding.

See [Camera Controllers](../camera-extensions/camera-controllers.md) for the keys, the on-screen help and every option on both scripts.
- [`SetCameraPosition()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.SetCameraPosition(Stride.Engine.Game,Stride.Core.Mathematics.Vector3,System.String)) - Moves the existing camera. Use this after a `SetupBase*` call rather than adding a second camera that competes for the same slot.
- [`SetCameraRotation()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.SetCameraRotation(Stride.Engine.Game,Stride.Core.Mathematics.Vector3,System.String)) - Aims the existing camera, taking yaw, pitch and roll in degrees.
- [`GetCameraEntity()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.GetCameraEntity(Stride.Engine.Game,System.String)) - The camera entity itself, for everything the two setters do not cover - reading the transform, attaching a script, replacing the camera outright.

## Lighting

- [`AddDirectionalLight()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddDirectionalLight(Stride.Engine.Game,System.String,System.Boolean,System.Single)) - Adds a single directional light, with shadows on by default.
- [`AddAllDirectionLighting()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddAllDirectionLighting(Stride.Engine.Game,System.Single,System.Boolean)) - Lights the scene from every direction at once, so nothing is left in shadow. Blunt, but it makes a debug scene readable.
- [`AddStudioLighting()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddStudioLighting(Stride.Engine.Game,System.Single,System.Single,System.Boolean,System.Boolean)) - A three-point key, fill and rim rig aimed at the scene centre, returned as a tuple so you can adjust each light afterwards.

For image-based ambient light, `AddSkybox()` ships in the `Stride.CommunityToolkit.Skyboxes` package - see [Code-Only Extensions](../code-only/extensions.md).

## Materials and primitives

- [`CreateMaterial()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.CreateMaterial(Stride.Games.IGame,System.Nullable{Stride.Core.Mathematics.Color},System.Single,System.Single)) - A basic lit material, with optional colour, metalness and glossiness values.
- [`CreateFlatMaterial()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.CreateFlatMaterial(Stride.Games.IGame,System.Nullable{Stride.Core.Mathematics.Color})) - An emissive material unaffected by lighting, which is what you want for 2D and for anything that must stay readable regardless of where the lights are.
- [`CreateEmissiveMaterial()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.CreateEmissiveMaterial(Stride.Games.IGame,Stride.Core.Mathematics.Color,System.Single)) - A surface that gives off its own light: a lamp, a sign, a glowing edge. Above an intensity of 1 it blooms under post effects.
- [`CreateTexturedMaterial()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.CreateTexturedMaterial(Stride.Games.IGame,Stride.Graphics.Texture,System.Single,System.Single,System.Single)) - A texture where the colour would be, tiled, with the same metalness and glossiness numbers.
- [`CreateScreenMaterial()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.CreateScreenMaterial(Stride.Games.IGame,Stride.Graphics.Texture,System.Single)) - A screen showing a texture, unlit and clamped: a monitor for a render-texture camera's feed.
- [`CreateMaterial(descriptor)`](xref:Stride.CommunityToolkit.Engine.GameExtensions.CreateMaterial(Stride.Games.IGame,Stride.Rendering.Materials.MaterialDescriptor)) - Compiles a descriptor and keeps it on the material, so it can be a layer later. The descriptors behind every helper are in [`MaterialDescriptors`](xref:Stride.CommunityToolkit.Rendering.MaterialDescriptors): take one, add a feature, compile it.
- [`Create3DPrimitive()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Create3DPrimitive(Stride.Games.IGame,Stride.CommunityToolkit.Rendering.ProceduralModels.PrimitiveModelType,Stride.CommunityToolkit.Engine.Primitive3DEntityOptions)) - A cube, sphere, capsule and so on as an entity, with no collider attached.
- [`Create2DPrimitive()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Create2DPrimitive(Stride.Games.IGame,Stride.CommunityToolkit.Rendering.ProceduralModels.Primitive2DModelType,Stride.CommunityToolkit.Engine.Primitive2DEntityOptions)) - The 2D equivalent - square, circle, triangle, polygon.

> [!NOTE]
> The Bepu and Bullet packages each define their own `Create2DPrimitive()` and `Create3DPrimitive()`
> that also attach a collider. If you have imported one of those namespaces, that overload is the one
> you get. The two above are the physics-free versions.

## Text renderers

Both register their renderer once, however many times you call them.

- [`AddEntityTextRenderer()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddEntityTextRenderer(Stride.Engine.Game)) - Puts the renderer for `EntityTextComponent`, screen-space text drawn over the scene, on the compositor before the first frame; the component registers it itself when missing. See [Entity Text](../rendering/entity-text.md).
- [`AddWorldTextRenderer()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddWorldTextRenderer(Stride.Engine.Game)) - Puts the renderer for `WorldTextComponent`, text that lives in the 3D scene, on the compositor before the first frame; the component registers it itself when missing. See [World Text](../rendering/world-text.md).

## Debugging and diagnostics

- [`AddProfiler()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddProfiler(Stride.Engine.Game,System.String)) - Adds Stride's profiler, toggled with <kbd>Left Shift</kbd> + <kbd>Left Ctrl</kbd> + <kbd>P</kbd>.
- [`SetDeterministic()`](xref:Stride.CommunityToolkit.Engine.GameLoopExtensions.SetDeterministic(Stride.Engine.Game,System.Nullable{System.TimeSpan})) - Pins the loop so frame N is the same simulated instant on every run: fixed timestep, draws in step, one update per draw. For replays, lockstep, tests and screenshots.
- [`AddGroundGizmo()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddGroundGizmo(Stride.Engine.Game,System.Nullable{Stride.Core.Mathematics.Vector3},System.Boolean,System.Boolean)) - Draws the world axes at the origin, optionally labelled. The quickest way to work out which way you are facing.
- [`AddEntityDebugSceneRenderer()`](xref:Stride.CommunityToolkit.Renderers.GraphicsCompositorExtensions.AddEntityDebugSceneRenderer(Stride.Engine.Game,Stride.CommunityToolkit.Renderers.EntityDebugSceneRendererOptions)) - Draws entity names and positions over the scene. In `Stride.CommunityToolkit.Renderers`.
- [`TakeScreenShot()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.TakeScreenShot(Stride.Games.IGame,System.String,Stride.Graphics.ImageFileType)) - Saves the current frame to a file. This is what the toolkit's own screenshot capture is built on.

To see colliders, use `ShowColliders()` in [Physics Extensions](../physics-extensions/index.md) for Bullet, or Bepu's own debug rendering.

## Frame rate

These take `IGame`, since they need nothing from the scene.

- [`DeltaTime()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.DeltaTime(Stride.Games.IGame)) - Seconds since the last update, as a `float`.
- [`DeltaTimeAccurate()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.DeltaTimeAccurate(Stride.Games.IGame)) - The same as a `double`, for accumulating over long runs.
- [`FPS()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.FPS(Stride.Games.IGame)) - The current frames per second.
- [`SetMaxFPS()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.SetMaxFPS(Stride.Games.IGame,System.Int32)) - Caps the frame rate.
- [`SetFocusLostFPS()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.SetFocusLostFPS(Stride.Games.IGame,System.Int32)) - Caps it separately for when the window is in the background, so a minimised game stops burning a core.
- [`EnableVSync()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.EnableVSync(Stride.Games.IGame)) / [`DisableVSync()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.DisableVSync(Stride.Games.IGame)) - Turns vertical sync on or off at runtime.
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

## Scene setup

The compositor, camera, lighting, sky and ground of a code-only scene as one object that `Run` applies before `start`.

- [`SceneSetup3D`](xref:Stride.CommunityToolkit.Engine.SceneSetup3D) and [`SceneSetup2D`](xref:Stride.CommunityToolkit.Engine.SceneSetup2D) - Every piece a property with a default; `null` leaves a piece out. `game.Run(start:)` with no setup applies `SceneSetup3D.Default`.
- [`Run(setup, start, update)`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Run(Stride.Engine.Game,Stride.CommunityToolkit.Engine.SceneSetup,System.Action{Stride.Engine.Scene},System.Action{Stride.Engine.Scene,Stride.Games.GameTime},Stride.Games.GameContext)) - Applies the setup and runs the game.

The physics packages choose the engine with `UseBepu()` and `UseBullet()` - see [Physics Extensions](../physics-extensions/index.md); the pieces one by one are under [Code-Only Extensions](../code-only/extensions.md).

## Graphics compositor

Nothing renders without a compositor. These add one, or hang extra renderers off the one you have.

- [`AddGraphicsCompositor()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddGraphicsCompositor(Stride.Games.IGame,System.Nullable{Stride.Core.Mathematics.Color},Stride.Graphics.MultisampleCount)) - Adds the default compositor, with post-processing enabled and an optional clear colour.
- [`Add2DGraphicsCompositor()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Add2DGraphicsCompositor(Stride.Games.IGame,System.Nullable{Stride.Core.Mathematics.Color},Stride.Graphics.MultisampleCount)) - The 2D configuration, without post-processing.
- [`AddSceneRenderer()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddSceneRenderer(Stride.Games.IGame,Stride.Rendering.Compositing.SceneRendererBase)) - Appends your own `SceneRendererBase` to the compositor's render chain.
- [`AddRootRenderFeature()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddRootRenderFeature(Stride.Games.IGame,Stride.Rendering.RootRenderFeature)) - Registers a `RootRenderFeature`, for drawing a component type the engine does not know about.
- [`AddParticleRenderer()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddParticleRenderer(Stride.Games.IGame)) - Adds the particle stages and features, which the default code-only compositor leaves out.
- [`ConfigurePostEffects()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.ConfigurePostEffects(Stride.Games.IGame,System.Action{Stride.Rendering.Images.PostProcessingEffects})) - Switches post effects on and tunes them. The default compositor has bloom, ambient occlusion, depth of field, screen-space reflections, fog, outline and FXAA all present but *disabled*, and only tone mapping in its colour transforms; vignette, film grain and dither are not there at all until added to `ColorTransforms.Transforms`. See the [Post Effects](../code-only/examples/post-effects.md) example.
- [`GetPostEffects()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.GetPostEffects(Stride.Games.IGame)) - The live `PostProcessingEffects`, or `null` for a compositor without any (the 2D one), for toggling an effect at runtime.

## Cameras

- [`Add2DCamera()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Add2DCamera(Stride.Games.IGame,System.String,System.Nullable{Stride.Core.Mathematics.Vector3},System.Nullable{Stride.Core.Mathematics.Vector3})) - Adds a 2D camera and binds it to the compositor's first camera slot.
- [`Add3DCamera()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Add3DCamera(Stride.Games.IGame,System.String,System.Nullable{Stride.Core.Mathematics.Vector3},System.Nullable{Stride.Core.Mathematics.Vector3},Stride.Engine.Processors.CameraProjectionMode)) - Adds a 3D camera, perspective by default.
- [`Add2DCameraController()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Add2DCameraController(Stride.Games.IGame,System.String,Stride.Input.Keys,System.Boolean)) - Makes the 2D camera pannable and zoomable, with an on-screen key reminder toggled by <kbd>F2</kbd>.
- [`Add3DCameraController()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Add3DCameraController(Stride.Games.IGame,System.Nullable{Stride.CommunityToolkit.Rendering.ScreenAnchor},System.String,Stride.Input.Keys,System.Boolean,System.Boolean)) - Adds free-look movement. The <kbd>F2</kbd> panel also prints the camera's live position and rotation, which is the quickest way to find numbers worth hard-coding.

- [`SetCameraPosition()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.SetCameraPosition(Stride.Games.IGame,Stride.Core.Mathematics.Vector3,System.String)) - Moves the existing camera. Use this after `Run(setup, ...)` rather than adding a second camera that competes for the same slot.
- [`SetCameraRotation()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.SetCameraRotation(Stride.Games.IGame,Stride.Core.Mathematics.Vector3,System.String)) - Aims the existing camera, taking yaw, pitch and roll in degrees.
- [`GetCameraEntity()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.GetCameraEntity(Stride.Games.IGame,System.String)) - The camera entity itself, for everything the two setters do not cover - reading the transform, attaching a script, replacing the camera outright.

See [Camera Controllers](../camera-extensions/camera-controllers.md) for the keys, the on-screen help and every option on both scripts.

## Lighting

- [`AddDirectionalLight()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddDirectionalLight(Stride.Games.IGame,System.String,System.Boolean,System.Single)) - Adds a single directional light, with shadows on by default.
- [`AddAllDirectionLighting()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddAllDirectionLighting(Stride.Games.IGame,System.Single,System.Boolean)) - Lights the scene from every direction at once, so nothing is left in shadow. Blunt, but it makes a debug scene readable.
- [`AddStudioLighting()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddStudioLighting(Stride.Games.IGame,System.Single,System.Single,System.Boolean,System.Boolean)) - A three-point key, fill and rim rig aimed at the scene centre, returned as a tuple so you can adjust each light afterwards.

The setup's gradient sky lights the scene from above; for a photographed sky, `UseSkybox()` and `AddSkybox()` ship in the `Stride.CommunityToolkit.Skyboxes` package - see [Code-Only Extensions](../code-only/extensions.md).

## Materials and primitives

- [`CreateMaterial()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.CreateMaterial(Stride.Games.IGame,System.Nullable{Stride.Core.Mathematics.Color},System.Single,System.Single)) - A basic lit material, with optional colour, metalness and glossiness values.
- [`CreateFlatMaterial()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.CreateFlatMaterial(Stride.Games.IGame,System.Nullable{Stride.Core.Mathematics.Color})) - An emissive material unaffected by lighting, which is what you want for 2D and for anything that must stay readable regardless of where the lights are.
- [`CreateEmissiveMaterial()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.CreateEmissiveMaterial(Stride.Games.IGame,Stride.Core.Mathematics.Color,System.Single)) - A surface that gives off its own light: a lamp, a sign, a glowing edge. Above an intensity of 1 it blooms under post effects.
- [`CreateTexturedMaterial()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.CreateTexturedMaterial(Stride.Games.IGame,Stride.Graphics.Texture,System.Single,System.Single,System.Single)) - A texture where the colour would be, tiled, with the same metalness and glossiness numbers.
- [`CreateScreenMaterial()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.CreateScreenMaterial(Stride.Games.IGame,Stride.Graphics.Texture,System.Single)) - A screen showing a texture, unlit and clamped: a monitor for a render-texture camera's feed.
- [`CreateMaterial(descriptor)`](xref:Stride.CommunityToolkit.Engine.GameExtensions.CreateMaterial(Stride.Games.IGame,Stride.Rendering.Materials.MaterialDescriptor)) - Compiles a descriptor and keeps it on the material, so it can be a layer later. The descriptors behind every helper are in [`MaterialDescriptors`](xref:Stride.CommunityToolkit.Rendering.MaterialDescriptors): take one, add a feature, compile it.
- [`CreatePrimitive(Primitive3D)`](xref:Stride.CommunityToolkit.Engine.GameExtensions.CreatePrimitive(Stride.Games.IGame,Stride.CommunityToolkit.Rendering.ProceduralModels.Primitive3D,Stride.CommunityToolkit.Engine.Primitive3DEntityOptions)) - A cube, sphere, capsule and so on as an entity. The shape comes from a [`Primitive3D`](xref:Stride.CommunityToolkit.Rendering.ProceduralModels.Primitive3D) factory with its dimensions: `Primitive3D.Sphere(0.5f)`, `Primitive3D.Box(new Vector3(2, 1, 1))`.
- [`CreatePrimitive(Primitive2D)`](xref:Stride.CommunityToolkit.Engine.GameExtensions.CreatePrimitive(Stride.Games.IGame,Stride.CommunityToolkit.Rendering.ProceduralModels.Primitive2D,Stride.CommunityToolkit.Engine.Primitive2DEntityOptions)) - The 2D equivalent - square, circle, triangle, polygon - from a [`Primitive2D`](xref:Stride.CommunityToolkit.Rendering.ProceduralModels.Primitive2D) factory.

> [!NOTE]
> With `UseBepu()` or `UseBullet()` on the scene setup, both attach that engine's collider as well;
> `IncludePhysics = false` on the options leaves it out. See [Physics Extensions](../physics-extensions/index.md).

## Text renderers

Both register their renderer once, however many times you call them.

- [`AddEntityTextRenderer()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddEntityTextRenderer(Stride.Games.IGame)) - Puts the renderer for `EntityTextComponent`, screen-space text drawn over the scene, on the compositor before the first frame; the component registers it itself when missing. See [Entity Text](../rendering/entity-text.md).
- [`AddWorldTextRenderer()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddWorldTextRenderer(Stride.Games.IGame)) - Puts the renderer for `WorldTextComponent`, text that lives in the 3D scene, on the compositor before the first frame; the component registers it itself when missing. See [World Text](../rendering/world-text.md).

## Debugging and diagnostics

- [`AddProfiler()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddProfiler(Stride.Games.IGame,System.String)) - Adds Stride's profiler, toggled with <kbd>Left Shift</kbd> + <kbd>Left Ctrl</kbd> + <kbd>P</kbd>.
- [`SetDeterministic()`](xref:Stride.CommunityToolkit.Engine.GameLoopExtensions.SetDeterministic(Stride.Games.IGame,System.Nullable{System.TimeSpan})) - Pins the loop so frame N is the same simulated instant on every run: fixed timestep, draws in step, one update per draw. For replays, lockstep, tests and screenshots.
- [`AddGroundGizmo()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddGroundGizmo(Stride.Games.IGame,System.Nullable{Stride.Core.Mathematics.Vector3},System.Boolean,System.Boolean)) - Draws the world axes at the origin, optionally labelled. Marks the origin and a unit in the scene.
- [`AddOrientationGizmo()`](xref:Stride.CommunityToolkit.Rendering.Gizmos.OrientationGizmoExtensions.AddOrientationGizmo(Stride.Games.IGame,Stride.CommunityToolkit.Rendering.ScreenAnchor)) - Draws the world axes in a corner of the screen, turning with the camera. The quickest way to work out which way you are facing; see [Orientation Gizmo](../rendering/orientation-gizmo.md).
- [`AddEntityInfoRenderer()`](xref:Stride.CommunityToolkit.Rendering.Overlay.EntityInfoRendererExtensions.AddEntityInfoRenderer(Stride.Games.IGame,Stride.CommunityToolkit.Rendering.Overlay.EntityInfoRendererOptions)) - Draws entity names and positions over the scene. In `Stride.CommunityToolkit.Rendering.Text`.
- [`TakeScreenShot()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.TakeScreenShot(Stride.Games.IGame,System.String,Stride.Graphics.ImageFileType)) - Saves the current frame to a file. This is what the toolkit's own screenshot capture is built on.

To see colliders, use `AddPhysicsDebug()` for Bepu or `ShowColliders()` for Bullet, both in [Physics Extensions](../physics-extensions/index.md).

## Frame rate

These take `IGame`, since they need nothing from the scene.

- [`DeltaTime()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.DeltaTime(Stride.Games.IGame)) - Seconds since the last update, as a `float`.
- [`DeltaTimeAccurate()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.DeltaTimeAccurate(Stride.Games.IGame)) - The same as a `double`, for accumulating over long runs.
- [`FPS()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.FPS(Stride.Games.IGame)) - The current frames per second.
- [`SetMaxFPS()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.SetMaxFPS(Stride.Games.IGame,System.Int32)) - Caps the frame rate; `0` removes the cap.
- [`SetFocusLostFPS()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.SetFocusLostFPS(Stride.Games.IGame,System.Int32)) - Caps it separately for when the window is in the background, so a minimised game stops burning a core.
- [`EnableVSync()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.EnableVSync(Stride.Games.IGame)) / [`DisableVSync()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.DisableVSync(Stride.Games.IGame)) - Turns vertical sync on or off at runtime.
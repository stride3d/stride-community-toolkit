# Extensions

Extensions primarily useful for **code-only** projects.

In a Game Studio project the editor hands you a scene, a camera, lighting and a graphics compositor before you write a line of code. Code-only starts from nothing, so these methods build the same pieces from C#. They are ordinary extension methods - a Game Studio project can call any of them - but this is the set you are most likely to reach for when there is no editor.

Extensions that apply equally either way live under the [Extensions](../game-extensions/index.md) section of the manual: [Animation](../animation-extensions/index.md), [Camera](../camera-extensions/index.md), [Entity](../entity-extensions/index.md), [Game](../game-extensions/index.md), [Model](../model-extensions/index.md), [Physics](../physics-extensions/index.md), [Script](../script-extensions/index.md) and [Script System](../script-system-extensions/index.md).

Feel free to inspect and modify the source to adapt behaviour or create your own variants. These are starting points - use them as-is or customise as needed.

## Starting the game

- [`Run()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Run(Stride.Engine.Game,System.Action{Stride.Engine.Scene},System.Action{Stride.Engine.Scene,Stride.Games.GameTime},Stride.Games.GameContext)) - Starts the game loop. `start` runs once the root scene exists, `update` runs every frame after it.
- [`Run()` with async `start`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Run(Stride.Engine.Game,System.Func{Stride.Engine.Scene,System.Threading.Tasks.Task},System.Action{Stride.Engine.Scene,Stride.Games.GameTime},Stride.Games.GameContext)) - The same, with a `start` that can `await` between steps. `update` begins only once that task completes.
- [`UseGameSettings()`](xref:Stride.CommunityToolkit.Engine.GameSettingsExtensions.UseGameSettings(Stride.Engine.Game,System.Action{Stride.Engine.Design.GameSettings})) - The `GameSettings` asset a code-only project does not have, built in code: HRTF, physics and navigation defaults, Bepu simulations, rendering settings, shader compilation mode. Before `Run()`. Only needed when one of those must change - without it every subsystem runs on the same defaults a new Game Studio project ships with. In a project that does have the asset, the asset wins and the call only adds the configurations the asset lacks.

## Setting up a scene

The `SetupBase*` methods are the shortcut: one call instead of assembling the pieces below by hand.

- [`SetupBase2D()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.SetupBase2D(Stride.Engine.Game,System.Nullable{Stride.Core.Mathematics.Color})) - Adds a graphics compositor and a 2D camera.
- [`SetupBase3D()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.SetupBase3D(Stride.Engine.Game)) - Adds a graphics compositor, a 3D camera and a directional light.
- [`SetupBase2DScene()`](xref:Stride.CommunityToolkit.Bepu.GameExtensions.SetupBase2DScene(Stride.Engine.Game)) - `SetupBase2D()` plus a skybox, 2D ground and a camera controller, so the scene is immediately visible and navigable.
- [`SetupBase3DScene()`](xref:Stride.CommunityToolkit.Bepu.GameExtensions.SetupBase3DScene(Stride.Engine.Game)) - `SetupBase3D()` plus a skybox, 3D ground and a camera controller.

## Building a scene piece by piece

- [`AddGraphicsCompositor()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddGraphicsCompositor(Stride.Engine.Game,System.Nullable{Stride.Core.Mathematics.Color},Stride.Graphics.MultisampleCount)) - Adds a default `GraphicsCompositor`. Nothing renders without one.
- [`AddDirectionalLight()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddDirectionalLight(Stride.Engine.Game,System.String,System.Boolean,System.Single)) - Adds a directional light, with shadows on by default.
- [`AddSkybox()`](xref:Stride.CommunityToolkit.Skyboxes.GameExtensions.AddSkybox(Stride.Engine.Game,System.String)) - Adds a skybox, which also provides ambient lighting.
- [`Add2DGround()`](xref:Stride.CommunityToolkit.Bepu.GameExtensions.Add2DGround(Stride.Engine.Game,Stride.CommunityToolkit.Bepu.Bepu2DPhysicsOptions)) - Adds a static 2D ground collider.
- [`Add3DGround()`](xref:Stride.CommunityToolkit.Bepu.GameExtensions.Add3DGround(Stride.Engine.Game,Stride.CommunityToolkit.Bepu.Bepu3DPhysicsOptions)) - Adds a static 3D ground plane.
- [`AddInfinite3DGround()`](xref:Stride.CommunityToolkit.Bullet.GameExtensions.AddInfinite3DGround(Stride.Engine.Game,Stride.CommunityToolkit.Bullet.Bullet3DPhysicsOptions)) - Adds a ground plane that nothing can fall off. Bullet only.

> [!NOTE]
> The scene and ground methods come in a Bepu and a Bullet flavour, in `Stride.CommunityToolkit.Bepu` and `Stride.CommunityToolkit.Bullet`. The links above point at Bepu, which is the default. Import one namespace or the other, not both.

## Cameras

- [`Add2DCamera()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Add2DCamera(Stride.Engine.Game,System.String,System.Nullable{Stride.Core.Mathematics.Vector3},System.Nullable{Stride.Core.Mathematics.Vector3})) - Adds a 2D camera and binds it to the compositor's first camera slot.
- [`Add3DCamera()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Add3DCamera(Stride.Engine.Game,System.String,System.Nullable{Stride.Core.Mathematics.Vector3},System.Nullable{Stride.Core.Mathematics.Vector3},Stride.Engine.Processors.CameraProjectionMode)) - Adds a 3D camera, perspective by default.
- [`Add2DCameraController()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Add2DCameraController(Stride.Engine.Game,System.String,Stride.Input.Keys,System.Boolean)) - Makes the 2D camera pannable and zoomable, with an on-screen key reminder toggled by <kbd>F2</kbd>.
- [`Add3DCameraController()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Add3DCameraController(Stride.Engine.Game,System.Nullable{Stride.CommunityToolkit.Rendering.Text.DisplayPosition},System.String,Stride.Input.Keys,System.Boolean)) - Adds free-look movement to the 3D camera. The <kbd>F2</kbd> panel also prints the camera's live position and rotation, which is the quickest way to find numbers worth hard-coding.
- See [Camera Controllers](../camera-extensions/camera-controllers.md) for the keys, the on-screen help and every option on both scripts.
- [`GrabberScript`](xref:Stride.CommunityToolkit.Bepu.GrabberScript) - On the camera entity too, through `game.AddGrabber()`: left mouse picks up any dynamic body, carries it, and throws it on release, through servo constraints so the body still collides. One line that makes any Bepu scene a toy.
- [`Grabber2DScript`](xref:Stride.CommunityToolkit.Box2D.Grabber2DScript) - The same for a Box2D world: give it the `Box2DSimulation` and the left mouse picks up, drags and throws through a kinematic anchor and a motor joint.
- [`CharacterMover2D`](xref:Stride.CommunityToolkit.Box2D.CharacterMover2D) - A platformer character for a Box2D world with no rigid body: a capsule moved Quake style on Box2D v3's mover API, registered with the simulation, driven by `Throttle` and `Jump()`.
- [`SetCameraPosition()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.SetCameraPosition(Stride.Engine.Game,Stride.Core.Mathematics.Vector3,System.String)) - Moves the existing camera. Use this after `SetupBase3DScene()` rather than calling `Add3DCamera()` again, which would create a second camera competing for the same slot.
- [`SetCameraRotation()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.SetCameraRotation(Stride.Engine.Game,Stride.Core.Mathematics.Vector3,System.String)) - Aims the existing camera, taking yaw, pitch and roll in degrees.

## Sound

A code-only game has no `Sound` asset, so these are the only way to make a noise. All on `game.Audio`, from the `start` callback onwards; see [Audio Extensions](../audio-extensions/index.md) for the whole story.

- [`CreateProceduralSound()`](xref:Stride.CommunityToolkit.Audio.AudioSystemExtensions.CreateProceduralSound(Stride.Audio.AudioSystem,Stride.CommunityToolkit.Audio.SampleFiller,System.Int32,System.Boolean,System.Boolean,System.Boolean,Stride.Audio.AudioListener)) - A sound generated by a callback, with no file behind it.
- [`LoadWav()`](xref:Stride.CommunityToolkit.Audio.AudioSystemExtensions.LoadWav(Stride.Audio.AudioSystem,System.String)) - Reads a `.wav` from disk into a `WavSound`; each `CreateInstance()` is an independent playback.
- [`AttachListener()`](xref:Stride.CommunityToolkit.Audio.AudioSystemExtensions.AttachListener(Stride.Audio.AudioSystem,Stride.Engine.Entity)) - A listener that follows the camera, for spatialised sound. The engine's default listener never moves.

## Rendering and debugging

- [`ConfigurePostEffects()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.ConfigurePostEffects(Stride.Engine.Game,System.Action{Stride.Rendering.Images.PostProcessingEffects})) - Turns post effects on: `fx.Bloom.Enabled = true`. Everything starts disabled, and the colour transforms other than tone mapping must be added rather than enabled. Call it after `AddCleanUIStage()`.
- [`SetDeterministic()`](xref:Stride.CommunityToolkit.Engine.GameLoopExtensions.SetDeterministic(Stride.Engine.Game,System.Nullable{System.TimeSpan})) - Pins the loop so frame N is the same simulated instant on every run: fixed timestep, draws in step, one update per draw. For replays, lockstep, tests and screenshots.
- [`AddCleanUIStage()`](xref:Stride.CommunityToolkit.Rendering.Compositing.GraphicsCompositorExtensions.AddCleanUIStage(Stride.Rendering.Compositing.GraphicsCompositor)) - Adds a UI render stage to the `GraphicsCompositor` and resets its post effects to tone mapping only, so UI text stays clean and white. Enable bloom or other effects *after* this call. (Until September 2026 it silently left bloom, ambient occlusion, screen-space reflections, light streaks, lens flare and FXAA running - roughly half the frame time of a simple scene.)
- [`AddRenderTextureCamera()`](xref:Stride.CommunityToolkit.Rendering.Compositing.RenderTextureCameraExtensions.AddRenderTextureCamera(Stride.Engine.Game,Stride.Engine.Entity,System.Int32,System.Int32,Stride.Graphics.PixelFormat,System.Action{Stride.Rendering.Images.PostProcessingEffects})) - Makes a camera draw into a texture alongside the main view, for mirrors, monitors and minimaps; see [Render to Texture](../rendering/render-to-texture.md).
- [`AddGpuPicker()`](xref:Stride.CommunityToolkit.Effects.Picking.GpuPickerExtensions.AddGpuPicker(Stride.Engine.Game,Stride.Rendering.RenderGroupMask)) - Answers what is under a screen point from the rendered frame, with no collider: entity, mesh, material, instance and surface point, two frames after the request; see [GPU Picking](../rendering/gpu-picking.md). In the `Stride.CommunityToolkit.Effects` package.
- [`AddDebugShapes()`](xref:Stride.CommunityToolkit.DebugShapes.Code.DebugShapeExtensions.AddDebugShapes(Stride.Engine.Game,Stride.Rendering.RenderGroup)) - Registers the immediate-mode debug shape renderer, for drawing lines, spheres and boxes each frame.
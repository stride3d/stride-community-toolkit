# Extensions

Each extension addresses common game development scenarios. They encapsulate setup details so you can focus on game logic.

Feel free to inspect and modify the source to adapt behavior or create your own variants. These are starting points, use them as-is or customize as needed.

Practice and exploration help you master Stride. Tinker with the extensions and start building!

## GameExtensions.cs

Some extensions return `Entity` so it can be further modified.

![Done](https://img.shields.io/badge/status-done-green)

- [`Run()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Run(Stride.Engine.Game,Stride.Games.GameContext,System.Action{Stride.Engine.Scene},System.Action{Stride.Engine.Scene,Stride.Games.GameTime})) - Initializing the game; use `start` and `update` parameters.
- [`SetupBase2D()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.SetupBase2D(Stride.Engine.Game,System.Nullable{Stride.Core.Mathematics.Color})) - Adds a graphics compositor and a camera.
- [`SetupBase3D()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.SetupBase3D(Stride.Engine.Game)) - Adds a graphics compositor, camera, and directional light.
- [`SetupBase2DScene()`](xref:Stride.CommunityToolkit.Bepu.GameExtensions.SetupBase2DScene(Stride.Engine.Game)) - Like `SetupBase()` plus skybox, ground, mouse-look camera.
- [`SetupBase3DScene()`](xref:Stride.CommunityToolkit.Bepu.GameExtensions.SetupBase3DScene(Stride.Engine.Game)) - Like `SetupBase()` plus skybox, ground, mouse-look camera.
- [`AddGraphicsCompositor()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddGraphicsCompositor(Stride.Engine.Game)) - Adds a default `GraphicsCompositor`.
- [`Add2DCamera()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Add2DCamera(Stride.Engine.Game,System.String,System.Nullable{Stride.Core.Mathematics.Vector3},System.Nullable{Stride.Core.Mathematics.Vector3})) - Adds a 2D camera.
- [`Add3DCamera()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Add3DCamera(Stride.Engine.Game,System.String,System.Nullable{Stride.Core.Mathematics.Vector3},System.Nullable{Stride.Core.Mathematics.Vector3},Stride.Engine.Processors.CameraProjectionMode)) - Adds a 3D camera.
- [`AddDirectionalLight()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddDirectionalLight(Stride.Engine.Game,System.String)) - Adds a directional light.
- [`AddSkybox()`](xref:Stride.CommunityToolkit.Skyboxes.GameExtensions.AddSkybox(Stride.Engine.Game,System.String)) - Adds a skybox.
- [`Add2DGround()`](xref:Stride.CommunityToolkit.Bullet.GameExtensions.Add2DGround(Stride.Engine.Game,System.String,System.Nullable{Stride.Core.Mathematics.Vector2})) - Adds 2D ground.
- [`Add3DGround()`](xref:Stride.CommunityToolkit.Bepu.GameExtensions.Add3DGround(Stride.Engine.Game,System.String,System.Nullable{Stride.Core.Mathematics.Vector2},System.Boolean)) - Adds 3D ground.
- [`AddInfinite3DGround()`](xref:Stride.CommunityToolkit.Bullet.GameExtensions.AddInfinite3DGround(Stride.Engine.Game,System.String,System.Nullable{Stride.Core.Mathematics.Vector2},System.Boolean)) - Adds infinite 3D ground.

## GraphicsCompositorExtensions.cs

- [`AddCleanUIStage()`](xref:Stride.CommunityToolkit.Rendering.Compositing.GraphicsCompositorExtensions.AddCleanUIStage(Stride.Rendering.Compositing.GraphicsCompositor)) - Adds a UI render stage and a clean white text effect to the `GraphicsCompositor`.

![ToDo](https://img.shields.io/badge/status-todo-orange)

- `AddGizmo()` - Adds a gizmo to the ground.

## ScriptComponentExtensions.cs

| `DeltaTime()` | ![Done](https://img.shields.io/badge/status-done-green) | Returns delta time as `float` seconds |
| `GetCamera()` | ![Research](https://img.shields.io/badge/status-research-blue) | Gets the first camera named "Main"; currently doesn't work at game start due to null in `GraphicsCompositor` |
| `GetCamera(string name)` | ![Research](https://img.shields.io/badge/status-research-blue) | Gets the first camera by name; currently doesn't work at game start due to null in `GraphicsCompositor` |
| `GetFirstCamera()` | ![Research](https://img.shields.io/badge/status-research-blue) | Gets the first camera in `GraphicsCompositor`; currently doesn't work at game start due to null |

## AnimationComponentExtensions.cs

| `PlayAnimation()` | ![Done](https://img.shields.io/badge/status-done-green) | Plays an animation if not already playing |

## ModelComponentExtensions.cs

| `GetMeshHeight()` | ![Done](https://img.shields.io/badge/status-done-green) | Returns mesh height as `float` |
| `GetMeshHWL()` | ![Done](https://img.shields.io/badge/status-done-green) | Returns mesh height, width, and length as `Vector3` |

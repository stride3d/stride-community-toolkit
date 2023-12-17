# Extensions

Each extension has been crafted to address common game development scenarios. They encapsulate and abstract away some of the complexities involved in setting up these scenarios, thus allowing you to focus more on the game logic and less on the setup and configuration.

To modify an extension, you can examine its code to understand how it works. Once you grasp the underlying logic, you can modify it or even create a new extension that better suits your needs. Remember, these extensions are just tools to help you get started; don't be afraid to modify them or build your own to align with your unique requirements.

In conclusion, whether you are a beginner just starting out with Stride or an experienced developer looking for a quicker way to get your game up and running, these extensions are a valuable resource. They are designed to be a starting point that can be used as is, or can be customised and built upon to create the perfect solution for your game development needs.

Remember, the key to mastering Stride, and game development in general, is practice and exploration. So, go ahead, play around with these extensions, and start creating!

## GameExtensions.cs

Some extensions return `Entity` so it can be further modified.

![Done](https://img.shields.io/badge/status-done-green)

- [`Run()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Run(Stride.Engine.Game,Stride.Games.GameContext,System.Action{Stride.Engine.Scene},System.Action{Stride.Engine.Scene,Stride.Games.GameTime})) - Initialising the game, use `start` and `update` params
- [`SetupBase()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.SetupBase(Stride.Engine.Game)) - Adds graphics compositor, camera and directional light
- [`SetupBase3DScene()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.SetupBase3DScene(Stride.Engine.Game)) - Same as `SetupBase()` plus skybox, ground, mouse look camera
- [`AddGraphicsCompositor()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddGraphicsCompositor(Stride.Engine.Game)) - Adds a default `GraphicsCompositor`
- [`AddCamera()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddCamera(Stride.Engine.Game,System.String,System.Nullable{Stride.Core.Mathematics.Vector3},System.Nullable{Stride.Core.Mathematics.Vector3})) - Adds camera
- [`AddDirectionalLight()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddDirectionalLight(Stride.Engine.Game,System.String)) - Adds directional light
- [`AddSkybox()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddSkybox(Stride.Engine.Game,System.String)) - Adds skybox
- [`AddGround()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddGround(Stride.Engine.Game,System.String,System.Nullable{Stride.Core.Mathematics.Vector2},System.Boolean)) - Adds ground

## GraphicsCompositorExtensions.cs

- [`AddCleanUIStage()`](xref:Stride.CommunityToolkit.Rendering.Compositing.GraphicsCompositorExtensions.AddCleanUIStage(Stride.Rendering.Compositing.GraphicsCompositor)) - Adds a UI render stage and white/clean text effect to the given `GraphicsCompositor`

![ToDo](https://img.shields.io/badge/status-todo-orange)

- `AddGizmo()` - Adds Gizmo to the ground

## ScriptComponentExtensions.cs

| ```DeltaTime()``` |![Done](https://img.shields.io/badge/status-done-green)| Easier way of getting DeltaTime as a `float` in seconds |
| ```GetCamera()``` |![Research](https://img.shields.io/badge/status-research-blue)| Gets the first camera with the name "Main", currently doesnt work at the start of a game due to being null in the `GraphicsCompositor` |
| ```GetCamera(string name)``` |![Research](https://img.shields.io/badge/status-research-blue)| Gets the first camera with the name provided, currently doesnt work at the start of a game due to being null in the `GraphicsCompositor` |
| ```GetFirstCamera()``` |![Research](https://img.shields.io/badge/status-research-blue)| Gets the first camera in the `GraphicsCompositor`. currently doesnt work at the start of a game due to being null in the `GraphicsCompositor` |

## AnimationComponentExtensions.cs

| ```PlayAnimation()``` |![Done](https://img.shields.io/badge/status-done-green)| Plays an animation if not already playing |

## ModelComponentExtensions.cs

| ```GetMeshHeight()``` |![Done](https://img.shields.io/badge/status-done-green)| Gets the Mesh height as a `float` |
| ```GetMeshHWL()``` |![Done](https://img.shields.io/badge/status-done-green)| Gets the Mesh height, width and length as a `Vector3` |

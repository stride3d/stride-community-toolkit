# GameExtensions.cs

![Done](https://img.shields.io/badge/status-done-green)

`GameExtensions.cs` provides a suite of extension methods for the Game class in the Stride game engine, enhancing its capabilities and offering convenient functionalities to game developers. These methods streamline common tasks in game development, ranging from performance monitoring to material creation and entity manipulation.

Here's a brief overview of the functionalities provided by these extension methods:

- [`AddAllDirectionLighting()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddAllDirectionLighting(Stride.Engine.Game,System.Single,System.Boolean)) - Adds directional lighting from multiple angles to the current scene
- [`AddProfiler()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddProfiler(Stride.Engine.Game,System.String)) - Adds a profiler to the game, which can be toggled on/off with Left Shift + Left Ctrl + P
- [`CreateMaterial()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.CreateMaterial(Stride.Games.IGame,System.Nullable{Stride.Core.Mathematics.Color},System.Single,System.Single)) - Creates a basic material with optional color, specular reflection, and micro-surface smoothness values
- [`DeltaTime()`](xref:Stride.CommunityToolkit.Games.GameExtensions.DeltaTime(Stride.Games.IGame)) - Gets the time elapsed since the last game update in seconds as a single-precision floating-point number
- [`DeltaTimeAccurate()`](xref:Stride.CommunityToolkit.Games.GameExtensions.DeltaTimeAccurate(Stride.Games.IGame)) - Gets the time elapsed since the last game update in seconds as a double-precision floating-point
- [`FPS()`](xref:Stride.CommunityToolkit.Games.GameExtensions.FPS(Stride.Games.IGame)) - Retrieves the current frames per second (FPS) rate of the running game
- [`SetFocusLostFPS()`](xref:Stride.CommunityToolkit.Games.GameExtensions.SetFocusLostFPS(Stride.Games.IGame,System.Int32)) - Sets the maximum frames per second (FPS) rate for the game when not in focus
- [`SetMaxFPS()`](xref:Stride.CommunityToolkit.Games.GameExtensions.SetMaxFPS(Stride.Games.IGame,System.Int32)) - Sets the maximum frames per second (FPS) rate for the game

![ToDo](https://img.shields.io/badge/status-todo-orange)

- `AddEntityNames()` - Debug. To see entity properties in the game
- `AddPhysicsDebugger()` - Debug. To see colliders. Tracked here [Issue #9](https://github.com/stride3d/stride-community-toolkit/issues/9)
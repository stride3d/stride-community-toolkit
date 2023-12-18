# GameExtensions.cs

![Done](https://img.shields.io/badge/status-done-green)

`GameExtensions.cs` provides a suite of extension methods for the Game class in the Stride game engine, enhancing its capabilities and offering convenient functionalities to game developers. These methods streamline common tasks in game development, ranging from performance monitoring to material creation and entity manipulation.

Here's a brief overview of the functionalities provided by these extension methods:

- [`AddProfiler()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.AddProfiler(Stride.Engine.Game,System.String)) - Adds a profiler to the game, which can be toggled on/off with Left Shift + Left Ctrl + P
- [`CreateMaterial()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.CreateMaterial(Stride.Engine.Game,System.Nullable{Stride.Core.Mathematics.Color},System.Single,System.Single)) - Creates a basic material with optional color, specular reflection, and microsurface smoothness values
- [`CreatePrimitive()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.CreatePrimitive(Stride.Engine.Game,Stride.CommunityToolkit.ProceduralModels.PrimitiveModelType,System.String,Stride.Rendering.Material,System.Boolean,System.Nullable{Stride.Core.Mathematics.Vector3})) - Creates a primitive 3D model entity of the specified type with optional customizations
- [`DeltaTime()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.DeltaTime(Stride.Games.IGame)) - Gets the time elapsed since the last game update in seconds as a single-precision floating-point number
- [`DeltaTimeAccurate()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.DeltaTimeAccurate(Stride.Games.IGame)) - Gets the time elapsed since the last game update in seconds as a double-precision floating-point
- [`FPS()`](xref:Stride.CommunityToolkit.Engine.GameExtensions.FPS(Stride.Engine.Game)) - Retrieves the current frames per second (FPS) rate of the running game

![ToDo](https://img.shields.io/badge/status-todo-orange)

- `AddEntityNames()` - Debug. To see entity properties in the game
- `AddPhysicsDebugger()` - Debug. To see colliders. Tracked here [Issue #9](https://github.com/stride3d/stride-community-toolkit/issues/9)
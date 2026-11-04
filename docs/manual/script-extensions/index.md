# Script Extensions

Extension methods for `ScriptComponent`, in the `Stride.CommunityToolkit.Engine` namespace. Mostly shortcuts for things a script reaches for on almost every frame.

- [`DeltaTime()`](xref:Stride.CommunityToolkit.Engine.ScriptComponentExtensions.DeltaTime(Stride.Engine.ScriptComponent)) - Seconds since the last update, instead of writing `Game.UpdateTime.Elapsed.TotalSeconds` out in full.
- [`GetCamera()`](xref:Stride.CommunityToolkit.Engine.ScriptComponentExtensions.GetCamera(Stride.Engine.ScriptComponent,System.String)) - Gets the camera bound to the graphics compositor slot named `Main`, or to the slot named by `cameraName`. Returns `null` when no slot matches.
- [`GetFirstCamera()`](xref:Stride.CommunityToolkit.Engine.ScriptComponentExtensions.GetFirstCamera(Stride.Engine.ScriptComponent)) - Gets the first camera in the graphics compositor whatever its slot is called, which is usually what you want when there is only one. Returns `null` when the compositor has no camera slot.

> [!NOTE]
> These look the camera up by its graphics compositor slot rather than by searching the scene, so they
> find the camera that is actually being rendered from. A slot is bound a frame or two after the game
> starts, so a script's first updates may get `null`.

## See also

- [Script System Extensions](../script-system-extensions/index.md)
- [Camera Extensions](../camera-extensions/index.md)
- [Components and Scripts](../components-and-scripts.md)
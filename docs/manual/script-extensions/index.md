# Script Extensions

Extension methods for `ScriptComponent`, in the `Stride.CommunityToolkit.Engine` namespace. Mostly shortcuts for things a script reaches for on almost every frame.

- [`DeltaTime()`](xref:Stride.CommunityToolkit.Engine.ScriptComponentExtensions.DeltaTime(Stride.Engine.ScriptComponent)) - Seconds since the last update, instead of writing `Game.UpdateTime.Elapsed.TotalSeconds` out in full.
- [`GetGCCamera()`](xref:Stride.CommunityToolkit.Engine.ScriptComponentExtensions.GetGCCamera(Stride.Engine.ScriptComponent)) - Gets the camera bound to the graphics compositor slot named `Main`. Returns `null` when no slot matches.
- [`GetGCCamera(cameraName)`](xref:Stride.CommunityToolkit.Engine.ScriptComponentExtensions.GetGCCamera(Stride.Engine.ScriptComponent,System.String)) - The same, for a camera slot you named yourself.
- [`GetFirstGCCamera()`](xref:Stride.CommunityToolkit.Engine.ScriptComponentExtensions.GetFirstGCCamera(Stride.Engine.ScriptComponent)) - Gets the first camera in the graphics compositor whatever its slot is called, which is usually what you want when there is only one.

> [!NOTE]
> `GC` here is the *graphics compositor*, not the garbage collector. These look the camera up by its
> compositor slot rather than by searching the scene, so they find the camera that is actually being
> rendered from.
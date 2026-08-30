# Entity Extensions

Extension methods for `Entity`, in the `Stride.CommunityToolkit.Engine` namespace. They cover the four things you end up doing to an entity constantly: getting components off it, finding it again, attaching a controller, and drawing a gizmo on it.

## Components

- [`GetComponent<T>()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.GetComponent``1(Stride.Engine.Entity)) - The first component of type `T`, or `null`.
- [`GetComponents<T>()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.GetComponents``1(Stride.Engine.Entity)) - Every component of type `T` on the entity.
- [`TryGetComponent<T>()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.TryGetComponent``1(Stride.Engine.Entity,``0@)) - The `Try` form, for when a missing component is expected rather than a bug.
- [`GetComponentInChildren<T>()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.GetComponentInChildren``1(Stride.Engine.Entity)) - Searches the entity's descendants, which is where the component usually is on an imported model.
- [`Get<T1, T2, …>()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.Get``2(Stride.Engine.Entity)) - Fetches several components in one pass and returns them as a tuple, so you can deconstruct them into named locals. Overloads exist for two through sixteen types.

```csharp
var (model, light) = entity.Get<ModelComponent, LightComponent>();
```

## Finding entities

- [`FindEntity()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.FindEntity(Stride.Engine.Entity,System.String)) - Finds a direct child by name.
- [`FindEntityRecursive()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.FindEntityRecursive(Stride.Engine.Entity,System.String)) - Searches the whole subtree, not only the immediate children.
- [`WorldPosition()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.WorldPosition(Stride.Engine.Entity,System.Boolean)) - The entity's position in world space rather than relative to its parent, updating the transform hierarchy first unless you ask it not to.
- [`Remove()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.Remove(Stride.Engine.Entity)) - Takes the entity out of its scene by setting `Scene` to `null`.

## Camera controllers

These are the entity-level counterparts of the `Game` extensions; use them when you already hold the camera entity.

- [`Add2DCameraController()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.Add2DCameraController(Stride.Engine.Entity,Stride.Input.Keys,System.Boolean)) - Attaches the interactive 2D camera script, giving pan and zoom.
- [`Add3DCameraController()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.Add3DCameraController(Stride.Engine.Entity,System.Nullable{Stride.CommunityToolkit.Rendering.Text.DisplayPosition},Stride.Input.Keys,System.Boolean)) - Attaches the interactive 3D camera script, giving free-look movement and an on-screen key reminder.

See [Camera Controllers](../camera-extensions/camera-controllers.md) for the keys, the on-screen help and every option on both scripts.

## Gizmos

- [`AddGizmo()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.AddGizmo(Stride.Engine.Entity,Stride.Graphics.GraphicsDevice,System.Nullable{Stride.Core.Mathematics.Color},System.Nullable{Stride.Core.Mathematics.Color},System.Nullable{Stride.Core.Mathematics.Color},System.Boolean,System.Boolean)) - Draws a translation gizmo at the entity, with optional per-axis colours and axis letters.
- [`AddLightDirectionalGizmo()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.AddLightDirectionalGizmo(Stride.Engine.Entity,Stride.Graphics.GraphicsDevice,System.Nullable{Stride.Core.Mathematics.Color})) - Draws which way a directional light is pointing, which is otherwise invisible.
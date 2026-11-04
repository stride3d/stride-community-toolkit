# Entity Extensions

Extension methods for `Entity`, in the `Stride.CommunityToolkit.Engine` namespace. They cover getting components off an entity, finding and adding entities, moving them, attaching a camera controller and drawing a gizmo.

## Components

- [`GetComponent<T>()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.GetComponent``1(Stride.Engine.Entity)) - The first component of type `T`, or `null`. This is Stride's own `entity.Get<T>()` under the name other engines use; prefer `Get<T>()` in new code.
- [`GetComponents<T>()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.GetComponents``1(Stride.Engine.Entity)) - Every component of type `T` on the entity: Stride's own `entity.GetAll<T>()`.
- [`TryGetComponent<T>()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.TryGetComponent``1(Stride.Engine.Entity,``0@)) - The `Try` form, for when a missing component is expected rather than a bug.
- [`GetComponentInDescendantsAndSelf<T>()`](xref:Stride.CommunityToolkit.Engine.EntitySearchExtensions.GetComponentInDescendantsAndSelf``1(Stride.Engine.Entity,System.Boolean)) - The entity first, then its descendants depth-first, which is where the component usually is on an imported model. [`GetComponentInChildren<T>()`](xref:Stride.CommunityToolkit.Engine.EntitySearchExtensions.GetComponentInChildren``1(Stride.Engine.Entity,System.Boolean)) searches the descendants only, breadth-first.
- [`Get<T1, T2, …>()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.Get``2(Stride.Engine.Entity)) - Fetches several components in one pass and returns them as a tuple, so you can deconstruct them into named locals. Overloads exist for two through sixteen types.

```csharp
var (model, light) = entity.Get<ModelComponent, LightComponent>();
```

## Finding entities

- [`scene.FindEntity()`](xref:Stride.CommunityToolkit.Engine.SceneExtensions.FindEntity(Stride.Engine.Scene,System.String,System.Boolean)) - Finds an entity by name among the scene's top-level entities, or with `recursive: true` among their descendants too. Search once, in `Start`, and keep the result.
- [`FindEntity()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.FindEntity(Stride.Engine.Entity,System.String)) - The same search, started from an entity that is already in the scene: its top-level entities, not this entity's children.
- [`FindEntityRecursive()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.FindEntityRecursive(Stride.Engine.Entity,System.String)) - The same from an entity, descendants included. Both throw an `InvalidOperationException` that says what to do when the entity is not in a scene yet.
- [`WorldPosition()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.WorldPosition(Stride.Engine.Entity,System.Boolean)) - The entity's position in world space rather than relative to its parent, updating the transform hierarchy first unless you ask it not to.
- [`AddTo(scene)`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.AddTo(Stride.Engine.Entity,Stride.Engine.Scene)) / [`AddTo(parent)`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.AddTo(Stride.Engine.Entity,Stride.Engine.Entity)) - Adds the entity to a scene, or as a child of another entity, and returns it, so creating, adding and placing read as one line: `game.CreatePrimitive(Primitive3D.Cube()).AddTo(scene).Transform.Position = new Vector3(0, 8, 0);`.
- [`Remove()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.Remove(Stride.Engine.Entity)) - Takes the entity out of its scene by setting `Scene` to `null`.

To search a whole scene rather than one entity's subtree:

- [`scene.GetComponentsInScene<T>()`](xref:Stride.CommunityToolkit.Engine.SceneSearchExtensions.GetComponentsInScene``1(Stride.Engine.Scene,System.Boolean)) - Every component of type `T` on any entity of the scene, its children and, unless `includeChildScenes` is `false`, its child scenes. [`GetComponentInScene<T>()`](xref:Stride.CommunityToolkit.Engine.SceneSearchExtensions.GetComponentInScene``1(Stride.Engine.Scene,System.Boolean)) is the first one or `null`.
- [`game.GetComponentsInScene<T>()`](xref:Stride.CommunityToolkit.Engine.SceneSearchExtensions.GetComponentsInScene``1(Stride.Games.IGame)) - The same across every scene the game has loaded. Both walk every entity, so use them for a lookup at start or on an event, not every frame; a manager that many scripts need is better registered as a service, see the [FAQ](../faq.md#is-there-an-equivalent-of-unitys-findobjectoftype).

## Turning and moving

Extension methods for `TransformComponent`, in the same namespace:

- [`Translate()`](xref:Stride.CommunityToolkit.Engine.TransformExtensions.Translate(Stride.Engine.TransformComponent,Stride.Core.Mathematics.Vector3,Stride.CommunityToolkit.Engine.Space)) and [`Rotate()`](xref:Stride.CommunityToolkit.Engine.TransformExtensions.Rotate(Stride.Engine.TransformComponent,Stride.Core.Mathematics.Vector3,Stride.CommunityToolkit.Engine.Space)) - Move or turn by an amount in the entity's own space (`Space.Self`, the default) or in world space.
- [`LookAt()`](xref:Stride.CommunityToolkit.Engine.TransformExtensions.LookAt(Stride.Engine.TransformComponent,Stride.Core.Mathematics.Vector3,Stride.Core.Mathematics.Vector3,System.Single)) - Turns the entity's forward axis (-Z) towards a world point, with an optional `smooth` fraction for a turn that eases over several frames.
- [`FaceDirection()`](xref:Stride.CommunityToolkit.Engine.TransformExtensions.FaceDirection(Stride.Engine.TransformComponent,Stride.Core.Mathematics.Vector3,System.Nullable{Stride.Core.Mathematics.Vector3},System.Single)) - `LookAt` along a direction instead of towards a point: a character facing the way it moves, a projectile facing its velocity. A zero direction is ignored.
- [`RotateAround()`](xref:Stride.CommunityToolkit.Engine.TransformExtensions.RotateAround(Stride.Engine.TransformComponent,Stride.Core.Mathematics.Vector3,Stride.Core.Mathematics.Vector3,System.Single)) - Swings the entity about an axis through a point and turns it by the same angle, a moon keeping one face to its planet. Point and axis are in the parent's space, which is world space for a root entity; call it every frame with `speed * deltaTime` for an orbit.

## Camera controllers

These are the entity-level counterparts of the `Game` extensions; use them when you already hold the camera entity.

- [`Add2DCameraController()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.Add2DCameraController(Stride.Engine.Entity,Stride.Input.Keys,System.Boolean)) - Attaches the interactive 2D camera script, giving pan and zoom.
- [`Add3DCameraController()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.Add3DCameraController(Stride.Engine.Entity,System.Nullable{Stride.CommunityToolkit.Rendering.Anchor},Stride.Input.Keys,System.Boolean,System.Boolean)) - Attaches the interactive 3D camera script, giving free-look movement and an on-screen key reminder.

See [Camera Controllers](../camera-extensions/camera-controllers.md) for the keys, the on-screen help and every option on both scripts.

## Gizmos

- [`AddGizmo()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.AddGizmo(Stride.Engine.Entity,Stride.Graphics.GraphicsDevice,System.Nullable{Stride.Core.Mathematics.Color},System.Nullable{Stride.Core.Mathematics.Color},System.Nullable{Stride.Core.Mathematics.Color},System.Boolean,System.Boolean)) - Draws a translation gizmo at the entity, with optional per-axis colours and axis letters.
- [`AddLightDirectionalGizmo()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.AddLightDirectionalGizmo(Stride.Engine.Entity,Stride.Graphics.GraphicsDevice,System.Nullable{Stride.Core.Mathematics.Color})) - Draws which way a directional light is pointing, which is otherwise invisible.

## See also

- [Game Extensions](../game-extensions/index.md)
- [Camera Extensions](../camera-extensions/index.md)
- [Physics Extensions](../physics-extensions/index.md)
- [Entity Text](../rendering/entity-text.md)
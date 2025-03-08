# EntityExtensions.cs

![Done](https://img.shields.io/badge/status-done-green)

`EntityExtensions.cs` is a collection of extension methods enhancing the functionality of entities within the Stride game engine. These methods provide additional capabilities to entities, making it easier to work with them in various scenarios. From adding gizmos and interactive scripts to retrieving components and managing entity positions.

Here is an overview of the available extension methods:

- [`Add2DCameraController()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.Add2DCameraController(Stride.Engine.Entity)) - Adds an interactive 2D camera script to the specified entity, enabling camera movement and rotation
- [`Add3DCameraController()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.Add3DCameraController(Stride.Engine.Entity)) - Adds an interactive 3D camera script to the specified entity, enabling camera movement and rotation
- [`AddGizmo()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.AddGizmo(Stride.Engine.Entity,Stride.Graphics.GraphicsDevice,System.Nullable{Stride.Core.Mathematics.Color},System.Nullable{Stride.Core.Mathematics.Color},System.Nullable{Stride.Core.Mathematics.Color},System.Boolean,System.Boolean)) - Adds a TranslationGizmo to the specified entity with optional custom colors
- [`GetComponent<T>()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.GetComponent``1(Stride.Engine.Entity)) - Retrieves the first component of the specified type from the entity
- [`GetComponents<T>()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.GetComponents``1(Stride.Engine.Entity)) - Retrieves all components of the specified type from the entity
- [`Remove()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.Remove(Stride.Engine.Entity)) - Removes the entity from its current scene by setting its `Scene` property to null
- [`TryGetComponent<T>()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.TryGetComponent``1(Stride.Engine.Entity,``0@)) - Tries to retrieve a component of type T from the given entity
- [`WorldPosition()`](xref:Stride.CommunityToolkit.Engine.EntityExtensions.WorldPosition(Stride.Engine.Entity,System.Boolean)) - An easier way to get world position

These extensions are designed to streamline common tasks associated with entities in Stride, enhancing the overall efficiency and flexibility of game development workflows.
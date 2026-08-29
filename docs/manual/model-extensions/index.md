# Model Extensions

Extension methods for `ModelComponent`, in the `Stride.CommunityToolkit.Engine` namespace.

## Measuring a model

Useful when you are placing procedurally created entities and do not know their size up front.

- [`GetMeshHWL()`](xref:Stride.CommunityToolkit.Engine.ModelComponentExtensions.GetMeshHWL(Stride.Engine.ModelComponent)) - Returns the height, width and length of the model as a `Vector3`, from its bounding box.
- [`GetMeshHeight()`](xref:Stride.CommunityToolkit.Engine.ModelComponentExtensions.GetMeshHeight(Stride.Engine.ModelComponent)) - Just the height, for the common case of standing something on the ground.
- [`GetMeshVerticesAndIndices()`](xref:Stride.CommunityToolkit.Engine.ModelComponentExtensions.GetMeshVerticesAndIndices(Stride.Engine.ModelComponent,Stride.Games.IGame)) - Reads the raw vertex and index data back out of the mesh, for building a collider or doing your own geometry work.

## Material parameters

- [`SetMaterialParameter()`](xref:Stride.CommunityToolkit.Engine.ModelComponentExtensions.SetMaterialParameter``1(Stride.Engine.ModelComponent,Stride.Rendering.ValueParameterKey{``0},``0,System.Int32,System.Int32)) - Sets a shader parameter on one of the component's materials, without going through `RenderGroup` plumbing yourself. Overloads cover value, object and permutation parameters, keys and accessors, single values and arrays, with optional `materialIndex` and `passIndex`.
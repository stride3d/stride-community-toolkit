# Model Extensions

Extension methods for `ModelComponent`, in the `Stride.CommunityToolkit.Engine` namespace.

## Measuring a model

Useful when you are placing procedurally created entities and do not know their size up front.

- [`GetModelSize()`](xref:Stride.CommunityToolkit.Engine.ModelComponentExtensions.GetModelSize(Stride.Engine.ModelComponent)) - Returns the size of the model's bounding box as a `Vector3`: width along X, height along Y and depth along Z.
- [`GetMeshHeight()`](xref:Stride.CommunityToolkit.Engine.ModelComponentExtensions.GetMeshHeight(Stride.Engine.ModelComponent)) - Just the height, for the common case of standing something on the ground.
- [`GetMeshVerticesAndIndices()`](xref:Stride.CommunityToolkit.Engine.ModelComponentExtensions.GetMeshVerticesAndIndices(Stride.Engine.ModelComponent,Stride.Games.IGame)) - Reads the raw vertex and index data back out of the mesh, for building a collider or doing your own geometry work.

## Material parameters

- [`SetMaterialParameter()`](xref:Stride.CommunityToolkit.Engine.ModelComponentExtensions.SetMaterialParameter``1(Stride.Engine.ModelComponent,Stride.Rendering.ValueParameterKey{``0},``0,System.Int32,System.Int32)) - Sets a shader parameter on one pass of one of the component's materials. The first call gives the component its own copy of the material, so other models that share it are not changed. Overloads cover value, object and permutation parameters, keys and accessors, single values and arrays, with optional `materialIndex` and `passIndex`.

## See also

- [Materials](../rendering/materials.md)
- [Entity Extensions](../entity-extensions/index.md)
Physics extensions provide..  

> [!TIP]
> New to Bepu, or seeing a mesh that moves without colliding? Read
> [Bepu: Who Owns the Transform?](bepu-transform-ownership.md) first. It covers the one-way
> physics-to-transform sync and the silent failures that follow from it.
>
> Building joints or motors? [Bepu: Why Isn't My Constraint Doing Anything?](bepu-constraints.md)
> covers the equivalent silent failures on the constraint side - jammed joints, motors that produce
> no force, and settings that are discarded without warning.

**Bepu Extensions**

- [`Add3DGround()`](xref:Stride.CommunityToolkit.Bepu.GameExtensions.Add3DGround(Stride.Engine.Game,System.String,System.Nullable{Stride.Core.Mathematics.Vector2},System.Boolean))
- [`Create2DPrimitive()`](xref:Stride.CommunityToolkit.Bepu.GameExtensions.Create2DPrimitive(Stride.Games.IGame,Stride.CommunityToolkit.Rendering.ProceduralModels.Primitive2DModelType,Stride.CommunityToolkit.Bepu.Bepu2DPhysicsOptions)) - Creates a primitive 2D model entity of the specified type with optional customizations
- [`Create3DPrimitive()`](xref:Stride.CommunityToolkit.Bepu.GameExtensions.Create3DPrimitive(Stride.Games.IGame,Stride.CommunityToolkit.Rendering.ProceduralModels.PrimitiveModelType,Stride.CommunityToolkit.Bepu.Bepu3DPhysicsOptions)) - Creates a primitive 3D model entity of the specified type with optional customizations
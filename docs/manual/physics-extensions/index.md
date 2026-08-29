# Physics Extensions

Stride ships two physics engines, and the toolkit wraps both. The methods below come in matching pairs - `Stride.CommunityToolkit.Bepu` and `Stride.CommunityToolkit.Bullet` define many of the same names - so **import one namespace or the other, not both**.

Bepu is the newer engine and the toolkit's default; the examples and the `SetupBase*Scene()` shortcuts assume it unless they say otherwise.

> [!TIP]
> New to Bepu, or seeing a mesh that moves without colliding? Read
> [Bepu: Who Owns the Transform?](bepu-transform-ownership.md) first. It covers the one-way
> physics-to-transform sync and the silent failures that follow from it.
>
> Building joints or motors? [Bepu: Why Isn't My Constraint Doing Anything?](bepu-constraints.md)
> covers the equivalent silent failures on the constraint side - jammed joints, motors that produce
> no force, and settings that are discarded without warning.

## Bepu

### Scene setup

- [`SetupBase3DScene()`](xref:Stride.CommunityToolkit.Bepu.GameExtensions.SetupBase3DScene(Stride.Engine.Game)) - Compositor, 3D camera, directional light, skybox, ground and a camera controller. One call to a scene you can fly around.
- [`SetupBase2DScene()`](xref:Stride.CommunityToolkit.Bepu.GameExtensions.SetupBase2DScene(Stride.Engine.Game)) - The 2D equivalent, with a 2D camera and 2D ground.
- [`Add3DGround()`](xref:Stride.CommunityToolkit.Bepu.GameExtensions.Add3DGround(Stride.Engine.Game,Stride.CommunityToolkit.Bepu.Bepu3DPhysicsOptions)) - A static ground plane on its own, when you do not want the rest of the scene setup.
- [`Add2DGround()`](xref:Stride.CommunityToolkit.Bepu.GameExtensions.Add2DGround(Stride.Engine.Game,Stride.CommunityToolkit.Bepu.Bepu2DPhysicsOptions)) - The 2D static ground collider.

### Primitives with colliders

- [`Create3DPrimitive()`](xref:Stride.CommunityToolkit.Bepu.GameExtensions.Create3DPrimitive(Stride.Games.IGame,Stride.CommunityToolkit.Rendering.ProceduralModels.PrimitiveModelType,Stride.CommunityToolkit.Bepu.Bepu3DPhysicsOptions)) - A primitive model entity with a matching Bepu collider already attached. `Bepu3DPhysicsOptions` controls the material, size, mass and whether the body is static.
- [`Create2DPrimitive()`](xref:Stride.CommunityToolkit.Bepu.GameExtensions.Create2DPrimitive(Stride.Games.IGame,Stride.CommunityToolkit.Rendering.ProceduralModels.Primitive2DModelType,Stride.CommunityToolkit.Bepu.Bepu2DPhysicsOptions)) - The 2D equivalent, constrained to the XY plane.

### Raycasting

- [`Raycast()`](xref:Stride.CommunityToolkit.Bepu.CameraComponentExtensions.Raycast(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector2,System.Single,Stride.BepuPhysics.HitInfo@,Stride.BepuPhysics.CollisionMask)) - Casts from the camera through a screen position and reports the first hit.
- [`RaycastMouse()`](xref:Stride.CommunityToolkit.Bepu.CameraComponentExtensions.RaycastMouse(Stride.Engine.CameraComponent,Stride.Engine.ScriptComponent,System.Single,Stride.BepuPhysics.HitInfo@,Stride.BepuPhysics.CollisionMask)) - The same, reading the mouse position for you.
- [`BepuSimulation.RayCast()`](xref:Stride.CommunityToolkit.Bepu.SimulationExtensions.RayCast(Stride.BepuPhysics.BepuSimulation,Stride.CommunityToolkit.Mathematics.RaySegment@,Stride.BepuPhysics.HitInfo@,Stride.BepuPhysics.CollisionMask)) - Casts a `RaySegment` directly, with no camera and no maximum distance to choose: the segment's own length is the limit. Pairs with `ScreenToWorldRaySegment()`.
- [`RayCastPenetrating()`](xref:Stride.CommunityToolkit.Bepu.SimulationExtensions.RayCastPenetrating(Stride.BepuPhysics.BepuSimulation,Stride.CommunityToolkit.Mathematics.RaySegment@,System.Collections.Generic.ICollection{Stride.BepuPhysics.HitInfo},Stride.BepuPhysics.CollisionMask)) - Returns every hit along the segment rather than stopping at the first. An overload fills a collection you supply, to avoid allocating.

### Convex hulls

In the `Stride.CommunityToolkit.Bepu.Extensions` namespace, for giving an arbitrary mesh a collider.

- [`ToConvexHullCollider()`](xref:Stride.CommunityToolkit.Bepu.Extensions.ConvexHullColliderExtensions.ToConvexHullCollider(Stride.Graphics.GeometricMeshData{Stride.Graphics.VertexPositionNormalTexture})) - Wraps mesh data in a single convex hull. Cheap, but it fills in any concave detail.
- [`ToDecomposedHulls()`](xref:Stride.CommunityToolkit.Bepu.Extensions.ConvexHullColliderExtensions.ToDecomposedHulls(Stride.Graphics.GeometricMeshData{Stride.Graphics.VertexPositionNormalTexture})) - Splits the mesh into several hulls so concave shapes collide correctly. More accurate, more expensive to build.

## Bullet

### Scene setup

- [`SetupBase3DScene()`](xref:Stride.CommunityToolkit.Bullet.GameExtensions.SetupBase3DScene(Stride.Engine.Game)) - The Bullet counterpart of the Bepu scene setup.
- [`SetupBase2DScene()`](xref:Stride.CommunityToolkit.Bullet.GameExtensions.SetupBase2DScene(Stride.Engine.Game)) - The 2D equivalent.
- [`Add3DGround()`](xref:Stride.CommunityToolkit.Bullet.GameExtensions.Add3DGround(Stride.Engine.Game,Stride.CommunityToolkit.Bullet.Bullet3DPhysicsOptions)) - A static ground plane.
- [`Add2DGround()`](xref:Stride.CommunityToolkit.Bullet.GameExtensions.Add2DGround(Stride.Engine.Game,Stride.CommunityToolkit.Bullet.Bullet2DPhysicsOptions)) - The 2D static ground collider.
- [`AddInfinite3DGround()`](xref:Stride.CommunityToolkit.Bullet.GameExtensions.AddInfinite3DGround(Stride.Engine.Game,Stride.CommunityToolkit.Bullet.Bullet3DPhysicsOptions)) - A ground plane nothing can fall off the edge of. Bullet only; there is no Bepu equivalent.

### Primitives and colliders

- [`Create3DPrimitive()`](xref:Stride.CommunityToolkit.Bullet.GameExtensions.Create3DPrimitive(Stride.Games.IGame,Stride.CommunityToolkit.Rendering.ProceduralModels.PrimitiveModelType,Stride.CommunityToolkit.Bullet.Bullet3DPhysicsOptions)) - A primitive model entity with a matching Bullet collider.
- [`Create2DPrimitive()`](xref:Stride.CommunityToolkit.Bullet.GameExtensions.Create2DPrimitive(Stride.Games.IGame,Stride.CommunityToolkit.Rendering.ProceduralModels.Primitive2DModelType,Stride.CommunityToolkit.Bullet.Bullet2DPhysicsOptions)) - The 2D equivalent.
- [`AddBullet3DPhysics()`](xref:Stride.CommunityToolkit.Bullet.EntityExtensions.AddBullet3DPhysics(Stride.Engine.Entity,Stride.CommunityToolkit.Rendering.ProceduralModels.PrimitiveModelType,Stride.CommunityToolkit.Bullet.Bullet3DPhysicsOptions)) - Attaches a collider to an entity you already built, rather than creating both together.
- [`AddBullet2DPhysics()`](xref:Stride.CommunityToolkit.Bullet.EntityExtensions.AddBullet2DPhysics(Stride.Engine.Entity,Stride.CommunityToolkit.Rendering.ProceduralModels.Primitive2DModelType,Stride.CommunityToolkit.Bullet.Bullet2DPhysicsOptions)) - The 2D equivalent.

### Raycasting

- [`Raycast()`](xref:Stride.CommunityToolkit.Bullet.CameraComponentExtensions.Raycast(Stride.Engine.CameraComponent,Stride.Engine.ScriptComponent,Stride.Core.Mathematics.Vector2,Stride.Physics.CollisionFilterGroups,Stride.Physics.CollisionFilterGroupFlags)) - Casts from the camera through a screen position. Takes either a `ScriptComponent` or a `Simulation`.
- [`RaycastMouse()`](xref:Stride.CommunityToolkit.Bullet.CameraComponentExtensions.RaycastMouse(Stride.Engine.CameraComponent,Stride.Engine.ScriptComponent,Stride.Physics.CollisionFilterGroups,Stride.Physics.CollisionFilterGroupFlags)) - The same, reading the mouse position for you.
- [`Simulation.Raycast()`](xref:Stride.CommunityToolkit.Bullet.SimulationExtensions.Raycast(Stride.Physics.Simulation,Stride.CommunityToolkit.Mathematics.RaySegment)) - Casts a `RaySegment` directly, with no camera involved. Overloads also cast from an entity along a direction.
- [`RaycastPenetrating()`](xref:Stride.CommunityToolkit.Bullet.SimulationExtensions.RaycastPenetrating(Stride.Physics.Simulation,Stride.CommunityToolkit.Mathematics.RaySegment)) - Returns every hit along the ray rather than stopping at the first. An overload fills a list you supply, to avoid allocating.

### Debugging

- [`ShowColliders()`](xref:Stride.CommunityToolkit.Bullet.GameExtensions.ShowColliders(Stride.Engine.Game)) - Draws the collider shapes over the scene, which is how you find out that the collider is not where the model is.
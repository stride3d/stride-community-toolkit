# Camera Extensions

Extension methods for `CameraComponent`. They fall into two halves: converting between screen and world space, which needs no physics engine, and raycasting into the scene, which does - so those live in the Bepu and Bullet packages.

## Screen and world space

In the `Stride.CommunityToolkit.Engine` namespace. Screen positions here are normalized: `(0,0)` is the bottom-left of the viewport and `(1,1)` the top-right.

- [`GetPickRay()`](xref:Stride.CommunityToolkit.Engine.CameraComponentExtensions.GetPickRay(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector2)) - A `Ray` from the camera through a screen point. The usual starting point for object picking.
- [`CalculateRayFromScreenPosition()`](xref:Stride.CommunityToolkit.Engine.CameraComponentExtensions.CalculateRayFromScreenPosition(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector2)) - The same ray as a `(nearPoint, farPoint)` pair of world positions, when you want the endpoints rather than a `Ray`.
- [`CalculateRayPlaneIntersectionPoint()`](xref:Stride.CommunityToolkit.Engine.CameraComponentExtensions.CalculateRayPlaneIntersectionPoint(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector2)) - Where that ray crosses the `Z=0` plane, as a `Vector2`. This is how you turn a mouse position into a 2D world position without any physics at all.
- [`ScreenPointToRay()`](xref:Stride.CommunityToolkit.Engine.CameraComponentExtensions.ScreenPointToRay(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector2)) - The near and far vectors of the ray, unprojected but not yet divided through by `w`.
- [`ScreenToWorldRaySegment()`](xref:Stride.CommunityToolkit.Engine.CameraComponentExtensions.ScreenToWorldRaySegment(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector2)) - The ray as a `RaySegment` from the near plane to the far plane, which is the form the Bepu and Bullet `Simulation` raycast helpers take.
- [`ScreenToWorldPoint()`](xref:Stride.CommunityToolkit.Engine.CameraComponentExtensions.ScreenToWorldPoint(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector3)) - Converts a screen position with a depth to a world position.
- [`WorldToScreenPoint()`](xref:Stride.CommunityToolkit.Engine.CameraComponentExtensions.WorldToScreenPoint(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector3)) - The inverse: where a world position lands on screen. Use this to hang a label on an object.
- [`WorldToClip()`](xref:Stride.CommunityToolkit.Engine.CameraComponentExtensions.WorldToClip(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector3@)) - Converts a world position to clip space, one step earlier than `WorldToScreenPoint()`.
- [`LogicDirectionToWorldDirection()`](xref:Stride.CommunityToolkit.Engine.CameraComponentExtensions.LogicDirectionToWorldDirection(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector2)) - Turns a 2D input direction into a world direction relative to where the camera is facing, so "forward" on a gamepad stick means forward on screen. An overload takes an explicit up vector.

> [!NOTE]
> Most of these have `in`/`out` overloads that avoid copying vectors, and `WorldToScreenPoint()` has one
> taking a `GraphicsDevice` when you want pixels rather than normalized coordinates. See the
> [API reference](xref:Stride.CommunityToolkit.Engine.CameraComponentExtensions) for the full set.

## Raycasting - Bepu

In the `Stride.CommunityToolkit.Bepu` namespace.

- [`Raycast()`](xref:Stride.CommunityToolkit.Bepu.CameraComponentExtensions.Raycast(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector2,System.Single,Stride.BepuPhysics.HitInfo@,Stride.BepuPhysics.CollisionMask)) - Casts from the camera through a screen position and reports the first hit as a `HitInfo`.
- [`RaycastMouse()`](xref:Stride.CommunityToolkit.Bepu.CameraComponentExtensions.RaycastMouse(Stride.Engine.CameraComponent,Stride.Engine.ScriptComponent,System.Single,Stride.BepuPhysics.HitInfo@,Stride.BepuPhysics.CollisionMask)) - The same, reading the mouse position for you.

## Raycasting - Bullet

In the `Stride.CommunityToolkit.Bullet` namespace. Both take either a `ScriptComponent` or a `Simulation` and return a `HitResult`.

- [`Raycast()`](xref:Stride.CommunityToolkit.Bullet.CameraComponentExtensions.Raycast(Stride.Engine.CameraComponent,Stride.Engine.ScriptComponent,Stride.Core.Mathematics.Vector2,Stride.Physics.CollisionFilterGroups,Stride.Physics.CollisionFilterGroupFlags)) - Casts from the camera through a screen position, filtered by collision group.
- [`RaycastMouse()`](xref:Stride.CommunityToolkit.Bullet.CameraComponentExtensions.RaycastMouse(Stride.Engine.CameraComponent,Stride.Engine.ScriptComponent,Stride.Physics.CollisionFilterGroups,Stride.Physics.CollisionFilterGroupFlags)) - The same, reading the mouse position for you.
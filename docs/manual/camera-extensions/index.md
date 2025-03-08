# CameraComponentExtensions.cs

![Done](https://img.shields.io/badge/status-done-green)

These extensions provide powerful and convenient ways to interact with the camera, transforming coordinates between different spaces, performing raycasting, and handling user inputs for dynamic camera control.

**Bepu Physics:**

- [`Raycast()`](xref:Stride.CommunityToolkit.Bepu.CameraComponentExtensions.Raycast(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector2,System.Single,Stride.BepuPhysics.HitInfo@,Stride.BepuPhysics.CollisionMask)) - Performs a raycasting operation from the specified CameraComponent's position through the specified screen position in world coordinates, and returns information about the hit result
- [`RaycastMouse()`](xref:Stride.CommunityToolkit.Bepu.CameraComponentExtensions.RaycastMouse(Stride.Engine.CameraComponent,Stride.Engine.ScriptComponent,System.Single,Stride.BepuPhysics.HitInfo@,Stride.BepuPhysics.CollisionMask)) - Performs a raycasting operation from the specified CameraComponent's position through the mouse cursor position in screen coordinates, and returns information about the hit result

**Bullet Physics:**

- Bullet Physics - [`Raycast(ScriptComponent component, ..)`](xref:Stride.CommunityToolkit.Bullet.CameraComponentExtensions.Raycast(Stride.Engine.CameraComponent,Stride.Engine.ScriptComponent,Stride.Core.Mathematics.Vector2,Stride.Physics.CollisionFilterGroups,Stride.Physics.CollisionFilterGroupFlags)), [`Raycast(Simulation simulation, ..)`](xref:Stride.CommunityToolkit.Bullet.CameraComponentExtensions.Raycast(Stride.Engine.CameraComponent,Stride.Physics.Simulation,Stride.Core.Mathematics.Vector2,Stride.Physics.CollisionFilterGroups,Stride.Physics.CollisionFilterGroupFlags)) - Performs a raycasting operation from the specified CameraComponent's position through the specified screen position in world coordinates, and returns information about the hit result
- Bullet Physics - [`RaycastMouse(ScriptComponent component, ..)`](xref:Stride.CommunityToolkit.Bullet.CameraComponentExtensions.RaycastMouse(Stride.Engine.CameraComponent,Stride.Engine.ScriptComponent,Stride.Physics.CollisionFilterGroups,Stride.Physics.CollisionFilterGroupFlags)), [`RaycastMouse(Simulation simulation, ..)`](xref:Stride.CommunityToolkit.Bullet.CameraComponentExtensions.RaycastMouse(Stride.Engine.CameraComponent,Stride.Physics.Simulation,Stride.Core.Mathematics.Vector2,Stride.Physics.CollisionFilterGroups,Stride.Physics.CollisionFilterGroupFlags)) - Performs a raycasting operation from the specified CameraComponent's position through the mouse cursor position in screen coordinates, and returns information about the hit result


**Physics independent extension methods:**

- [`GetPickRay()`](xref:Stride.CommunityToolkit.Engine.CameraComponentExtensions.GetPickRay(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector2)) - Calculates a ray from the camera through a point on the screen in world space
- [`LogicDirectionToWorldDirection()`](xref:Stride.CommunityToolkit.Engine.CameraComponentExtensions.LogicDirectionToWorldDirection(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector2)) - Converts a 2D logical direction into a 3D world direction relative to the camera's orientation
- [`LogicDirectionToWorldDirection()`](xref:Stride.CommunityToolkit.Engine.CameraComponentExtensions.LogicDirectionToWorldDirection(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector2,Stride.Core.Mathematics.Vector3)) - Converts a 2D logical direction into a 3D world direction relative to the camera's orientation, using a specified up vector
- [`ScreenPointToRay()`](xref:Stride.CommunityToolkit.Engine.CameraComponentExtensions.ScreenPointToRay(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector2)) - Calculates the near and far vectors for a ray that starts at the camera and passes through a given screen point
- [`ScreenToWorldPoint()`](xref:Stride.CommunityToolkit.Engine.CameraComponentExtensions.ScreenToWorldPoint(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector3@)) - Converts the screen position to a point in world coordinates
- [`ScreenToWorldRaySegment()`](xref:Stride.CommunityToolkit.Engine.CameraComponentExtensions.ScreenToWorldRaySegment(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector2)) - Converts the screen position to a `RaySegment` in world coordinates 
- [`ScreenToWorldRaySegment()`](xref:Stride.CommunityToolkit.Engine.CameraComponentExtensions.ScreenToWorldRaySegment(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector2@,Stride.CommunityToolkit.Scripts.RaySegment@)) - Converts the screen position to a `RaySegment` in world coordinates 
- [`WorldToClip()`](xref:Stride.CommunityToolkit.Engine.CameraComponentExtensions.WorldToClip(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector3@)) - Converts the world position to clip space coordinates relative to camera
- [`WorldToScreenPoint()`](xref:Stride.CommunityToolkit.Engine.CameraComponentExtensions.WorldToScreenPoint(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector3@)) - Converts the world position to screen space coordinates relative to camera

Each of these methods is designed to offer streamlined, high-level operations that simplify camera manipulation tasks, allowing you to focus on creating immersive and dynamic 3D environments.
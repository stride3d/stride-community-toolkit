# Physics Extensions

Stride ships two physics engines, and the toolkit wraps both. The methods below come in matching pairs - `Stride.CommunityToolkit.Bepu` and `Stride.CommunityToolkit.Bullet` define many of the same names - so **import one namespace or the other, not both**. A third, Box2D.NET, is not a Stride engine at all but a 2D world the toolkit runs beside Stride; its wrapper is at the [end of this page](#box2d).

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

A raycast sees colliders only. For what the eye sees under a screen point - a mesh with no body, an instanced model, a teapot - the Effects package has a [GPU picker](../rendering/gpu-picking.md) that reads the answer from the rendered frame.

### Grabbing bodies

- [`GrabberScript`](xref:Stride.CommunityToolkit.Bepu.GrabberScript) - `game.AddGrabber()` puts it on the camera entity, or add it there in Game Studio under Physics, and any dynamic body can be picked up with the mouse, carried on the end of the camera ray, and thrown. The wheel changes the carry distance; <kbd>T</kbd> plus mouse movement turns the held body. It works through a linear and an angular servo constraint rather than by moving the body, so the held body still collides and pushes, and its force caps scale with mass so heavy and light bodies drag alike. [`Grab()`](xref:Stride.CommunityToolkit.Bepu.GrabberScript.Grab(Stride.BepuPhysics.BodyComponent,Stride.Core.Mathematics.Vector3,System.Single)) and [`Release()`](xref:Stride.CommunityToolkit.Bepu.GrabberScript.Release) do the same from code.

```csharp
game.AddGrabber();
```

Every constraint example carries it, so the constrained bodies can be pulled about and the constraints watched doing their work.

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
## Box2D

In `Stride.CommunityToolkit.Box2D`, for a 2D world run by Box2D.NET beside Stride. The wrapper is built around `Box2DSimulation`, which owns the world and steps it; see the Box2D examples for the whole pattern.

### Joints

- [`Joints2D`](xref:Stride.CommunityToolkit.Box2D.Joints2D) - Every Box2D joint type with world-space anchors: `CreateRevolute`, `CreatePrismatic`, `CreateWheel`, `CreateDistance`, `CreateWeld`, `CreateMotor`, `CreateFilter`, plus `Destroy`, `IsValid` and `GetAnchors` for drawing. A raw definition wants each anchor as a local frame in the body's own space; these take the pivot and the axis in world space and work the frames out, with the pose at creation as the joint's zero.
- [`Box2DSimulation.Joints`](xref:Stride.CommunityToolkit.Box2D.SimulationJoints2D) - The same factories with the world id filled in, and overloads that take the entities the simulation created bodies for.
- Options records - [`RevoluteJointOptions`](xref:Stride.CommunityToolkit.Box2D.RevoluteJointOptions), [`PrismaticJointOptions`](xref:Stride.CommunityToolkit.Box2D.PrismaticJointOptions), [`WheelJointOptions`](xref:Stride.CommunityToolkit.Box2D.WheelJointOptions), [`DistanceJointOptions`](xref:Stride.CommunityToolkit.Box2D.DistanceJointOptions), [`WeldJointOptions`](xref:Stride.CommunityToolkit.Box2D.WeldJointOptions), [`MotorJointOptions`](xref:Stride.CommunityToolkit.Box2D.MotorJointOptions) - mirror the per-type knobs under Box2D's names. A property left null keeps Box2D's default, so an initialiser changes exactly what it names.

```csharp
var hinge = simulation.Joints.CreateRevolute(post, arm, pivot, new RevoluteJointOptions
{
    EnableMotor = true,
    MotorSpeed = 2,
    MaxMotorTorque = 500,
});
```

### Debug drawing

- [`Box2DDebugDraw`](xref:Stride.CommunityToolkit.Box2D.Box2DDebugDraw) - Box2D's own debug draw, rendered through a `ShapeBatch`: one `Draw(simulation)` call a frame shows every shape, joint, contact point, force, bounding box, island and body name the testbed can, with the testbed's toggles as properties. Shapes and joints are on by default; switch `DrawShapes` off when your entities already draw themselves.

```csharp
var debugDraw = new Box2DDebugDraw(shapeBatch) { DrawShapes = false, DrawContactPoints = true };

// each frame, after the simulation update
debugDraw.Draw(simulation);
```

### Terrain

- [`ShapeFixtureBuilder.AttachChain()`](xref:Stride.CommunityToolkit.Box2D.ShapeFixtureBuilder.AttachChain(Stride.Core.Mathematics.Vector2[],Box2D.NET.B2BodyId,System.Boolean,System.Nullable{System.Single})) - A chain of segments for terrain, tracks and room walls: unlike a row of separate segments it has no internal corners for a rolling body to catch on. Two rules Box2D imposes, both handled or documented: a chain collides on the right of its direction of travel, so a floor is listed right to left; and an open chain needs a ghost point at each end, which the method adds for you.
- [`SvgPath2D.Parse()`](xref:Stride.CommunityToolkit.Box2D.SvgPath2D.Parse(System.String,Stride.Core.Mathematics.Vector2,System.Single,System.Boolean)) - The straight-line commands of an SVG path as points, so a level outline drawn in Inkscape becomes a chain: offset, scale, and the y flip from SVG's downward axis. Curves are refused; flatten them in the editor.

### Queries

- [`PhysicsQueries2D.CastCircleClosest()`](xref:Stride.CommunityToolkit.Box2D.PhysicsQueries2D.CastCircleClosest(Box2D.NET.B2WorldId,Stride.Core.Mathematics.Vector2,System.Single,Stride.Core.Mathematics.Vector2,Box2D.NET.B2QueryFilter)) and [`CastSegmentClosest()`](xref:Stride.CommunityToolkit.Box2D.PhysicsQueries2D.CastSegmentClosest(Box2D.NET.B2WorldId,Stride.Core.Mathematics.Vector2,Stride.Core.Mathematics.Vector2,Stride.Core.Mathematics.Vector2,Box2D.NET.B2QueryFilter)) - Shape casts: sweep a circle, a point or a segment along a displacement and get the closest hit with its point, normal and fraction, filtered by category bits. `CastShapeClosest()` takes any convex proxy. The same calls exist on `Box2DSimulation`, alongside the raycasts and overlaps.

### Characters

- [`CharacterMover2D`](xref:Stride.CommunityToolkit.Box2D.CharacterMover2D) - A platformer character with no rigid body, on Box2D v3's mover API: a capsule the game moves itself, Quake style, that asks the world only what it touches - collect the contact planes, solve a translation that respects them, sweep it - with a pogo shape cast from the feet that floats it above the ground and decides whether it is standing. Register it with the simulation and it steps after every fixed physics step; set `Throttle` and call `Jump()` from your update; give it an `Entity` and it moves the transform. How hard a shape pushes back is per shape through `SetResponse()`: a small push limit makes a soft obstacle the mover walks through, an elevator gets a firm one and velocity clipping so it carries the mover.

```csharp
var mover = new CharacterMover2D(new Vector2(2, 8)) { Entity = hero };
simulation.RegisterSimulationUpdate(mover);

// each frame
mover.Throttle = (input.IsKeyDown(Keys.D) ? 1 : 0) - (input.IsKeyDown(Keys.A) ? 1 : 0);
if (input.IsKeyPressed(Keys.Space)) mover.Jump();
```

### Explosions

- [`Box2DSimulation.Explode()`](xref:Stride.CommunityToolkit.Box2D.Box2DSimulation.Explode(Stride.Core.Mathematics.Vector2,System.Single,System.Single,System.Single,System.UInt64)) - A radial blast: every shape within the radius gets an impulse away from the centre, per metre of its perimeter facing the blast, fading over the falloff distance beyond the radius.

### Grabbing bodies

- [`Grabber2DScript`](xref:Stride.CommunityToolkit.Box2D.Grabber2DScript) - The 2D counterpart of `GrabberScript`: on the camera entity, given the simulation, the left button picks any dynamic body up, drags it and throws it. Box2D v3 has no mouse joint, so it uses a kinematic anchor moved to the cursor and a motor joint with a mass-scaled force cap and angular friction, the idiom of Box2D's own sample browser. Every Box2D example carries it.

```csharp
game.GetCameraEntity().Add(new Grabber2DScript { Simulation = simulation });
```
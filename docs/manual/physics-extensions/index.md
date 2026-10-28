# Physics Extensions

Stride ships two physics engines, and the toolkit wraps both. The scene setup chooses the engine, `new SceneSetup3D().UseBepu()` or `.UseBullet()`, and the core's `CreatePrimitive`, `Add3DGround` and `Add2DGround` attach that engine's bodies and colliders; each package's entity extensions attach its physics explicitly, so both namespaces can be imported together. A third, Box2D.NET, is not a Stride engine at all but a 2D world the toolkit runs beside Stride; its wrapper is at the [end of this page](#box2d).

Bepu is the newer engine and the toolkit's default; the examples choose it with `UseBepu()` unless they say otherwise.
Bullet is maintained, not extended: the package follows every toolkit change and its three examples stay, but new helpers and examples are written for Bepu, the engine Stride itself is moving to.

> [!TIP]
> New to Bepu, or seeing a mesh that moves without colliding? Read
> [Bepu: Who Owns the Transform?](bepu-transform-ownership.md) first. It covers the one-way
> physics-to-transform sync and the silent failures that follow from it.
>
> Building joints or motors? [Bepu: Why Isn't My Constraint Doing Anything?](bepu-constraints.md)
> covers the equivalent silent failures on the constraint side - jammed joints, motors that produce
> no force, and settings that are discarded without warning.
>
> Want something to bounce or slide? [Bepu: Bounce and Friction](bepu-contact-materials.md) lists
> the four contact properties, their defaults, and how two collidables combine them.

## Bepu

### Scene setup

- [`UseBepu()`](xref:Stride.CommunityToolkit.Bepu.SceneSetupExtensions.UseBepu``1(``0)) - Makes Bepu the physics engine of a [`SceneSetup3D`](xref:Stride.CommunityToolkit.Engine.SceneSetup3D) or [`SceneSetup2D`](xref:Stride.CommunityToolkit.Engine.SceneSetup2D): `game.Run(new SceneSetup3D().UseBepu(), start: Start)` is a scene you can fly around, with a ground that collides.
- [`BepuPhysicsSetup`](xref:Stride.CommunityToolkit.Bepu.BepuPhysicsSetup) - What `UseBepu()` sets: the core's `CreatePrimitive` then attaches what the options' `Body` asks for, a `BodyComponent` (a `Body2DComponent` for a flat shape) or a `StaticComponent`, with the collider fitted to the shape; the grounds are static.
- `Add3DGround()` and `Add2DGround()` are the core's; with Bepu chosen, the ground gets a static collider on its own, when you do not want the rest of the scene setup.

### Primitives with colliders

- `CreatePrimitive(Primitive3D)` and `CreatePrimitive(Primitive2D)` are the core's; with Bepu chosen, the entity comes with a matching Bepu collider. `Body` on any options picks the kind: `Dynamic` (the default), `Static`, `Kinematic` or `None`. A [`BepuPhysicsOptions`](xref:Stride.CommunityToolkit.Bepu.BepuPhysicsOptions) adds `Mass`, the mass of the fitted collider shape, where Bepu keeps it; `Interpolation`, on by default, so a body moves smoothly between physics steps; and `Component`, a collidable of your own, used as it is. A 2D shape's collider is as deep as the shape says, `Primitive2D.Circle(0.5f, depth: 2f)`.

```csharp
var crate = game.CreatePrimitive(Primitive3D.Cube());                                                  // a dynamic body
var floor = game.CreatePrimitive(Primitive3D.Box(new Vector3(10, 0.2f, 10)), new BepuPhysicsOptions { Body = BodyMode.Static });
var lift = game.CreatePrimitive(Primitive3D.Cube(), new BepuPhysicsOptions { Body = BodyMode.Kinematic });
var prop = game.CreatePrimitive(Primitive3D.Teapot(), new BepuPhysicsOptions { Body = BodyMode.None }); // the model alone
```

When the body needs more than the mode gives, friction, a contact handler, a collision layer, shapes of your own, supply the component and it is used as it is. The mode is then ignored; an empty `CompoundCollider` on it still gets the fitted shape, and a compound that holds shapes keeps them. Options with a supplied component serve one entity, since a component belongs to one.

```csharp
var lift = game.CreatePrimitive(Primitive3D.Cube(), new BepuPhysicsOptions
{
    Component = new BodyComponent { Kinematic = true, FrictionCoefficient = 0.9f, Collider = new CompoundCollider() },
});
```

### Collision filtering

Which pairs of bodies collide is Bepu's own, in two rules, one on top of the other. The two examples show them side by side and say when each fits.

- **Layers and the matrix.** Every collidable sits on one `CollisionLayer`, of 32, and the simulation's `CollisionMatrix` says for each pair of layers whether they collide. A `CollisionMask` is a set of layers; the raycast and shape-query helpers below take one, so a query obeys the same table as the solver. [Collision Layer](../code-only/examples/collision-layer.md) drops a cube of every layer onto a shelf of every layer, with the matrix drawn beside the scene and a key that flips each pair.
- **Groups and the index rule.** Inside a layer, a `CollisionGroup` is a number rule: bodies with the same group `Id` ignore each other when their `IndexA` values are the same or next to each other, and collide when two or more apart; `IndexB` and `IndexC` add two more such axes, and `Id` 0 is no group. [Collision Group](../code-only/examples/collision-group.md) runs two teams of spheres at each other in three lanes and hangs a chain whose neighbouring links are ignored and whose two ends still collide.

| The rule you have | Reach for | As in |
|---|---|---|
| A table of named pairs: players never touch enemies, debris ignores shots | Layers and the matrix | Players, enemies, shots and debris on their shelves |
| A formula over many bodies of one kind: teammates pass through each other, the pieces of one object ignore each other | Groups, one `Id` per team or object | A team, a chain, the debris of one explosion |
| A query that sees some layers and not others: the ground but not the units | A `CollisionMask` on the raycast or the shape query | A pick that ignores enemies |

The rules are read as pairs meet, so both examples drop or wake their bodies after a change, and the new rule is applied from a fresh contact.

### Raycasting

- [`Raycast()`](xref:Stride.CommunityToolkit.Bepu.CameraComponentExtensions.Raycast(Stride.Engine.CameraComponent,Stride.Core.Mathematics.Vector2,System.Single,Stride.BepuPhysics.HitInfo@,Stride.BepuPhysics.CollisionMask)) - Casts from the camera through a screen position and reports the first hit.
- [`RaycastMouse()`](xref:Stride.CommunityToolkit.Bepu.CameraComponentExtensions.RaycastMouse(Stride.Engine.CameraComponent,Stride.Engine.ScriptComponent,System.Single,Stride.BepuPhysics.HitInfo@,Stride.BepuPhysics.CollisionMask)) - The same, reading the mouse position for you.
- [`BepuSimulation.RayCast()`](xref:Stride.CommunityToolkit.Bepu.SimulationExtensions.RayCast(Stride.BepuPhysics.BepuSimulation,Stride.CommunityToolkit.Mathematics.RaySegment@,Stride.BepuPhysics.HitInfo@,Stride.BepuPhysics.CollisionMask)) - Casts a `RaySegment` directly, with no camera and no maximum distance to choose: the segment's own length is the limit. Pairs with `ScreenToWorldRaySegment()`.
- [`RayCastPenetrating()`](xref:Stride.CommunityToolkit.Bepu.SimulationExtensions.RayCastPenetrating(Stride.BepuPhysics.BepuSimulation,Stride.CommunityToolkit.Mathematics.RaySegment@,System.Collections.Generic.ICollection{Stride.BepuPhysics.HitInfo},Stride.BepuPhysics.CollisionMask)) - Returns every hit along the segment rather than stopping at the first. An overload fills a collection you supply, to avoid allocating.

A raycast sees colliders only. For what the eye sees under a screen point - a mesh with no body, an instanced model, a teapot - the Effects package has a [GPU picker](../rendering/gpu-picking.md) that reads the answer from the rendered frame.

### Shape queries

A raycast asks what a line meets first. Bepu also answers what a convex shape meets, in place or along a path, with the same `HitInfo` a raycast gives:

- `BepuSimulation.Overlap(shape, pose, buffer)` lists every collidable the shape intersects at that pose. The `Span<CollidableStack>` overload writes into stack memory and allocates nothing; the `ICollection<OverlapInfo>` overload appends records that also carry the penetration direction and depth of each.
- `BepuSimulation.SweepCast(shape, pose, velocity, maxDistance, out hit)` throws the shape along the velocity and reports the first contact: the point, the normal, the collidable and how far along the path it happened. `SweepCastPenetrating` collects every contact on the way.

```csharp
Span<CollidableStack> buffer = stackalloc CollidableStack[32];

foreach (var collidable in simulation.Overlap(new Box(2f, 1.5f, 2f), new RigidPose(centre, Quaternion.Identity), buffer))
{
    // Every body and static the box intersects right now
}

if (simulation.SweepCast(new Sphere(0.35f), new RigidPose(start, Quaternion.Identity), new BodyVelocity(direction, Vector3.Zero), 16f, out var hit))
{
    var touch = start + direction * hit.Distance;   // Where the sphere's centre is when it first touches hit.Collidable
}
```

The shapes are Bepu's own (`BepuPhysics.Collidables.Box`, `Sphere`, `Capsule`, a convex hull), not the toolkit's colliders. `maxDistance` and `hit.Distance` are measured along the velocity: with a unit direction they are metres, with a real velocity they are seconds. A sweep is a straight line; gravity is not part of it. `E05_3D_ShapeQueries` shows both queries, and `E05_3D_Raycast` shows `RayCastPenetrating` listing every hit along a ray.

### Triggers and contact events

A collidable's `ContactEventHandler` is told about its contacts, and the handler decides whether the collidable is solid:

- `Trigger` is the engine's ready-made handler for a sensor volume. Its `NoContactResponse` is true, so bodies pass through as if the collidable were not there, and `OnEnter` and `OnLeave` fire with the other collidable. A static with a `BoxCollider` and no model is a zone.
- Your own `IContactHandler` returns `NoContactResponse` false for a solid that also wants to know: `OnStartedTouching`, `OnTouching` and `OnStoppedTouching` receive the `Contacts`, with `Other`, every contact's point, normal and depth, and `ComputeImpactForce(contact)` for how hard a first touch was.

```csharp
var trigger = new Trigger();

trigger.OnEnter += (_, other) => Console.WriteLine($"{other.Entity.Name} came in");
trigger.OnLeave += (_, other) => Console.WriteLine($"{other.Entity.Name} left");

var zone = new Entity("Zone")
{
    new StaticComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider { Size = new Vector3(3f, 2f, 3f) } } }, ContactEventHandler = trigger },
};
```

The callbacks run on the main thread after the physics step, so they may change entities and materials. `OnTouching` does not fire for sleeping pairs. One body can touch through several contacts and several children of a compound, so keep a set of who touches, not a count. `E05_3D_TriggerZones` shows a zone, a solid plate that counts what stands on it, and a crate that measures its hits.

### The simulation clock

`BepuSimulation` runs on a clock of its own, in fixed steps, and three of its properties can change while the game runs:

- `TimeScale` runs it slower or faster than the game. It stacks with `GameTime.Factor`, so a game that slows its whole clock slows its physics with it.
- `Enabled` pauses it; bodies hang where they are until it is set again.
- `PoseGravity` takes effect on the next step, on every body.

A simulation running faster than the game needs more fixed steps per frame: at four times the speed and sixty frames a second it needs four, and the default `MaxStepPerFrame` of three lets it fall behind the clock. Raise the cap when a game runs fast. To read the simulation's clock exactly, give a component `ISimulationUpdate`: it is called once per fixed step with the step's length, before the step and after it. `E05_3D_TimeControl` puts a dropper on the game clock and a spinning bar on the physics clock, so the difference can be watched.

### Putting a body back

- [`TeleportAtRest()`](xref:Stride.CommunityToolkit.Bepu.BodyComponentExtensions.TeleportAtRest(Stride.BepuPhysics.BodyComponent,Stride.Core.Mathematics.Vector3,System.Nullable{Stride.Core.Mathematics.Quaternion})) - Teleports a body to a pose and leaves it still and awake. `Teleport()` alone keeps the body's velocity, its spin and its sleep, so a reset key or a respawn wants this one.

### Grabbing bodies

- [`GrabberScript`](xref:Stride.CommunityToolkit.Bepu.GrabberScript) - `game.AddGrabber()` puts it on the camera entity, or add it there in Game Studio under Physics, and any dynamic body can be picked up with the mouse, carried on the end of the camera ray, and thrown. The wheel changes the carry distance; <kbd>T</kbd> plus mouse movement turns the held body. The pick looks through trigger volumes, so a body inside a zone can still be grabbed; a solid static in front still hides what is behind it. It works through a linear and an angular servo constraint rather than by moving the body, so the held body still collides and pushes, and its force caps scale with mass so heavy and light bodies drag alike. [`Grab()`](xref:Stride.CommunityToolkit.Bepu.GrabberScript.Grab(Stride.BepuPhysics.BodyComponent,Stride.Core.Mathematics.Vector3,System.Single)) and [`Release()`](xref:Stride.CommunityToolkit.Bepu.GrabberScript.Release) do the same from code.

```csharp
game.AddGrabber();
```

The same call serves a 2D scene: under an orthographic camera the pick ray runs along the view, a `Body2DComponent` is carried across its plane, and the mouse wheel does nothing for a 2D body or under an orthographic camera, since depth there is the plane itself.

### Debug views

- [`AddPhysicsDebug()`](xref:Stride.CommunityToolkit.Bepu.GameExtensions.AddPhysicsDebug(Stride.Games.IGame,System.Nullable{Stride.Core.Mathematics.Color4},System.Boolean,System.Boolean,System.Boolean)) - One entity carrying Bepu's two scene-wide debug views: [`CollidableGizmoScript`](xref:Stride.CommunityToolkit.Bepu.CollidableGizmoScript) draws every collider on <kbd>P</kbd>, and [`DebugRenderComponentScript`](xref:Stride.CommunityToolkit.Bepu.DebugRenderComponentScript) turns the physics debug meshes on for every Bepu entity on <kbd>F11</kbd>. Both act on the whole scene, so one entity is all a scene needs; a second copy of either script answers the same key and toggles the first one back. `contactsVisible: true` adds the solver's contact points and normals to the debug meshes (Stride 4.4.1 or later).

```csharp
game.AddPhysicsDebug(gizmoColor: new Color4(0.4f, 0.843f, 0, 0.9f));
```

`E08_3D_CollidableGizmo` and `E08_3D_DebugRenderComponent` show one view each; `E05_3D_TriggerZones`, `E05_3D_ShapeQueries` and `E05_3D_MeshColliders` carry both.

### Convex hulls

In the `Stride.CommunityToolkit.Bepu` namespace, for giving an arbitrary mesh a collider.

- [`ToConvexHullCollider()`](xref:Stride.CommunityToolkit.Bepu.ConvexHullColliderExtensions.ToConvexHullCollider(Stride.Graphics.GeometricMeshData{Stride.Graphics.VertexPositionNormalTexture})) - Wraps mesh data in a single convex hull. Cheap, but it fills in any concave detail.
- [`ToDecomposedHulls()`](xref:Stride.CommunityToolkit.Bepu.ConvexHullColliderExtensions.ToDecomposedHulls(Stride.Graphics.GeometricMeshData{Stride.Graphics.VertexPositionNormalTexture})) - Splits the mesh into several hulls so concave shapes collide correctly. More accurate, more expensive to build.

### Mesh colliders

The engine's `MeshCollider` is the model's own triangles, so a hole stays a hole where a hull would fill it. It needs the model, so it is attached once the entity has one: create the primitive with `Body = BodyMode.None`, then add the component.

```csharp
var ring = game.CreatePrimitive(Primitive3D.Torus(1.2f, 0.35f), new BepuPhysicsOptions { Body = BodyMode.None });

ring.Add(new StaticComponent { Collider = new MeshCollider { Model = ring.Get<ModelComponent>().Model, Closed = true } });
```

What the triangles are not, measured in `E05_3D_MeshColliders`: a mesh is a surface with no inside, so a body that starts inside a closed mesh falls out through it; two meshes do meet, triangle against triangle, so a moving mesh works but costs more than a hull. `Closed` says the surface has no holes and is what the inertia of a moving mesh is computed from. The entity's scale scales the mesh.

## Bullet

### Scene setup

- [`UseBullet()`](xref:Stride.CommunityToolkit.Bullet.SceneSetupExtensions.UseBullet``1(``0)) - Makes Bullet the physics engine of a scene setup, the counterpart of `UseBepu()`.
- [`BulletPhysicsSetup`](xref:Stride.CommunityToolkit.Bullet.BulletPhysicsSetup) - What `UseBullet()` sets: the core's `CreatePrimitive` then attaches what the options' `Body` asks for, a `RigidbodyComponent`, kinematic or not, or a `StaticColliderComponent`, with the collider fitted to the shape; the grounds are static.
- [`AddInfinite3DGround()`](xref:Stride.CommunityToolkit.Bullet.GameExtensions.AddInfinite3DGround(Stride.Games.IGame,System.Nullable{Stride.Core.Mathematics.Vector2},Stride.CommunityToolkit.Bullet.BulletPhysicsOptions)) - A ground plane nothing can fall off the edge of. Bullet only; there is no Bepu equivalent.

### Primitives and colliders

- `CreatePrimitive(Primitive3D)` and `CreatePrimitive(Primitive2D)` are the core's; with Bullet chosen, the entity comes with a matching Bullet collider. `Body` on any options picks the kind, as with Bepu. A [`BulletPhysicsOptions`](xref:Stride.CommunityToolkit.Bullet.BulletPhysicsOptions) adds `Mass`, the mass of the rigid body, and `PhysicsComponent`, a component of your own, used as it is.
- [`AddBulletPhysics()`](xref:Stride.CommunityToolkit.Bullet.EntityExtensions.AddBulletPhysics(Stride.Engine.Entity,Stride.CommunityToolkit.Rendering.ProceduralModels.Primitive3D,Stride.CommunityToolkit.Bullet.BulletPhysicsOptions)) - Attaches a collider to an entity you already built, rather than creating both together.
- [`AddBulletPhysics()`](xref:Stride.CommunityToolkit.Bullet.EntityExtensions.AddBulletPhysics(Stride.Engine.Entity,Stride.CommunityToolkit.Rendering.ProceduralModels.Primitive2D,Stride.CommunityToolkit.Bullet.BulletPhysicsOptions)) - The 2D equivalent.

### Raycasting

- [`Raycast()`](xref:Stride.CommunityToolkit.Bullet.CameraComponentExtensions.Raycast(Stride.Engine.CameraComponent,Stride.Engine.ScriptComponent,Stride.Core.Mathematics.Vector2,Stride.Physics.CollisionFilterGroups,Stride.Physics.CollisionFilterGroupFlags)) - Casts from the camera through a screen position. Takes either a `ScriptComponent` or a `Simulation`.
- [`RaycastMouse()`](xref:Stride.CommunityToolkit.Bullet.CameraComponentExtensions.RaycastMouse(Stride.Engine.CameraComponent,Stride.Engine.ScriptComponent,Stride.Physics.CollisionFilterGroups,Stride.Physics.CollisionFilterGroupFlags)) - The same, reading the mouse position for you.
- [`Simulation.Raycast()`](xref:Stride.CommunityToolkit.Bullet.SimulationExtensions.Raycast(Stride.Physics.Simulation,Stride.CommunityToolkit.Mathematics.RaySegment)) - Casts a `RaySegment` directly, with no camera involved. Overloads also cast from an entity along a direction.
- [`RaycastPenetrating()`](xref:Stride.CommunityToolkit.Bullet.SimulationExtensions.RaycastPenetrating(Stride.Physics.Simulation,Stride.CommunityToolkit.Mathematics.RaySegment)) - Returns every hit along the ray rather than stopping at the first. An overload fills a list you supply, to avoid allocating.

### Debugging

- [`ShowColliders()`](xref:Stride.CommunityToolkit.Bullet.GameExtensions.ShowColliders(Stride.Games.IGame)) - Draws the collider shapes over the scene, which is how you find out that the collider is not where the model is.

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

### Collision groups

- [`Box2DCollisionMatrix`](xref:Stride.CommunityToolkit.Box2D.Box2DCollisionMatrix) - Which groups collide with which, as a symmetric table over positive group numbers; every pair collides until `SetCollision(a, b, false)` says otherwise. The simulation's own is [`Box2DSimulation.CollisionMatrix`](xref:Stride.CommunityToolkit.Box2D.Box2DSimulation.CollisionMatrix), and it is the one consulted.
- [`ShapeFixtureBuilder.InCollisionGroup()`](xref:Stride.CommunityToolkit.Box2D.ShapeFixtureBuilder.InCollisionGroup(Box2D.NET.B2ShapeDef,System.Int32)) - Puts a shape definition in a group: Box2D's `groupIndex`, with custom filtering switched on so the matrix is asked about that shape's pairs. Shapes in no group are untouched, and category and mask bits still apply first.

```csharp
simulation.CollisionMatrix.SetCollision(Debris, Player, false);

var debrisDef = ShapeFixtureBuilder.CreateDefaultShapeDef().InCollisionGroup(Debris);
ShapeFixtureBuilder.AttachShape(Primitive2D.Circle(0.2f), bodyId, debrisDef);
```

Configure the matrix between updates: the simulation reads it from its worker threads during a step.

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
game.AddGrabber2D(simulation);
```
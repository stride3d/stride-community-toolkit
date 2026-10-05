# Frequently asked questions

These are the questions people ask most often when they build a Stride game from code. Each
answer is short and links to the page that says more. The answers apply to Stride 4.4 and the
current toolkit. For build errors, see [Troubleshooting](troubleshooting.md). For a word you don't
know, see the [Glossary](glossary.md).

## Getting started

### Do I need Game Studio or Visual Studio?

No. You need the .NET SDK, and any editor works: Visual Studio, Rider or VS Code. A code-only game
creates its scenes in C# and never opens Game Studio. Game Studio runs only on Windows. Games run on
Windows and Linux, and Stride also targets Android and iOS. See [Code-Only](code-only/index.md).

### Can I write my game in F# or Visual Basic?

Yes. A Stride game is an ordinary .NET project. The toolkit has F# and Visual Basic examples, such
as [Basic examples in F#](code-only/examples/basic-examples-fs.md). Game Studio only finds C#
scripts, so if you use the editor, keep its components in C#.

### Can I use NuGet packages?

Yes. A game project is an SDK-style .NET project, so you add packages and project references as
usual. Stride has no built-in networking; use a .NET library instead. The
[SignalR example](code-only/examples/stride-signalr.md) shows one way.

### The build fails with "Detected package downgrade". Why?

All Stride packages in a project must have the same version. Update the one that is behind, or set
a single version property and use it for every Stride package reference.

### I get "'Game' is a namespace but is used like a type". Why?

Your namespace ends in `.Game`, so inside it `Game` names your namespace, not `Stride.Engine.Game`.
Rename the namespace, or write `Stride.Engine.Game` in full.

## Scripts, entities and scenes

### I added a script in code, but its Start method never runs. Why?

A script runs only while its entity is in a running scene. Add the script with
`entity.Add(new MyScript())`, and make sure the entity is in the scene, for example with
`entity.Scene = rootScene`.

### How do I get a reference to another entity?

The cheapest way is a public `Entity` or component property on your script, set when you create
the entity. To search by name, use [`FindEntity`](entity-extensions/index.md) on an entity, or
[`GetCamera`](xref:Stride.CommunityToolkit.Engine.SceneExtensions) for a camera. Search once and keep
the result; don't search every frame.

### Does `entity.Get<T>()` find derived component types?

Yes. It returns the first component that is a `T` or derives from `T`, or `null` if there is none.

### Is there an equivalent of Unity's FindObjectOfType?

No. For one entity and its children, use
[`GetComponentsInChildren<T>`](xref:Stride.CommunityToolkit.Engine.EntitySearchExtensions). For a
manager that other scripts need, register it as a service instead: call
`Services.AddService(this)` in its `Start` method and `Services.RemoveService<MyManager>()` in
`Cancel`. Other scripts then call `Services.GetService<MyManager>()`.

### Should I subclass `Entity` to add data such as hit points?

No. Put the data in a component and add the component to the entity. See
[Components and scripts](components-and-scripts.md).

### Is `entity.Transform` different from `entity.Get<TransformComponent>()`?

No. They are the same object. The entity keeps a direct reference to it for speed.

### How do I hide an entity, including its children?

Call `entity.EnableAll(false, applyOnChildren: true)`. It disables every component that can be
disabled, on the entity and on its children. To hide only the model, set the model component's
`Enabled` to `false`.

### How do I remove an entity?

Call the toolkit's [`Remove`](entity-extensions/index.md), or set `entity.Scene = null`. Dispose any
GPU resources you created for it yourself.

### Can I add entities from a background thread?

No. Scenes and entity collections are not thread-safe. Changing them from another thread causes
errors such as "Collection was modified". Build the entity on the worker thread if you like, then
add it to the scene from a script that runs on the game thread.

### My own class can't reach Content, Input or the scene. How do I give it access?

Those belong to scripts and to the game. Make the class a script, or pass it the `Game` or its
`Services` and get what it needs from there. Stride has no static global access.

### How do I get the time since the last frame?

Read `Game.UpdateTime.Elapsed` (a `TimeSpan`) and convert it with `.TotalSeconds`. Multiply speeds
by it, so movement doesn't depend on the frame rate. `Game.UpdateTime.Total` is the time since the
game started. See [Delta time](glossary.md).

### How do I make the game run in slow motion?

Set `Game.UpdateTime.Factor`. A value between 0 and 1 slows time down, and a value above 1 speeds
it up. The factor applies to physics, animations and particles. Bepu's `BepuSimulation.TimeScale`
adds its own factor on top of this one.

### How do I save the player's progress?

Stride has no save system. Write a plain data class to a file with a serializer such as
`System.Text.Json`. The [Cube Clicker example](code-only/examples/stride-ui-cube-clicker.md) saves
and loads its data under `VirtualFileSystem.ApplicationData`.

## Game Studio and serialization

### My component doesn't appear in Game Studio's Add component list. Why?

The class must be public and not abstract, and it must derive from a script or component type.
Build the game project, then reload assemblies in Game Studio. See
[Using the toolkit in Game Studio](game-studio.md).

### Loading fails with "No serializer available for type". Why?

A public member of a component has a type Stride can't serialize. Mark that member
`[DataMemberIgnore]`, or mark its type `[DataContract]`. Stride ignores `[Serializable]`. A list
property needs `[DataContract]` on its element type too.

### A property in Game Studio shows a different value from the one in my code. Why?

The scene stores the value set in the editor, and that value replaces your C# initializer when the
scene loads. Change the value in the property grid, or reset it there.

### I renamed a property, and the scene lost its value. How do I keep it?

The scene stores members by name. Add `[DataAlias("OldName")]` to the renamed member so that old
scenes still load the value.

## Transforms

### Why does `Transform.Position += offset` ignore my entity's rotation?

`Position` is in the parent's space, so the offset doesn't turn with the entity. Use the toolkit's
[`Translate`](xref:Stride.CommunityToolkit.Engine.TransformExtensions) with `Space.Self`, or rotate
the offset by `Transform.Rotation` first.

### Which way is forward?

Stride is right-handed with Y up, and forward is -Z. A ray in front of an entity starts at the
entity and points along `-Vector3.UnitZ`, rotated by the entity's rotation.

### Why does adding to `Transform.Rotation.Y` skew or scale my object?

`Rotation` is a quaternion, not a set of angles, so changing one of its components breaks it.
Multiply by a rotation instead, for example `Rotation *= Quaternion.RotationY(angle)`, or use the
toolkit's [`Rotate`](xref:Stride.CommunityToolkit.Engine.TransformExtensions). To turn an entity
toward a point, use [`LookAt`](xref:Stride.CommunityToolkit.Engine.TransformExtensions).

### Why is `WorldMatrix` out of date after I set `Position`?

The transform processor recomputes the matrices once per frame. Call `UpdateWorldMatrix()` if you
need the new value at once. If you write `LocalMatrix` or `WorldMatrix` yourself, the next update
overwrites it unless you set `UseTRS` to `false`.

### How big is one unit?

One unit is one metre. Physics and the default lighting assume that scale.

### How do I convert between a screen position and a world position?

Use the toolkit's camera extensions: `ScreenToWorldPoint`, `ScreenToWorldRaySegment` and
`WorldToScreenPoint`. See [Camera extensions](camera-extensions/index.md) and
[Picking](engine-patterns.md#picking-from-cursor-to-world-point).

## Rendering

### My code-only scene is blank. What is missing?

A scene needs a camera, a graphics compositor that renders it, and at least one light.
`game.SetupBase3D()` adds all three. With the Bepu package, `game.SetupBase3DScene()` also adds a
camera controller and a ground. See
[Code-Only](code-only/index.md).

### My procedurally built model doesn't show. Why?

The usual cause is a model with no material, which draws nothing. Add a material and check that
the mesh's `MaterialIndex` points to it. If the texture looks flat, check the UV calculation:
integer division turns every UV into 0. See [Mesh builder](rendering/mesh-builder.md).

### My procedural mesh disappears at some camera angles. Why?

Stride culls a mesh by its bounding box. Set the mesh's `BoundingBox` and `BoundingSphere` to cover
its vertices, or Stride may decide it is off screen.

### My vertex-coloured mesh is black. Why?

The material doesn't read the colour stream. Use `ComputeVertexStreamColor` as the diffuse colour.
Stride doesn't convert vertex colours from sRGB to linear, but it does convert colour constants. If
vertex colours look washed out next to constant ones, convert them yourself.

### Objects far away draw over near ones on my transparent material. Why?

A transparent material doesn't write depth, even when its alpha is 1. Use it only for surfaces
that are really transparent.

### How do I change a material's colour or texture while the game runs?

Set the material's parameters, not its descriptor; the descriptor is used only when the material
is built. See [Materials](rendering/materials.md).

### How do I draw text or shapes on top of the scene every frame?

Use [DebugOverlay](rendering/debug-overlay.md) for text and [ShapeBatch](rendering/shape-batch.md)
for 2D and 3D shapes. A `SpriteBatch` drawn from a script's `Update` method shows nothing, because
it has to run inside the graphics compositor. `DebugText.Print` lasts one frame, so call it every
frame.

### My pixel-art tiles have seams or look blurry. How do I fix them?

Use point sampling, and keep tile textures uncompressed. Block compression changes the colours at
tile edges.

### How do I draw thousands of the same object?

Use instancing: one mesh drawn many times in a single draw call. Thousands of entities cost
processing time each frame, even when they are culled. See the
[instancing example](code-only/examples/instancing.md).

### How do I save a screenshot?

Call [`TakeScreenShot`](game-extensions/index.md).

### Does Stride have LOD, terrain, tile maps or baked lighting?

None of these is built in. Stride has texture streaming, light probes, a skybox light and cel
shading. Terrain and tile maps are usually built in code, from a height map or from sprites.

### The game stutters the first time an object appears. Why?

Stride compiles each shader variation the first time it's needed. Later frames use the compiled
version.

### My laptop shows a black screen or a graphics profile error. Why?

The game may be running on the integrated GPU, or it may be asking for a graphics profile the
device doesn't support. Make the game use the discrete GPU in the graphics driver's settings or in
Windows graphics settings.

## Physics

### My entity stops moving after I add a physics body. Why?

The simulation owns a dynamic body's transform and overwrites what you set. Move the body with
forces or velocities, make it kinematic, or teleport it. See
[Who owns the transform?](physics-extensions/bepu-transform-ownership.md)

### What's the difference between a kinematic body and a character?

You move a kinematic body yourself. It pushes other bodies, but gravity and collisions don't move
it. A character has its own gravity and collision response, and it's meant for player and NPC
movement.

### Can an entity have a body and a static collider at the same time?

No. Give each entity one physics component. A dynamic body and a static collider on the same
entity give results such as boxes floating above the ground.

### My collider doesn't line up with my model. Why?

The collider is centred on the entity's origin, but the model's pivot may be somewhere else, for
example at the base of the mesh. Give the collider shape an offset, or move the pivot when you
export the model. Use [debug rendering](code-only/examples/debug-render-component.md) to see the
shapes.

### Can I put dynamic bodies under other dynamic bodies in the hierarchy?

No. Each body writes its own world transform, so a child body moves twice and drifts away from its
collider. Keep dynamic bodies at the root and connect them with
[constraints](physics-extensions/bepu-constraints.md).

### My raycast doesn't hit my model. Why?

A physics raycast hits colliders only. Give the model a collider, or use
[GPU picking](rendering/gpu-picking.md), which picks what is drawn. See the
[raycast example](code-only/examples/raycast.md).

### How do I make physics 2D?

With Bepu, use the toolkit's `Body2DComponent`, which keeps a body in the XY plane and lets it turn
only around Z. For a 2D-only engine, use the [Box2D package](code-only/examples/box2d-physics.md).
See [Physics](physics-extensions/index.md).

## Running and shipping

### How do I uncap the frame rate?

Call [`DisableVSync`](game-extensions/index.md). To set a limit instead, call
[`SetMaxFPS`](game-extensions/index.md).

### How do I ship extra files, such as data or textures, with the game?

Add them to the project with "Copy to output directory", and build their paths from
`AppContext.BaseDirectory`. To load a texture from a file, use the toolkit's `TextureLoader`.

### How do I change the game's icon?

Set `<ApplicationIcon>` in the project file to your `.ico` file.

### The debugger stops inside Stride, not in my code. Why?

By default, Visual Studio breaks only where an exception goes unhandled, which is often inside the
engine. Turn on "Common Language Runtime Exceptions" in Exception Settings, and the debugger will
stop where your code throws.
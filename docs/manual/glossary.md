# Glossary

The words the toolkit's manual, examples and release notes use, explained for someone who is new
to Stride, to game development, or to both. Each entry is a sentence or two and a link to the page
that says more: a toolkit page where the toolkit owns the idea, the
[Stride manual](https://doc.stride3d.net/latest/en/manual/index.html) where the engine does. Terms
are grouped by topic, the way the [Stride glossary](https://doc.stride3d.net/latest/en/manual/glossary/index.html)
is, and sorted alphabetically inside each group. Use your browser's find (Ctrl+F) to jump to a word.

## Getting around

- **Code-only**: An approach to building a Stride game in C# without using Game Studio. Scenes are created in code, and content can be generated or loaded from files; the toolkit's extensions keep that code short. See [Code-Only](code-only/index.md).
- **Example**: A small runnable project that teaches one idea, such as moving a camera or adding physics. Each has its own folder under `examples/code-only` and appears in the docs gallery and the launchers. See the [examples gallery](code-only/examples/index.md) and [contributing examples](../contributing/examples/index.md).
- **Extension method**: A C# static method called as if it belonged to another type, such as `game.AddGround()`. Nearly every toolkit feature is one, so the engine's own types stay as they are. See [Extensions](code-only/extensions.md).
- **File-based app**: A single `.cs` file run with `dotnet run app.cs`, with its packages declared in `#:package` lines at the top instead of a project file. See [Create a file-based app](code-only/create-file-based-app.md).
- **Game Studio**: Stride's editor for scenes, assets and settings. The toolkit works without it, and its components also work inside it. See [Using the Toolkit in Game Studio](game-studio.md) and the [Stride manual](https://doc.stride3d.net/latest/en/manual/game-studio/index.html).
- **Launcher**: The two toolkit programs that list and start every example, one in a console and one with a window, both reading the same generated manifest. See [Building the toolkit](../contributing/toolkit/building.md).
- **Manual**: These pages. The [Stride manual](https://doc.stride3d.net/latest/en/manual/index.html) covers the engine; this one covers what the toolkit adds, and explains engine behaviour that is easy to trip over when working from code.
- **NuGet package**: How the toolkit reaches a game: each library, such as `Stride.CommunityToolkit.Bepu`, is a package a project references. See [Create project](code-only/create-project.md).
- **Stride**: The open-source C# game engine the toolkit extends. See [stride3d.net](https://www.stride3d.net/) and [Key concepts](https://doc.stride3d.net/latest/en/manual/get-started/key-concepts.html).
- **Stride Community Toolkit**: A set of libraries, examples and documentation, maintained by the community, that make Stride easier to use from code. See [Getting started](getting-started.md).

## Engine building blocks

- **Component**: A piece of data or behaviour attached to an entity: a model, a camera, a physics body, a script. See [Components and scripts](components-and-scripts.md) and [Entity-component system](https://doc.stride3d.net/latest/en/manual/engine/entity-component-system/index.html).
- **Delta time**: The game time that passed since the previous update, read from `GameTime.Elapsed` (a `TimeSpan`; `.TotalSeconds` gives it in seconds). Multiply a speed by it to get how far to move in this update, so movement does not depend on frame rate. See [Delta time](https://doc.stride3d.net/latest/en/tutorials/csharpbeginner/delta-time.html) and [GameTime](https://doc.stride3d.net/latest/en/api/Stride.Games.GameTime.html).
- **Deterministic mode**: A fixed timestep with exactly one update per draw, so frame N is the same simulated instant every run; `game.SetDeterministic()` turns it on. Captures and golden images depend on it. See [Game extensions](game-extensions/index.md).
- **Entity**: The basic object in a scene: a name, a transform, and a list of components. On its own it does nothing. See [Entity extensions](entity-extensions/index.md) and the [Stride manual](https://doc.stride3d.net/latest/en/manual/engine/entity-component-system/index.html).
- **Entity-component system (ECS)**: The pattern Stride is built on: entities hold components, and processors handle the behaviour that components of one kind share. See [Components and scripts](components-and-scripts.md).
- **Fixed timestep**: Updating the simulation in equal time slices regardless of how fast frames arrive. Physics runs this way; deterministic mode makes the whole game do so.
- **Game loop**: The engine's repeating cycle: read input, update the world, draw a frame. `game.Run(start:, update:)` hands you the two moments that matter. See [Game extensions](game-extensions/index.md).
- **GameTime**: The clock object passed to every update: total time, time since the previous update, frame count. See the [API](https://doc.stride3d.net/latest/en/api/Stride.Games.GameTime.html).
- **Local space / world space**: Local coordinates are relative to an entity's parent; world coordinates are absolute. An entity's `Transform.WorldMatrix` converts one to the other. See [Engine patterns](engine-patterns.md).
- **Processor**: An engine system that tracks matching components and handles shared behaviour, such as updating them or preparing them for rendering. The right home for behaviour shared by many entities. See [Components and scripts](components-and-scripts.md) and [Components in Game Studio](../contributing/toolkit/game-studio-components.md).
- **Scene**: The tree of entities the game shows and updates. Code-only games build it in `start`; Game Studio saves it as an asset. See [Scenes](https://doc.stride3d.net/latest/en/manual/game-studio/scenes.html).
- **Script**: A component with code the engine calls: a `StartupScript` once, a `SyncScript` every frame, an `AsyncScript` as a long-running task. See [Script extensions](script-extensions/index.md) and [Types of script](https://doc.stride3d.net/latest/en/manual/scripts/types-of-script/index.html).
- **Script system**: The engine service that schedules scripts, and what `game.Script.AddTask(...)` and the toolkit's script-system extensions talk to. See [Script system extensions](script-system-extensions/index.md).
- **Transform**: The component every entity has: position, rotation and scale, plus the parent link. Moving an entity means writing to it. See [Engine patterns](engine-patterns.md).
- **World units**: Stride's distances are metres by convention; a cube of size 1 is one metre across. See [World units](https://doc.stride3d.net/latest/en/manual/game-studio/world-units.html).

## Content: models, materials, textures

- **Asset**: A file the engine compiles and loads: a model, texture, sound, scene. Code-only games make most content at runtime instead. See [Assets](https://doc.stride3d.net/latest/en/manual/assets/index.html).
- **Asset compiler**: The tool that turns assets into runtime data; even a code-only game runs it once, to copy the engine's shader sources into its `data/db` folder. See [Troubleshooting](troubleshooting.md).
- **Content manager**: `game.Content`, the loader for compiled assets by URL. Code-only games rarely need it. See [Asset URL](https://doc.stride3d.net/latest/en/manual/scripts/create-a-model-from-code.html).
- **Index buffer**: The list of vertex indices that says which vertices form each triangle, so a vertex shared by six triangles is stored once. See [MeshBuilder](rendering/mesh-builder.md).
- **Material**: How a surface reacts to light: colour, roughness, metalness, emission, transparency. In code it is built from a `MaterialDescriptor`; `game.CreateMaterial(...)` wraps the common case. See [Engine patterns](engine-patterns.md) and [Materials](https://doc.stride3d.net/latest/en/manual/graphics/materials/index.html).
- **Mesh**: Geometry the GPU draws: a vertex buffer, an index buffer and a layout. A model is one or more meshes with materials. See [MeshBuilder](rendering/mesh-builder.md).
- **Model**: The renderable asset an entity shows through its `ModelComponent`: meshes plus the materials they use. See [Model extensions](model-extensions/index.md).
- **Prefab**: A saved entity tree instantiated many times, in the editor or from code. See [Entity extensions](entity-extensions/index.md) and [Prefabs](https://doc.stride3d.net/latest/en/manual/game-studio/prefabs/index.html).
- **Primitive**: A built-in shape such as a cube, sphere, capsule, cone or torus, generated from a few numbers. `game.Create3DPrimitive(...)` builds one with a material; the overloads in the physics packages can also add a collider. See [Engine patterns](engine-patterns.md).
- **Procedural model**: A model computed from parameters at runtime, with no file behind it; every primitive is one, and the toolkit adds 2D and lettering variants. See [Engine patterns](engine-patterns.md).
- **Runtime font**: A font whose character images are drawn as they are needed, from a system or embedded TTF file, with no font asset prepared beforehand. Several of the toolkit's text features use one, so a code-only game needs no font file of its own. See [Engine patterns](engine-patterns.md).
- **Skybox**: The picture of the surroundings, drawn behind everything and used to light reflective surfaces. `Stride.CommunityToolkit.Skyboxes` carries a ready one. See [Skyboxes](https://doc.stride3d.net/latest/en/manual/graphics/textures/skyboxes-and-backgrounds.html).
- **Sprite font**: Draws text using character images or distance-field data stored in textures. Characters can be prepared before the game runs or generated as needed. See [Sprite fonts](https://doc.stride3d.net/latest/en/manual/graphics/sprite-fonts.html).
- **Texture**: An image on the GPU: a picture on a surface, a data table for a shader, or a target to draw into. See [TextureCanvas](rendering/texture-canvas.md) and [Textures](https://doc.stride3d.net/latest/en/manual/graphics/textures/index.html).
- **Vertex**: One corner of a triangle, carrying a position and whatever else the shader needs: a normal, a colour, texture coordinates. See [MeshBuilder](rendering/mesh-builder.md).
- **Vertex buffer**: The GPU array of vertices a mesh draws from, laid out by its vertex declaration. See [MeshBuilder](rendering/mesh-builder.md).

## Rendering

- **Billboard**: A flat shape that always turns to face the camera, so a sprite or a glow never shows its edge. Used by particles, ShapeBatch and world text. See [ShapeBatch](rendering/shape-batch.md).
- **Charts**: The toolkit's line, scatter and 3D charts, drawn through ShapeBatch and updated live. See [Charts](rendering/charts.md).
- **Compositor** (graphics compositor): The description of how a frame is rendered: which cameras, in which order, through which renderers, into which targets. The toolkit's `Add*` calls edit it for you. See [Graphics compositor](https://doc.stride3d.net/latest/en/manual/graphics/graphics-compositor/index.html).
- **CPU / GPU**: The CPU (central processing unit) is the computer's main processor, which runs your C# code. The GPU (graphics processing unit) is the graphics card's processor, which runs shaders and draws the picture by doing many simple calculations at once.
- **Debug overlay**: The toolkit's block of text lines in a screen corner, for keys and readouts; every example's on-screen help uses it. See [Debug overlay](rendering/debug-overlay.md).
- **Debug shapes**: Immediate-mode lines, boxes, spheres and arrows drawn for a frame or a duration, for seeing what the code thinks. See [Debug shapes](rendering/debug-shapes.md).
- **Depth buffer**: The per-pixel record of how far the nearest drawn surface is, which is how a near object hides a far one. Soft particles and the depth fade read it. See [Rendering pipeline](https://doc.stride3d.net/latest/en/manual/graphics/rendering-pipeline/index.html).
- **Depth fade** (soft edge): Fading a transparent shape where it meets solid geometry, so a glow or a puff of smoke does not cut the floor with a hard line. See [ShapeBatch](rendering/shape-batch.md).
- **Display scale** (DPI): The operating system's zoom, such as 150%, that the toolkit's text and pixel sizes follow so a HUD reads the same on every monitor. See [Debug overlay](rendering/debug-overlay.md).
- **Draw call**: One command to the GPU to draw a mesh. Thousands per frame are expensive; instancing and batching cut them. See [Instancing](code-only/examples/instancing.md).
- **Entity text**: A screen-space label pinned to an entity, readable at any distance; the gallery's numbered labels. See [Entity text](rendering/entity-text.md).
- **Forward renderer**: Stride's default way to draw a frame: each mesh is shaded with its lights as it is drawn. See [Rendering pipeline](https://doc.stride3d.net/latest/en/manual/graphics/rendering-pipeline/index.html).
- **GPU picking**: Answering "what is under this screen point" by drawing entity ids into a hidden image and reading one pixel back, so it works without colliders. See [GPU picking](rendering/gpu-picking.md).
- **Glow**: In ShapeBatch and charts, a soft halo of a given pixel width outside a shape's edge; not the engine's bloom. See [ShapeBatch](rendering/shape-batch.md).
- **HDR**: High dynamic range: rendering with colour values above 1, so a bright light or an emissive (glowing) material can be brighter than white, then tone-mapped down for the screen. Stride's default pipeline is HDR. See [Post effects](https://doc.stride3d.net/latest/en/manual/graphics/post-effects/index.html).
- **HUD**: Heads-up display: text and shapes drawn on the screen rather than in the world, such as a score, a health bar or a crosshair. Screen-space ShapeBatch and the debug overlay draw one. See [ShapeBatch](rendering/shape-batch.md).
- **Instancing**: Drawing many copies of one mesh in a single draw call, each with its own transform. See [Instancing](code-only/examples/instancing.md) and [Engine patterns](engine-patterns.md).
- **MeshBuilder**: The toolkit's helper for building meshes from code with chained calls: choose a layout, add vertices and indices, get a mesh. See [MeshBuilder](rendering/mesh-builder.md).
- **Outline** (constant-pixel): An edge that stays the same width on screen whether the shape is near or far, which ShapeBatch gets from measuring the shape per pixel. See [ShapeBatch](rendering/shape-batch.md).
- **Picking**: Finding what is under the cursor. Raycast picking asks the physics engine; GPU picking asks the rendered frame. See [Engine patterns](engine-patterns.md) and [GPU picking](rendering/gpu-picking.md).
- **Post effect** (post-processing): An effect applied to the finished frame: bloom, fog, vignette, tone mapping, or a custom colour transform such as the toolkit's night vision. See [Post effects](https://doc.stride3d.net/latest/en/manual/graphics/post-effects/index.html) and the [post effects example](code-only/examples/post-effects.md).
- **Render feature**: The engine class that draws one kind of thing (meshes, sprites, particles, ShapeBatch shapes) in phases: collect, prepare, draw. See [Shaders in a package](../contributing/toolkit/shaders.md) and [Render features](https://doc.stride3d.net/latest/en/manual/graphics/rendering-pipeline/render-features.html).
- **Render group**: A tag on a component that a render stage's selector filters on; how one camera draws some things and not others. See [Render groups and masks](https://doc.stride3d.net/latest/en/manual/graphics/graphics-compositor/render-groups-and-masks.html).
- **Render stage**: A named slot in the frame (opaque, transparent, shadow casters, the toolkit's picking stage) that meshes are routed to. See [Render stages](https://doc.stride3d.net/latest/en/manual/graphics/rendering-pipeline/render-stages.html).
- **Render target** (render texture): A texture the GPU draws into instead of the screen, later shown on a surface or read back. See [Render to texture](rendering/render-to-texture.md).
- **Render to texture**: A second camera drawn into a texture, for monitors, minimaps or picture-in-picture. See [Render to texture](rendering/render-to-texture.md).
- **Render view**: One camera's worth of a frame in the render system: the objects it can see, sorted, with everything outside its view skipped (culled). A scene renderer owns one. See [Custom scene renderers](https://doc.stride3d.net/latest/en/manual/graphics/graphics-compositor/custom-scene-renderers.html).
- **Scene renderer**: A step in the compositor that draws something: the forward renderer, a debug renderer, the toolkit's text and picking renderers. See [Custom scene renderers](https://doc.stride3d.net/latest/en/manual/graphics/graphics-compositor/custom-scene-renderers.html).
- **Screen space**: Positions in pixels or fractions of the window rather than in the world. `ShapeBatch.Screen` draws there. See [ShapeBatch](rendering/shape-batch.md).
- **SDF** (signed distance field): For every pixel, how far it is from a shape's edge, negative inside. ShapeBatch draws its shapes from one, which is what makes outlines and glows exact at any distance. See [ShapeBatch](rendering/shape-batch.md).
- **ShapeBatch**: The toolkit's renderer for discs, rings, polygons, lines, arcs and panels with constant-pixel outlines, in the world or on the screen. See [ShapeBatch](rendering/shape-batch.md).
- **TextureCanvas**: The toolkit's CPU-side image editor: load, blur, resample, combine and draw into a texture from code. See [TextureCanvas](rendering/texture-canvas.md).
- **Tone mapping**: Squeezing HDR colours into the 0-1 range the screen can show. See [Post effects](https://doc.stride3d.net/latest/en/manual/graphics/post-effects/index.html).
- **Wireframe**: Drawing only the edges of triangles. Stride has a render setting for it; the toolkit's mesh examples show it. See [Mesh outline](code-only/examples/mesh-outline.md).
- **World text**: Text placed in the world with a height in world units, facing where you tell it; the gallery's index board. See [World text](rendering/world-text.md).

## Shaders

- **Compute shader**: A GPU program that runs over data rather than pixels, for simulations and heavy maths. See the [compute boids example](code-only/examples/compute-boids.md).
- **Effect** (`.sdfx`): Stride's file that assembles a shader from mixins, with switches for the variants a material or feature needs. The engine generates a `ShaderMixins` class from it. See [Effect language](https://doc.stride3d.net/latest/en/manual/graphics/effects-and-shaders/effect-language.html) and [Shaders in a package](../contributing/toolkit/shaders.md).
- **Effects package**: `Stride.CommunityToolkit.Effects`, the toolkit package containing effects such as post-processing colour transforms and GPU picking. See [GPU picking](rendering/gpu-picking.md).
- **HLSL**: Microsoft's shader language; SDSL is HLSL with classes and mixins added. See [Shading language](https://doc.stride3d.net/latest/en/manual/graphics/effects-and-shaders/shading-language/index.html).
- **Mixin**: A shader class that composes into another, Stride's way of building one shader from reusable pieces instead of copy-paste. See [Shader classes, mixins and inheritance](https://doc.stride3d.net/latest/en/manual/graphics/effects-and-shaders/shading-language/shader-classes-mixins-and-inheritance.html).
- **Parameter key**: The typed name a C# side uses to set a shader value, such as a colour or a texture. See [Custom shaders](https://doc.stride3d.net/latest/en/manual/graphics/effects-and-shaders/custom-shaders.html).
- **Shader**: A small program the GPU runs per vertex or per pixel to decide where geometry lands and what colour it gets. See [Effects and shaders](https://doc.stride3d.net/latest/en/manual/graphics/effects-and-shaders/index.html).
- **SDSL** (`.sdsl`): Stride's shading language and file extension: HLSL with classes, inheritance and mixins. A shader in a package must be an embedded resource so the engine can find its source. See [Shaders in a package](../contributing/toolkit/shaders.md).
- **Shader compilation**: Turning SDSL into a program the graphics driver can run. Stride can do it when the game is built or while it runs, and caches the result; when a shader it needs has not been compiled yet, the first frame that uses it pauses briefly, which is why captures wait a few frames. See [Compile shaders](https://doc.stride3d.net/latest/en/manual/graphics/effects-and-shaders/compile-shaders.html).

## Particles

- **Additive blending**: Adding a particle's colour to what is behind it, so overlapping particles get brighter: fire, sparks, lasers. Alpha blending covers instead: smoke. See the [particle gallery](code-only/examples/particles.md) and [Particle materials](https://doc.stride3d.net/latest/en/manual/particles/materials.html).
- **Child emitter**: An emitter that spawns from another emitter's particles, on their death, along their path or when they hit something; how a firework rocket bursts. See [Spawners](https://doc.stride3d.net/latest/en/manual/particles/spawners.html).
- **Emitter**: One stream of particles with its own lifetime, spawner, initializers, updaters, shape and material. A particle system holds several. See [Emitters](https://doc.stride3d.net/latest/en/manual/particles/emitters.html).
- **Flipbook**: A texture cut into a grid of frames played through over a particle's life, so one smoke puff forms and thins. See [Particle materials](https://doc.stride3d.net/latest/en/manual/particles/materials.html).
- **Force field**: An updater that pushes particles with a vortex, a repulsor or a directed wind inside a volume. See [Updaters](https://doc.stride3d.net/latest/en/manual/particles/updaters.html).
- **Initializer**: Sets a particle's starting values as it is born: position, velocity, size, colour, rotation. See [Initializers](https://doc.stride3d.net/latest/en/manual/particles/initializers.html).
- **Particle system**: A `ParticleSystemComponent` on an entity, simulated on the CPU and drawn by the GPU; Stride's is not a compute-shader system. See [Particles](https://doc.stride3d.net/latest/en/manual/particles/index.html).
- **Ribbon / trail**: Particles joined into one strip behind a moving emitter: a sword swing, a laser, a comet. See [Ribbons and trails](https://doc.stride3d.net/latest/en/manual/particles/ribbons-and-trails.html).
- **Shape builder**: Turns each particle into geometry: a billboard, a flat quad, an oriented quad stretched along its velocity, a hexagon, a ribbon. See [Shapes](https://doc.stride3d.net/latest/en/manual/particles/shapes.html).
- **Simulation space**: Whether particles, once born, move independently of the emitter (world space), so a moving emitter leaves them behind, or are carried along with it (local space). See [Emitters](https://doc.stride3d.net/latest/en/manual/particles/emitters.html).
- **Soft particles**: Particles with a depth fade, so a smoke sprite fades where it meets the ground instead of cutting it. See [Particle materials](https://doc.stride3d.net/latest/en/manual/particles/materials.html).
- **Spawner**: Decides when particles are born: so many per second, per frame, in a burst, per unit of distance travelled. See [Spawners](https://doc.stride3d.net/latest/en/manual/particles/spawners.html).
- **Updater**: Runs over every living particle each frame: gravity, force fields, colliders, size and colour over life. You can write your own in C#. See [Updaters](https://doc.stride3d.net/latest/en/manual/particles/updaters.html).

## Physics

- **Bepu** (BepuPhysics 2): The C# physics engine Stride 4.4 uses by default; the toolkit's `Stride.CommunityToolkit.Bepu` builds bodies and colliders for it. See [Physics extensions](physics-extensions/index.md) and [Physics](https://doc.stride3d.net/latest/en/manual/physics/index.html).
- **Body** (rigid body): A physics object with mass that gravity and collisions move. See [Rigid bodies](https://doc.stride3d.net/latest/en/manual/physics/rigid-bodies.html).
- **Box2D**: A 2D physics engine; `Stride.CommunityToolkit.Box2D` wraps Box2D.NET for 2D games. See [Physics extensions](physics-extensions/index.md).
- **Bullet**: Stride's previous physics engine, still available; `Stride.CommunityToolkit.Bullet` has the same helpers for it. See [Bullet physics](https://doc.stride3d.net/latest/en/manual/physics-bullet/index.html).
- **Character controller**: A body that walks, jumps and stands on slopes without tipping over, driven by input. See [Characters](https://doc.stride3d.net/latest/en/manual/physics/characters.html).
- **Collider**: The shape physics uses for an entity, often simpler than the mesh you see: a box, sphere, capsule, or a convex hull. See [Colliders](https://doc.stride3d.net/latest/en/manual/physics/colliders.html).
- **Compound collider**: Several collider shapes under one body; every toolkit helper builds one. See [Collider shapes](https://doc.stride3d.net/latest/en/manual/physics/collider-shapes.html).
- **Constraint** (joint): A rule linking two bodies: a hinge, a rope, a motor. See [Why isn't my constraint doing anything?](physics-extensions/bepu-constraints.md) and [Constraints](https://doc.stride3d.net/latest/en/manual/physics/constraints.html).
- **Contact events**: Callbacks when two colliders start or stop touching, for damage, sounds or triggers. See [Triggers](https://doc.stride3d.net/latest/en/manual/physics/triggers.html).
- **Convex hull**: The smallest shape with no inward dents that encloses a set of points, like shrink-wrap around a mesh. Used as a collider for shapes that are not primitives; hulls are expensive to build, so the toolkit shares one between bodies of the same shape. See [Who owns the transform?](physics-extensions/bepu-transform-ownership.md).
- **Kinematic body**: A body that moves only where you put it, pushes others, and is not pushed back: platforms, doors. See [Kinematic rigid bodies](https://doc.stride3d.net/latest/en/manual/physics/kinematic-rigid-bodies.html).
- **Motor / servo**: A constraint that drives towards a target velocity (motor) or a target position (servo). See [Why isn't my constraint doing anything?](physics-extensions/bepu-constraints.md).
- **NaN**: Not a Number: a special floating-point value produced by an undefined calculation, such as `0f / 0f`. NaN values spread through every calculation they touch and can disrupt a physics simulation; a body whose position becomes NaN is lost. See [Single.NaN](https://learn.microsoft.com/en-us/dotnet/api/system.single.nan) and [Who owns the transform?](physics-extensions/bepu-transform-ownership.md).
- **Overlap / sweep query**: Asking which bodies a shape touches (overlap) or what a shape would hit if moved along a line (sweep). See [Physics queries](https://doc.stride3d.net/latest/en/manual/physics/physics-queries/index.html).
- **Raycast**: Shooting a line into the world and asking the physics engine what it hits first; the usual way to click on things. See [Raycasts](https://doc.stride3d.net/latest/en/manual/physics/physics-queries/raycasts.html).
- **Restitution / friction**: Bounciness and grip of a surface, set per body or collider. See [Rigid bodies](https://doc.stride3d.net/latest/en/manual/physics/rigid-bodies.html).
- **Simulation update**: The physics engine's own tick, on a fixed timestep; `ISimulationUpdate` runs code in step with it. See [Physics update](https://doc.stride3d.net/latest/en/manual/physics/physics-update.html).
- **Static body**: A collider that never moves: floors, walls. See [Static colliders](https://doc.stride3d.net/latest/en/manual/physics/static-colliders.html).
- **Substeps**: Splitting one physics step into several smaller ones for stability, at a cost. See [Simulation](https://doc.stride3d.net/latest/en/manual/physics/simulation.html).
- **Transform ownership**: Once a body exists, the physics engine writes the entity's transform; moving the entity yourself does nothing or fights it. See [Who owns the transform?](physics-extensions/bepu-transform-ownership.md).
- **Trigger** (sensor): A collider that reports overlaps but does not push: a door zone, a checkpoint. See [Triggers](https://doc.stride3d.net/latest/en/manual/physics/triggers.html).
- **2D physics**: Bodies confined to the XY plane so a 3D engine behaves like a 2D one; Bepu does it with a 2D body component, Box2D natively. See [Physics extensions](physics-extensions/index.md).

## Maths

- **AABB** (axis-aligned bounding box): The smallest box with sides parallel to the X, Y and Z axes that encloses an object; cheap to test, so engines use it before anything precise. See [BoundingBox](https://doc.stride3d.net/latest/en/api/Stride.Core.Mathematics.BoundingBox.html).
- **Easing**: A curve that shapes how a value moves from start to end over time, so motion accelerates, decelerates or overshoots instead of moving linearly. See the [Easing Cheat Sheet](code-only/examples/easing.md) and the [easing tutorial](../tutorials/mathematics/easing.md).
- **Interpolation**: Blending from one value to another by a fraction between 0 and 1. Lerp, short for linear interpolation, is the simplest kind: the blend moves at a constant rate. `MathUtilEx.Interpolate` blends along an easing curve instead. See [MathUtil](https://doc.stride3d.net/latest/en/api/Stride.Core.Mathematics.MathUtil.html).
- **Matrix**: A 4x4 grid of numbers that stores a position, rotation and scale together; `WorldMatrix` is an entity's place in the world as one. See [Matrix](https://doc.stride3d.net/latest/en/api/Stride.Core.Mathematics.Matrix.html).
- **Normal**: A direction perpendicular to a surface, used by lighting to know which way a face points. See [MeshBuilder](rendering/mesh-builder.md).
- **Plane**: A flat surface without edges, given by a point and a normal or by four numbers; ShapeBatch draws on one. See [Plane](https://doc.stride3d.net/latest/en/api/Stride.Core.Mathematics.Plane.html).
- **Quaternion**: The four-number form of a rotation the engine uses; build one with `Quaternion.RotationYawPitchRoll` rather than reading its fields. See [Quaternion](https://doc.stride3d.net/latest/en/api/Stride.Core.Mathematics.Quaternion.html).
- **Radians**: An angle unit used by many rotation and maths functions. A full turn is 2π radians, equivalent to 360°, and `MathUtil.DegreesToRadians` converts. Check each API's expected unit: a camera's [field of view](https://doc.stride3d.net/latest/en/api/Stride.Engine.CameraComponent.html#Stride_Engine_CameraComponent_VerticalFieldOfView), for one, is in degrees. See [MathUtil](https://doc.stride3d.net/latest/en/api/Stride.Core.Mathematics.MathUtil.html).
- **Tween**: Changes a value smoothly over a chosen duration, such as moving an object or fading its colour. An easing function controls how the change progresses. The toolkit's `Tween` keeps the time for you: start it, pass it the frame time each update, and read the current value. See [Easing Basics](code-only/examples/easing-basics.md) and [Easing in a 3D Game](code-only/examples/easing-3d-game.md).
- **Vector2 / Vector3 / Vector4**: A pair, triple or quadruple of floats: a point, a direction, a size, or a colour. See [Vector3](https://doc.stride3d.net/latest/en/api/Stride.Core.Mathematics.Vector3.html).

## Input, UI and audio

- **Camera controller**: A script that flies or orbits the camera from keyboard and mouse; the toolkit ships several. See [Camera controllers](camera-extensions/camera-controllers.md).
- **Dear ImGui**: An immediate-mode UI library for tools and debug panels; the toolkit wraps it twice, through ImGui.NET and through its own binding. See the [ImGui example](code-only/examples/imgui-ui.md).
- **Immediate mode**: Drawing or building UI by calling functions every frame, with nothing kept between frames: stop calling and it disappears. ImGui, debug shapes and ShapeBatch all work this way.
- **Myra**: A UI library for Stride with windows, buttons and layouts. It is retained mode, the opposite of immediate mode: you build the controls once and they stay until you remove them. See the [Myra example](code-only/examples/myra-ui-draggable-window-and-services.md).
- **Spatial audio**: Sound with a position, so it gets quieter with distance and pans left and right. See [Audio extensions](audio-extensions/index.md) and [Spatialized audio](https://doc.stride3d.net/latest/en/manual/audio/spatialized-audio.html).
- **Stride UI**: The engine's own UI system: pages, panels, buttons, laid out in code or the editor. See [UI](https://doc.stride3d.net/latest/en/manual/ui/index.html).
- **Virtual buttons**: Named actions ("jump", "fire") bound to keys, gamepad buttons or axes, so the game reads the action, not the key. See [Virtual buttons](https://doc.stride3d.net/latest/en/manual/input/virtual-buttons.html).

## Tooling and quality

- **DocFX**: The tool that builds this site from Markdown and XML comments. See [Contribute to the documentation](../contributing/documentation/index.md).
- **Golden image** (gold scene): A reference screenshot committed to the repository; a test scene is drawn again and compared pixel by pixel, so a change to a renderer or shader that alters the picture fails a check instead of going unnoticed. See [Shaders in a package](../contributing/toolkit/shaders.md).
- **Metadata block**: The YAML comment at the end of an example's `Program.cs` that names, classifies and describes it for the docs and launchers. See [Metadata schema](../contributing/examples/metadata-schema.md).
- **NDepend**: The static-analysis tool the maintainers run over the whole solution to catch design problems; its rules and suppressions live beside the solution. See [Contribute code](../contributing/toolkit/index.md).
- **Release notes**: The running list of what changed in each version, kept current as work lands. See [Release notes](../release-notes/index.md).
- **Roslyn analyzer**: A compiler plug-in that reports code issues as warnings; Stride ships some of its own for serialization rules. See [Contribute code](../contributing/toolkit/index.md).
- **Screenshot capture**: A toolkit feature that makes any example save one frame as an image and exit, switched on by an environment variable. The docs gallery pictures and the golden images are made with it. See [Contribute examples](../contributing/examples/index.md).
- **WARP**: Windows Advanced Rasterization Platform: a software renderer that runs Direct3D rendering on the CPU. The toolkit uses it to reduce graphics-hardware differences in screenshot tests. See [Microsoft's WARP guide](https://learn.microsoft.com/en-us/windows/win32/direct3darticles/directx-warp) and [Shaders in a package](../contributing/toolkit/shaders.md).
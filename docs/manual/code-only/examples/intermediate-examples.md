---
generated: true
---

# C# Intermediate Examples

A Stride subsystem used directly, or several concepts combined. These assume you are comfortable with the basics.

## Examples Overview

- [Procedural Geometry](procedural-geometry.md): A triangle, a plane and a circle built at runtime with MeshBuilder, which handles the vertex layout and buffer bookkeeping that raw buffers make you do by hand.
- [Simple Geometry (Labelled Triangle)](simple-geometry.md): The smallest possible custom mesh - one triangle from three vertices - with each vertex labelled on screen so the relationship between the numbers in the code and the shape on screen is visible.
- [Material Gallery](material-gallery.md): The engine's material system on a ring of stations, all from code: the four numbers of a PBR material first, then the maps, the inputs a map can be built from, and the surfaces and shading models that change what light does - transparency, glass, clear coat, cel shading, hair, subsurface scattering, displacement and tessellation, layers - ending with Game Studio's Material Package transcribed into C#.
- [Material Preview](material-preview.md): Game Studio's material thumbnail rebuilt in code, with the editor's own numbers: the forward renderer with no post effects on a grey background, a camera pitched and turned the same way, the three thumbnail lights, and the model scaled to its unit bounding sphere.
- [Cylinder Mesh](cylinder-mesh.md): A cylinder generated with MeshBuilder, split into the three jobs any surface of revolution needs: place a ring of vertices, join consecutive rings into side walls, then close both ends with caps.
- [Partial Torus Mesh](partial-torus-mesh.md): A torus defined parametrically from two angles - one around the tube, one around the ring - and cut short by limiting the second, which turns the same code into an arc, a horseshoe or a full ring without special cases.
- [3D Letters (Mesh Text)](letters-3d.md): A gallery of every glyph LetterMeshFactory can build - the digits, the full A-Z alphabet and the dash - as solid extruded meshes that catch the light like any other geometry, plus a frame counter whose digits are rebuilt as a new mesh every frame.
- [Give Me a Cube (SimulationUpdate)](simulation-update.md): Drive an entity from the physics clock instead of the render loop.
- [Raycast](raycast.md): Click the ground and a sphere is kicked towards where you clicked; click the sphere and it stops dead.
- [Ship HUD](hud.md): A cockpit HUD composed from the toolkit's shapes and world text, with one widget per file.
- [Spatial Sound](spatial-sound.md): 3D positional audio for a runtime sound: a looping pad on an orb that circles a pillar, heard from the camera.
- [Collision Group](collision-group.md): Two players and an enemy, where the players collide with each other but the enemy passes through both.
- [Cube Fountain](cube-fountain.md): A fountain of cubes, spheres and cylinders that runs on the physics clock.
- [Collision Layer](collision-layer.md): The same players-and-enemy scene as the collision group example, solved the other way.
- [Box2D Joints](box2d-joints.md): Every Box2D joint, one rig each, in a row you can pull on: a hinge pendulum with a motor and a limit, a slider on a spring, a wheel on a suspension, a rope of distance joints, a soft weld and a motor joint that springs its box back home.
- [Box2D Car](box2d-car.md): A car on two wheel joints over hilly terrain: the wheel joint pins the wheel, lets it turn, springs it along the suspension axis with a travel limit, and drives it with a motor whose speed is the throttle and whose zero is the brake.
- [Box2D Character Mover](box2d-character-mover.md): A platformer character with no rigid body, on Box2D v3's mover API: a capsule the game moves itself, Quake style, asking the world only what it touches - collect the contact planes, solve a translation that respects them, sweep it - with a pogo shape cast from the feet that floats it above the ground.
- [Multiple Physics Simulations](multiple-simulations.md): Two Bepu simulations in one game, side by side: the left lane falls under Earth gravity, the right under Moon gravity, and an amber ball that belongs to the Moon world sinks straight through the Earth ground because the two worlds never touch.
- [Car](car.md): A drivable car from four constraints per wheel, the recipe of bepuphysics2's own car demo in Stride's components: a linear axis servo as the suspension spring, a point-on-line servo as the strut, an angular axis motor for drive and brake, and an angular hinge turned about the suspension axis for steering, with Ackermann geometry on the front wheels.
- [Cloth](cloth.md): Cloth from ordinary bodies and constraints, the way bepuphysics2's own demo does it: a lattice of sphere nodes tied by distance limits that may bunch but never stretch, area constraints on every triangle against shear, and collision groups keeping neighbours from fighting.
- [Simple Constraint](simple-constraint.md): One constraint, doing one thing: a distance servo holding two spheres three units apart, pulling them together or pushing them apart until they settle there.
- [Constraints - Servo vs Motor vs Limit](constraint-motors.md): The three kinds of Bepu constraint, side by side.
- [First-Person Character (Bepu)](first-person-character.md): A first-person character built entirely from code - no Game Studio scene - on a Bepu CharacterComponent, with boxes to walk into and jump onto.
- [GPU Instancing](instancing.md): Render two walls of the same cube built two different ways, side by side.
- [Particle Gallery](particles.md): Thirty-eight particle systems on a ring of stations, all built from code: the building blocks one at a time - spawners, shapes, initializers, updaters, materials, flipbooks, soft particles - then the showpieces that put them together: a campfire, fireworks with child emitters, a tornado, a swarm driven by an updater of our own, lasers, rain that splashes, a portal, a landing burn.
- [Debug Shapes](debug-shapes.md): The full tour of the DebugShapes package: every immediate-mode primitive it can draw, exercised from a ShapeUpdater component so the shapes animate and the batching can be seen under load.
- [Render to Texture](render-to-texture.md): Five cameras watch one scene and each draws into a texture shown on a monitor: an overhead map, a chase camera following an orbiting ball, a fixed CCTV corner, and two cameras with a look of their own - night vision and a thermal camera, colour transforms from the toolkit's Effects package.
- [GPU Picking](gpu-picking.md): What is under the mouse, answered by the renderer instead of by physics.
- [Debug Shapes Usage](debug-shapes-usage.md): The short version of the debug shapes example: turn the system on, draw a sphere and a circle, done.
- [ShapeBatch Gallery](shape-batch.md): A ring of numbered stations, one ShapeBatch idea each, from a single disc to a scrolling textured panel: discs, rings, polygons and rectangles on any plane, sectors and arcs for pie and progress indicators, thick 3D lines, wire boxes, polyline strokes and space strokes, billboards, a corridor of rings that proves the constant-pixel outline, HUD panels with world text, fill colours, glow, dashes, gradients, opacity, the soft depth fade, overlay versus depth-tested batches, and textured fills.
- [Stride UI - Capsule and Window](stride-ui-capsule-with-rigid-body.md): A capsule in a 3D scene with a "Hello, World" panel drawn over it using Stride's built-in UI.
- [Stride UI - Button Hover Animation](stride-ui-button-hover-animation.md): A main menu built from code whose buttons grow a blue underline while the pointer is over them.
- [ImGui UI](imgui-ui.md): An ImGui overlay for in-game tools, debug panels and live tweaking.
- [2D Spawn Menu](spawn-menu-2d.md): Drive a scene from the keyboard without filling the screen with instructions.
- [Cube Clicker](stride-ui-cube-clicker.md): A small clicker game: cubes appear, left and right clicks are counted, and both the score and the cube positions are written to disk so the next run picks up where the last one stopped.
- [Charts 2D](charts-2d.md): A flat, paper-like chart drawn entirely in code - no assets, no chart control, every line a pixel-width stroke in a shape batch.
- [Charts 3D](charts-3d.md): The same code-only chart API as the 2D example, in a lit 3D scene.

> [!NOTE]
> Each example references a handful of toolkit packages. The `using` directives at the top of
> every listing name them, and the linked project file on GitHub is authoritative. A few examples
> also need a third-party package - Box2D.NET, Jitter2, Myra or ImGui - which their page calls out.


> [!NOTE]
> Most examples run from a copy of their project with the toolkit packages from NuGet. `Stride.CommunityToolkit.Box2D` and `Stride.CommunityToolkit.Charts` are not on NuGet yet,
> so the examples built on them run from a clone of the repository: [Box2D Joints](box2d-joints.md), [Box2D Car](box2d-car.md), [Box2D Character Mover](box2d-character-mover.md), [Charts 2D](charts-2d.md), [Charts 3D](charts-3d.md).
[!INCLUDE [basic-examples-outro](../../../includes/manual/examples/basic-examples-outro.md)]
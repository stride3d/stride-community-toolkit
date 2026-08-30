---
generated: true
---

# C# Intermediate Examples

A Stride subsystem used directly, or several concepts combined. These assume you are comfortable with the basics.

## Examples Overview

- [Procedural Geometry](procedural-geometry.md): A triangle, a plane and a circle built at runtime with MeshBuilder, which handles the vertex layout and buffer bookkeeping that raw buffers make you do by hand.
- [Simple Geometry (Labelled Triangle)](simple-geometry.md): The smallest possible custom mesh - one triangle from three vertices - with each vertex labelled on screen so the relationship between the numbers in the code and the shape on screen is visible.
- [Cylinder Mesh](cylinder-mesh.md): A cylinder generated with MeshBuilder, split into the three jobs any surface of revolution needs: place a ring of vertices, join consecutive rings into side walls, then close both ends with caps.
- [Partial Torus Mesh](partial-torus-mesh.md): A torus defined parametrically from two angles - one around the tube, one around the ring - and cut short by limiting the second, which turns the same code into an arc, a horseshoe or a full ring without special cases.
- [3D Letters (Mesh Text)](letters-3d.md): A gallery of every glyph LetterMeshFactory can build - the digits, the full A-Z alphabet and the dash - as solid extruded meshes that catch the light like any other geometry, plus a frame counter whose digits are rebuilt as a new mesh every frame.
- [Give Me a Cube (SimulationUpdate)](simulation-update.md): Drive an entity from the physics clock instead of the render loop.
- [Raycast](raycast.md): Click the ground and a sphere is kicked towards where you clicked; click the sphere and it stops dead.
- [Collision Group](collision-group.md): Two players and an enemy, where the players collide with each other but the enemy passes through both.
- [Collision Layer](collision-layer.md): The same players-and-enemy scene as the collision group example, solved the other way.
- [Simple Constraint](simple-constraint.md): One constraint, doing one thing: a distance servo holding two spheres three units apart, pulling them together or pushing them apart until they settle there.
- [Constraints - Servo vs Motor vs Limit](constraint-motors.md): The three kinds of Bepu constraint, side by side.
- [First-Person Character (Bepu)](first-person-character.md): A first-person character built entirely from code - no Game Studio scene - on a Bepu CharacterComponent, with boxes to walk into and jump onto.
- [GPU Instancing](instancing.md): Render two identical walls of cubes built two different ways, side by side.
- [Particles](particles.md): A blue fountain: fifty particles a second launched upward from a small area, pulled back down by gravity, each rendered as a camera-facing billboard.
- [Debug Shapes](debug-shapes.md): The full tour of the DebugShapes package: every immediate-mode primitive it can draw, exercised from a ShapeUpdater component so the shapes animate and the batching can be seen under load.
- [Debug Shapes Usage](debug-shapes-usage.md): The short version of the debug shapes example: turn the system on, draw a sphere and a circle, done.
- [Stride UI - Capsule and Window](stride-ui-capsule-with-rigid-body.md): A capsule in a 3D scene with a "Hello, World" panel drawn over it using Stride's built-in UI.
- [ImGui UI](imgui-ui.md): An ImGui overlay for in-game tools, debug panels and live tweaking.
- [2D Spawn Menu](spawn-menu-2d.md): Drive a scene from the keyboard without filling the screen with instructions.
- [Cube Clicker](stride-ui-cube-clicker.md): A small clicker game: cubes appear, left and right clicks are counted, and both the score and the cube positions are written to disk so the next run picks up where the last one stopped.

> [!NOTE]
> Each example references a handful of toolkit packages. The `using` directives at the top of
> every listing name them, and the linked project file on GitHub is authoritative. A few examples
> also need a third-party package - Box2D.NET, Jitter2, Myra or ImGui - which their page calls out.

[!INCLUDE [basic-examples-outro](../../../includes/manual/examples/basic-examples-outro.md)]

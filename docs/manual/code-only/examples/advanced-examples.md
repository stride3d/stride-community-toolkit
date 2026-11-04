---
generated: true
---

# C# Advanced Examples

Custom engine extension points, third-party integration and multi-project architecture. The deepest material here.

## Examples Overview

- [Custom Scene Renderers](renderer.md): Three ways to draw 2D content over a 3D scene, side by side.
- [Root Renderer Shader](root-renderer-shader.md): An animated ribbon background drawn by a custom RootRenderFeature, which is the deepest extension point Stride offers short of writing your own compositor.
- [Mesh Outline Render Feature](mesh-outline.md): Draw coloured outlines around 3D primitives with a custom RootRenderFeature.
- [Image Processing (TextureCanvas)](image-processing.md): Every combination of anchor and stretch that TextureCanvas can apply, drawn as a grid of thumbnails so the options can be compared rather than read about.
- [Instancing with Entity Transforms](instancing-entity-transform.md): Every object stays a real entity, with a transform and a physics body, while the whole crowd is drawn in one draw call: a master entity holds the ModelComponent and an instancing type that reads its members' world matrices each frame.
- [3D Text Shaders](text-shaders.md): Four materials on the same solid lettering, which is geometry like any other with normals and texture coordinates: a gradient texture read through the caps' coordinates, a rim light from a shader class of twenty lines as an emissive node, a wave that moves the vertices along the word on the engine's clock through a feature in the vertex stage, and a dissolve with a glowing edge through a feature in the pixel stage, its amount driven every frame.
- [Basic 2D Scene (Stress Pile)](stress-pile-2d.md): Thousands of 2D physics bodies piling up, drawn in two instanced draw calls - awake bodies through one master, sleeping bodies tinted green through another - with the shape, batch size and spawn layout switchable while it runs.
- [Basic 2D Scene (Stress Pile, Box2D)](stress-pile-2d-box2d.md): The Box2D twin of the stress pile: thousands of bodies piling up, drawn in two instanced draw calls - awake bodies through one master, sleeping bodies tinted green through another - with the shape, batch size and spawn layout switchable while it runs, simulated by Box2D.NET instead of Bepu.
- [Junkyard (Box2D)](junkyard-box2d.md): A faithful replica of the Box2D.NET BenchmarkJunkyard sample: 8,000 small five-sided rocks rain into a walled yard and a kinematic plow sweeps back and forth through the pile, driven by a target transform once per fixed step.
- [Junkyard Playground (Box2D)](junkyard-playground-box2d.md): The playground sibling of the Junkyard replica: the same walled yard and sweeping plow, built the Stride way - every shape is an entity carrying ShapeComponent and a Box2D body, so components, scripts, events and the camera all join in.
- [Various Constraints](constraints.md): The full tour of Bepu constraints in one interactive scene: a distance limit holding two spheres within a range, a distance servo actively driving a separation with spring settings, a ball socket pivoting a platform on a kinematic foundation, and point-on-line servos confining cubes to vertical tracks.
- [Rope - building a stable chain of constraints](constraint-rope.md): Bepu has no rope type, so a rope is a chain of small bodies tied together at runtime.
- [Box2D.NET Physics](box2d-physics.md): A 2D simulation run by Box2D.NET rather than by Stride's own physics, with Stride reduced to drawing the result.
- [Jitter2 Physics Integration](jitter2-physics.md): Demonstrates integrating the Jitter2 physics engine with Stride.
- [Jitter2 Physics - Constraining to 2D](jitter2-constraints.md): Demonstrates restricting a Jitter2 3D physics simulation to 2D-style behaviour using axis locking.
- [Stride UI - Draggable Window](stride-ui-draggable-window.md): A windowing system built on Stride's UI: windows with title bars and close buttons that can be dragged around, and that come to the front when clicked.
- [Stride UI - Draggable Window - Bullet Physics](stride-ui-draggable-window-bullet.md): The draggable window example running on the legacy Bullet physics engine.
- [Compute Shader Boids](compute-boids.md): A flock of thousands of boids that lives entirely on the GPU.
- [Myra UI - Draggable Window and Services](myra-ui-draggable-window-and-services.md): Myra, an external UI library, hosted inside Stride: a draggable window, two health bars - one declared in the view, one added while it runs - and service lookup through GetService to keep the UI from depending directly on the game.
- [Myra UI - Control Panel](myra-control-panel.md): Myra's widget set at work in one draggable window that drives a Stride scene: a menu bar, tabs, a grid of sliders for the light and the physics clock, a combo for gravity, check buttons for shadows and collider gizmos, a spin button and buttons that spawn bodies, a list view that selects and lights one, a dialog that asks before clearing, tooltips and a status line.
- [ImGui.NET Text Rendering](imgui-net.md): Render debug text with ImGui.NET, both in screen space and anchored to positions in the 3D scene.
- [Stride + SignalR - Orbital Cargo Deck](stride-signalr.md): A Stride game and a Blazor page as two consoles of the same orbital cargo deck, talking both ways over a SignalR hub.
- [Game - Cube Collapse](cube-collapse.md): A colour-match collapse puzzle built from code, its six compiled sound assets the one exception.

> [!NOTE]
> Each example references a handful of toolkit packages. The `using` directives at the top of
> every listing name them, and the linked project file on GitHub is authoritative. A few examples
> also need a third-party package - Box2D.NET, Jitter2, Myra or ImGui - which their page calls out.


> [!NOTE]
> Most examples run from a copy of their project with the toolkit packages from NuGet. `Stride.CommunityToolkit.Box2D` and `Stride.CommunityToolkit.ImGuiNet` are not on NuGet yet,
> so the examples built on them run from a clone of the repository: [Basic 2D Scene (Stress Pile, Box2D)](stress-pile-2d-box2d.md), [Junkyard (Box2D)](junkyard-box2d.md), [Junkyard Playground (Box2D)](junkyard-playground-box2d.md), [Box2D.NET Physics](box2d-physics.md), [ImGui.NET Text Rendering](imgui-net.md).
[!INCLUDE [basic-examples-outro](../../../includes/manual/examples/basic-examples-outro.md)]
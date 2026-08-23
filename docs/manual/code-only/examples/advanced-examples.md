---
generated: true
---

# C# Advanced Examples

Custom engine extension points, third-party integration and multi-project architecture. The deepest material here.

## Examples Overview

- [Custom Scene Renderers](renderer.md): Two ways to draw your own 2D content over a 3D scene, side by side.
- [Root Renderer Shader](root-renderer-shader.md): An animated ribbon background drawn by a custom RootRenderFeature, which is the deepest extension point Stride offers short of writing your own compositor.
- [Mesh Outline Render Feature](mesh-outline.md): Draw coloured outlines around 3D primitives with a custom RootRenderFeature.
- [Image Processing (TextureCanvas)](image-processing.md): Every combination of anchor and stretch that TextureCanvas can apply, drawn as a grid of thumbnails so the options can be compared rather than read about.
- [Instancing with Entity Transforms](instancing-entity-transform.md): Keep every object a real entity - with a transform, a physics body and anything else you need - while still drawing the whole crowd in a single draw call.
- [Basic2D Scene (Stress Pile)](stress-pile-2d.md): Thousands of 2D physics bodies piling up, drawn in a single draw call through instancing, with the shape, batch size and spawn layout switchable while it runs.
- [Various Constraints](constraints.md): The full tour of Bepu constraints in one interactive scene: a distance limit holding two spheres within a range, a distance servo actively driving a separation with spring settings, a ball socket pivoting a platform on a static foundation, and point-on-line servos confining cubes to vertical tracks.
- [Rope - building a stable chain of constraints](constraint-rope.md): Bepu has no rope type, so a rope is a chain of small bodies tied together at runtime.
- [Box2D.NET Physics](box2d-physics.md): A 2D simulation run by Box2D.NET rather than by Stride's own physics, with Stride reduced to drawing the result.
- [Jitter2 Physics Integration](jitter2-physics.md): Demonstrates integrating Jitter2 physics engine with Stride.
- [Jitter2 Physics - Constraining to 2D](jitter2-constraints.md): Demonstrates constraining a Jitter2 3D physics simulation to 2D-style behaviour.
- [Stride UI - Draggable Window](stride-ui-draggable-window.md): A windowing system built on Stride's UI: windows with title bars and close buttons that can be dragged around, and that come to the front when clicked.
- [Stride UI - Draggable Window - Bullet Physics](stride-ui-draggable-window-bullet.md): The draggable window example running on the legacy Bullet physics engine.
- [ImGui.NET Text Rendering](imgui-net.md): Render debug text with ImGui.NET, both in screen space and anchored to positions in the 3D scene.
- [Stride + SignalR](stride-signalr.md): A Stride game and a Blazor web page talking to each other in real time over a SignalR hub, so a button in a browser can spawn an entity in the running game and the game can report back.
- [Game - Cubicle Calamity](cubicle-calamity.md): A colour-match collapse puzzle built entirely from code.

> [!NOTE]
> Each example references a handful of toolkit packages. The `using` directives at the top of
> every listing name them, and the linked project file on GitHub is authoritative. A few examples
> also need a third-party package - Box2D.NET, Jitter2, Myra or ImGui - which their page calls out.

[!INCLUDE [basic-examples-outro](../../../includes/manual/examples/basic-examples-outro.md)]

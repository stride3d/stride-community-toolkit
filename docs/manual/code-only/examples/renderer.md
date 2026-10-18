# Renderer

This example shows three ways to draw 2D content over a 3D scene: a scene-wide renderer, a renderer attached to one entity, and the toolkit's `EntityTextComponent` labels drawn by `EntityTextRenderer`, which need no renderer of your own. The two custom renderers:

1. Global Scene Renderer Implementation - `MyCustomSceneRenderer`:
   - Creates a custom renderer that operates on all entities in the scene
   - Implements SceneRendererBase to hook into Stride's rendering pipeline
   - Renders both static text ("Example 1") and dynamic per-entity information
   - Displays entity names and positions with semi-transparent backgrounds
   - Converts 3D world positions to 2D screen coordinates for text placement
2. Entity-Specific Renderer Implementation - `SpriteBatchRendererScript`:
   - Attaches a custom renderer to a specific entity via a StartupScript
   - Creates a DelegateSceneRenderer to handle rendering without subclassing
   - Displays "Example 3: I am a capsule" for its parent entity
   - Demonstrates renderer initialization and integration through the component system

The example also applies an angular impulse so the capsule rolls after it lands, which shows the labels following a moving entity. The approaches differ in scope:

- The scene-wide approach for global visualization needs (debug overlays, HUD elements)
- The entity-specific approach for object-focused rendering (labels, status indicators)

Both techniques utilize SpriteBatch for efficient 2D rendering within a 3D scene context, showing how to properly initialize resources, convert between coordinate systems, and integrate with Stride's render stages.

![Custom scene renderers](media/renderer.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E09_3D_SceneRenderer).

[!code-csharp[](../../../../examples/code-only/E09_3D_SceneRenderer/Program.cs?start=1&end=79)]
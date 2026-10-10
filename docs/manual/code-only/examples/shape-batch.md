---
generated: true
slug: shape-batch
---

# ShapeBatch Gallery

A ring of numbered stations, one ShapeBatch idea each, from a single disc to a scrolling
textured panel: discs, rings, polygons and rectangles on any plane, sectors and arcs for pie and
progress indicators, thick 3D lines, wire boxes, polyline strokes and space strokes, billboards,
a corridor of rings that proves the constant-pixel outline, HUD panels with world text, fill
colours, glow, dashes, gradients, opacity, the soft depth fade, overlay versus depth-tested
batches, and textured fills. Every station is one method that draws in its own coordinates, so
it can be lifted into a game as it is; the ring, labels and index board come from the registry.

The `Program.cs` file shows how to:

- Registering shape batches with AddShapeBatch, depth-tested, overlay and textured
- One static method per exhibit, drawing in a station's local frame, portable into any game
- A registry that lays out the ring, the labels and the index board on its own
- Numbered labels in screen-space entity text, joined to their exhibit by a dotted pixel line
- An eased camera flight between stations that gives way the moment the visitor takes the controls
- Why a shape never clips the text on it, and why wrapping is the caller's job
- Discs, rings, polygons and rectangles on an arbitrary plane in 3D
- Sectors, annuli and round-capped arcs for pie, donut and progress indicators
- Thick 3D lines and wire boxes from camera-facing capsules
- Polyline strokes with round joins, in pixels or world units, and space strokes through 3D points
- Billboards that keep their shape from any viewpoint, and pixel-radius markers that keep their size
- Why a signed distance function keeps an outline a constant pixel width
- HUD panels with glowing edges and glowing world text, including a live counter
- Fill colours, an outer glow in pixels, dashes animated through their phase, gradients and opacity
- The soft depth fade, where a shape melts into geometry instead of cutting off
- Two batches in one scene, and what the overlay one shows through
- Textured fills from a fill source, clamped for a picture and wrapped for a scrolling stripe
- A second camera rendered into an HDR texture and filled into a shape, built from the engine's own renderers

![ShapeBatch Gallery](media/shape-batch.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E11_3D_ShapeBatch_Gallery).

[!code-csharp[](../../../../examples/code-only/E11_3D_ShapeBatch_Gallery/Program.cs?start=1&end=200)]
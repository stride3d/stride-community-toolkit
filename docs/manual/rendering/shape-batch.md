# ShapeBatch - vector shapes without meshes, materials or a UI tree

Almost every panel, ring, gauge, glow and dashed line in the toolkit's recent examples - the 2D
panels gallery, the spaceship HUD, the Box2D scenes, the in-scene console of the SignalR example -
is drawn by one thing: `ShapeBatch`, in the `Stride.CommunityToolkit.Shapes` package. This page is
about *why* it exists and what makes it different, because the answer is not obvious from the API,
and the technique it uses is the reason it can do what the engine's own tools cannot.

If you only want the method list, the [ShapeBatch example](../code-only/examples/shape-batch.md)
tours every shape and the API reference has the signatures. Read on for the thinking.

## The problem: an outline that stays the same width

Draw a circle on the ground of a 3D scene as a marker under a unit. Now walk the camera away
from it. With any mesh-based drawing, the outline of that circle shrinks with distance exactly as
the circle does, because the outline *is* geometry: a ring of triangles some fraction of a metre
wide. Ten metres away it is a hairline; a hundred metres away it is gone. Zoom a 2D scene out and
every border on every shape thins to nothing at the same moment.

A designer never wants that. A selection ring, a HUD frame, a gauge, a debug outline - all of them
want a border that is *two pixels wide*, whatever the zoom, distance or window size. That is a
screen-space property, and no amount of world-space geometry can deliver it. You would have to
rebuild the ring every frame, per shape, from the camera's current scale. Which is a shader.

## The tempting paths, and why each one stops short

Before ShapeBatch, three things in Stride looked like the answer. Each is the right tool for
something else.

**Stride UI.** The UI system is a retained-mode tree: `Canvas`, `StackPanel`, `Border`,
`TextBlock`, `ImageElement`, laid out by rules, with input routing and focus. It draws *textured
quads* - a rounded, anti-aliased panel is a sprite asset with nine-slice borders, and scaling it
stretches pixels. There are no vector primitives: no circle, no arc, no ring, no dashed line, no
polygon. A `UIComponent` can stand in the world as a plane, but it is one rectangle with a
resolution, not shapes lying on the deck, facing the camera, or swung into a thick 3D line. Stride
UI is the right tool for a menu, a form, a drag-and-drop inventory
([E04_StrideUI_DragAndDrop](../code-only/examples/stride-ui-draggable-window.md)). It is the wrong
tool for a glowing ring on the floor.

**Debug Shapes.** The toolkit's [Debug Shapes](debug-shapes.md) draws real geometry: primitive
meshes for cubes, spheres and capsules, hardware lines for lines. Perfect for "where is this
collider" - and the line width is whatever the driver gives you, which on every modern API is one
pixel, because the shader's `LineWidthMultiplier` is a solid-or-wireframe toggle (1 or 10000), not
a width. Circles are polylines, so they are never anti-aliased and never constant-width.

**Meshes and materials.** Build the ring as a mesh, give it a material, add a `ModelComponent`.
This works, and the Box2D Junkyard replica originally did exactly this. What it cost, in order:
a border that had to be rebuilt per zoom bucket to look pixel-constant; a fill and a border mesh
whose draw order flipped at random between runs (opaque-stage sorting is draw-order dependent per
material pair); a rewrite of the border as a non-overlapping ring so order could not matter; and
then the discovery that a transparent material on an *instanced* mesh renders at full opacity in
Stride 4.4. Days of work for a rectangle with a border. The record is in the example's history; the
lesson is that this path fights the engine at every step because the
engine's mesh pipeline was built for lit, opaque, textured objects, and a debug rectangle is none
of those.

## The idea: measure the shape per pixel

Box2D's own testbed had already solved this, in about sixty lines of GLSL. Erin Catto's
`solid_polygon` shader draws every body in the physics debug view as a **signed distance function**:
the GPU draws one screen-aligned quad per shape, and for every pixel of that quad the fragment
shader computes how far the pixel is from the shape's edge - negative inside, positive outside.
Everything follows from that one number:

- inside by more than the border width: fill colour;
- inside by less than the border width: border colour;
- outside: transparent, with anti-aliasing from a smooth step over the last pixel.

The border width is compared in *pixels*, because the shader knows the current pixel scale.
So the border is two pixels wide at every distance, for free, with no geometry to rebuild. Rounding
a corner is subtracting a radius from the distance. A circle is a polygon with one vertex and a
radius; a capsule is two vertices and a radius; a rounded rectangle is four vertices and a radius.

`ShapeBatch` is that shader, ported to SDSL and extended. The extensions are all more functions of
the same distance: a hollow band gives rings and annuli, an angular cut gives sectors and arcs, a
glow is the distance falling off outside the edge, dashes fold a gap pattern into the distance
along the outline, a gradient runs across the shape's own extent, and an opacity multiplies the
final alpha. A **polyline** is the distance to the nearest of a run's segments, which makes a
stroke around it a curve with round joins and caps and no geometry for either. And - the part the
testbed never needed - each shape carries its own plane in 3D.

The shader is three files composed as SDSL mixins - `ShapeDistance` (the distance functions),
`ShapeColor` (unpacking and compositing) and `ShapeShader` (the streams and the two stages) - which
is the engine's own idiom for sharing shader code, and what lets the distance functions be reused
without copying them.

```mermaid
flowchart LR
    A["Your update loop<br/><i>shapes.DrawRing(...)</i>"] --> B["ShapeBatch<br/>one record per shape:<br/>plane, colours, style<br/>plus its points, any number"]
    B --> C["Three structured buffers<br/>records, points and space points,<br/>uploaded once per frame"]
    C --> D["Vertex shader<br/>one quad per instance,<br/>grown to fit the border and glow"]
    D --> E["Fragment shader<br/>reads the record and its points,<br/>signed distance per pixel:<br/>fill, border, glow, dash, cut"]
    E --> F["One instanced draw call<br/>however many shapes"]
```

The whole frame's shapes - thousands of them, if you like - go out as a single instanced draw of
one shared quad. No models, no materials, no assets; the package ships one shader.

## Why it is a *batch*

The API deliberately echoes `SpriteBatch`, and the mental model is the same: **immediate mode**.
You submit shapes every frame from your update logic, the batch draws them once, in submission
order, and forgets them. Nothing is retained, so there is nothing to keep in sync - a HUD is one
draw routine that reads the game's state, not a tree of objects whose properties have to be
updated when the state changes. Compare the two ways to show a fuel gauge:

```csharp
// Retained: create once, then remember to update it every time fuel changes
var bar = new Border { Width = 120, Height = 8, BackgroundColor = Color.Green };
canvas.Children.Add(bar);
// ... somewhere else, on every change ...
bar.Width = 120 * fuel;

// Immediate: the gauge is a function of the state, drawn every frame
shapes.Fill.Set(Color.Green, 0.95f);
shapes.DrawRectangle(center, axisX, axisY, new Vector2(120 * fuel, 8), Color.Green);
```

The second has no state to leak, no ordering bugs, and it restyles itself when the scheme changes
because it reads the scheme every frame. Every board in the SignalR example is written this way:
`StationBoard.Draw` runs from scratch each frame from the deck's census and the console's colours.

Style properties - `BorderWidth`, `Fill`, `Glow`, `Dash`, `Gradient`, `Opacity` - are *current
state*, captured by each draw call as it is made, exactly as a sprite batch captures its blend
state. Set them, draw, change them, draw again. Two batches can coexist with different depth
behaviour: one depth-tested for decals and markers the scene can cover, one overlay batch for gizmos
that must always show ([the playground](../code-only/examples/shape-batch.md) runs both; press T).

One number decides what a glow reads as, and it is not the width: the glow colour's **alpha**. At
full alpha the halo is solid colour where it meets the edge and only then falls off, so a thin
stroke with a six-pixel glow looks like a stroke three times fatter. At 30 to 40 percent it reads
as light around the stroke, which is the neon look. For a glow in each shape's own colour, where there
is no colour of your own to give an alpha to, `Glow.Strength` is that number: `Glow.Set(10f);
Glow.Strength = 0.35f;` glows everything the batch draws in its own colour, at a third. `Glow.Additive`
then makes that light add to the scene rather than cover it, which is what a lit thing over a dark
ground wants.

## Flat shapes, anywhere in 3D

The testbed was 2D. Making the same shader work in a 3D scene took one idea: every shape stays
flat but carries its own plane, in one of three modes.

| `PlaneMode` | The plane is | Gives you |
|---|---|---|
| `Fixed` | The two axes you pass | Discs on the floor, decals, panels standing on a wall or hanging in space |
| `Screen` | Aligned to the screen, facing the camera | Billboards that keep their shape from any angle |
| `Axial` | Your X axis kept, the plane swung about it to face the camera | A capsule between two points becomes a **thick 3D line** with round ends - the thing hardware lines cannot do |

Perspective came almost free. The vertex shader passes the clip-space `w` as a varying, which
interpolates to exactly the fragment's `w`, so a pixel-measured border is scaled correctly under a
perspective camera; under an orthographic 2D camera `w` is 1 and the 2D path is bit-identical to
the testbed's, which is how the Box2D examples were verified against it.

One kind of shape is not flat at all. A **space stroke** - `DrawPolyline` or `DrawPixelPolyline`
handed `Vector3` points - has no plane. The vertex stage projects its points, takes their bounding
box on screen and hands the pixel stage a screen-aligned quad; the pixel stage measures the run in
pixels, each segment's radius converted at its own depth. A rope narrows with distance and a trail
stays the same width, and both face the camera from every angle with no geometry behind them. Each
fragment writes the depth of the nearest point of the run, so a stroke threads behind and in front
of geometry the way a mesh does - the cost being that the shape shader writes depth, which gives up
the early depth rejection every shape used to get for free. Shapes are alpha blended and rarely
large on screen, so that has not shown in a frame time yet.

That depth buffer works the other way too. The forward renderer resolves the opaque pass's depth
as a texture before it draws the transparent stage, and offers it to every render feature; the
shape feature takes it, so a shape can compare its own distance from the camera with the
scene's at the same pixel. `DepthFade` is that comparison turned into a fade: over the distance
you give, in world units, a fragment fades out as it approaches whatever is behind it. A marker
sunk in the floor melts into it instead of ending in a hard line, and a ring standing a hand in
front of a wall dims over the wall and stays bright where the ground is far behind. It is the soft
particles trick, and it has the same limit: the depth test still removes what is behind the
surface, so the fade only softens the approach. On an overlay batch, where nothing is removed, a
fragment behind the surface fades to nothing instead, which makes the fade a soft depth test of
its own. A compositor that turns the resolved depth off leaves every shape at its hard cut.

## A picture in a shape

A fill was a flat colour or a two-colour gradient, and every image in a HUD was somebody else's
problem: a sprite under a frame, aligned by hand, clipped by nothing. Stride's material system had
the answer already. Any of its `IComputeColor` nodes - a texture with scale, offset and address
modes, a blend of two nodes, a custom shader class - can be composed into a shader with one
`compose` line, and the shape shader takes that plug for its fill. `ShapeBatch.FillSource` holds
the node; `FillWith(texture)` installs the common one. Every shape drawn while `Textured` is on
multiplies its fill by the sample, so a white fill shows the picture as it is, a tint darkens it,
the fill alpha fades it and a gradient still runs over it, while the border and the glow are
exactly what they were. The picture spans the shape's bounding box with `(0,0)` at the top left,
the sprite convention: a rounded panel shows it edge to edge, a disc shows the inscribed square,
and a thick line stretches it along its own box. What lies past the box, under a thick border, is
the node's address mode to decide - clamp repeats the edge, wrap tiles.

The composition is resolved when the effect is built, which makes it one fill source per batch,
the way a sprite batch is one texture per `Begin`. That is less of a constraint than it sounds:
`Textured` is per draw call, so one batch holds the picture panel and the plain outlines around it,
which matters because two batches do not order deterministically. A batch with a fill source gets
its own effect, with the batch's own parameters carrying the node's textures and values; the node
is re-read every frame, so a scrolling stripe is one assignment to the node's `Offset` in update,
and only a different composition reloads the effect. A plain batch keeps the shader it always had,
which the gold scenes confirm.

What it opens is what a picture inside an anti-aliased, outlined, glowing shape opens: a portrait
in a circular frame, a minimap in a rounded rectangle, a second camera's render target in a
cockpit mirror, hazard stripes that move, a noise texture behind a shield ring.

## A second camera in a panel

The textured fill's best customer is a picture that changes every frame: another camera's view.
A rear-view mirror, a security monitor, a picture-in-picture map. `game.AddRenderTextureCamera`
gives back the texture, and the shape gallery's last station is that call plus a batch with
`FillWith(feed.Texture)` and a rectangle drawn through it. What the call builds, and the two
things about it that are not obvious, are on the [render to texture](render-to-texture.md) page.

Every station's shapes appear in the mirror, because a batch is drawn once per view and emptied
only after the last one - the design decision from the render-feature hygiene pass, now doing the
job it was made for. The panel appears in its own mirror when the two cameras face each other.

One trap that is nothing to do with rendering, kept here because it cost an evening: `Textured`
is per-draw state like every other, so a method that turns it off to draw plain brackets over
the picture has turned it off for every draw that follows, in every later frame, until something
turns it back on. A blank panel with a perfectly good texture behind it is that.

## On the screen, from the same batch

A 3D game wants a crosshair, a health arc in a corner, a compass strip, a target box - shapes
that live on the screen, not in the world. Until now the answer was a second, orthographic camera
and world units that happened to look like pixels, which works when the whole game is 2D and falls
apart the moment the game is 3D. Now the batch has a switch:

```csharp
shapes.DrawDisc(new Vector3(0f, 0.02f, 1.5f), Vector3.UnitY, 3f, Color.OrangeRed); // in the world

shapes.Screen = true;                                                    // and now in pixels
var centre = shapes.ScreenSize * 0.5f;
shapes.DrawRing(new Vector3(centre, 0f), Vector3.UnitZ, 22f, Color.White);
shapes.DrawArc(shapes.Corner(ScreenCorner.BottomLeft) + new Vector2(100f, -90f), 50f, -MathF.PI * 0.5f, MathF.Tau * health, Color.LimeGreen, width: 14f);
shapes.Screen = false;
```

`Screen` is per-draw state like every other, captured as each call is made, so a HUD and the
world it sits over come from one batch in submission order - no second camera, no second batch,
no sort question. Coordinates are pixels from the top left with Y down, like a sprite, so a
positive angle turns clockwise on screen and the gauge above starts at twelve o'clock. `Corner()`
places a widget as a corner plus an offset rather than by a hardcoded resolution, and
`ScreenSize` is the window in the same units.

Those units are the display's *scaled* pixels when `AutoScale` is on, which is the default: a
layout is the same size to the eye on a 4K laptop and a 1080p monitor, and a pixel-measured width,
dash or glow means exactly what it means in the world. The shader treats a screen shape as one
whose world-to-pixel scale is 1, so every feature - the outline band, the anti-aliasing ramp, the
dash fit, the glow, the textured fill - runs unchanged.

A `Viewport` rectangle offsets everything drawn while it is set, and sizes `Corner()` to it, so a
chart or a panel draws in its own coordinates and lands where the rectangle is; nothing is clipped
to it. A screen shape is written at the near plane, so the depth test keeps it over any geometry
even in a depth-tested batch. The `Screen` switch on the batch and the `Screen` plane mode are
different things: the mode is a billboard in the world, the switch is a sprite on the glass.

The screen HUD station in the shape gallery draws only while you stand at it, because a HUD is
on the screen, not on the ring. Gold scene `shapes-screen` pins the pixel mapping, the Y-down
convention, the viewport offset and the near-plane depth over a depth-tested world disc.

## Why a white HUD came out grey

The shape gallery's frame draws a dotted line from each exhibit up to its label, in `Color.White`,
and next to the label - which is white - it read as grey. Every variant did: solid, dashed, thin,
thick, and a line in world units. Measured in a capture, every one of them peaked at 127 out of
255. Exactly half, and the same half whatever the width, which rules out anti-aliasing and the dash
ends and points at something that happens to the whole batch.

It is the tone mapper. The batch draws in the scene's transparent stage, into the HDR buffer, and
the post-processing chain then treats its pixels as scene light: auto exposure scales the frame to
the lit surfaces, which sit at several times one, and the tone map curve compresses what is left.
A shape's white is a unit colour, so it lands wherever the exposure puts one - about half, in a
scene lit for the material gallery - and it moves as the exposure adapts. The text renderers never
have this problem because they draw after the post effects, in the compositor's UI stage, which is
why the label beside the line was truly white.

```csharp
var world = game.AddShapeBatch(depthTest: true);          // a decal: lit, exposed and tone-mapped with the scene
var hud = game.AddShapeBatch(afterPostEffects: true);     // a HUD: a colour comes out as given
```

`afterPostEffects` puts the batch in the UI stage, after the chain, where a colour is written as it
was given. The batch is in the toolkit's UI render group, which the main view leaves out and a second
camera renderer draws last, so nothing there is depth-tested - that is the trade, and for a HUD it
is the right one. A compositor without the toolkit's UI stage has no such place, and the batch then
draws in the transparent stage like any other. Which to use is what the shape *is*: a marker on the
ground is part of the scene and should be exposed with it; a crosshair, a gauge, a leader line to a
label, is on the glass and should not.

## Which shape is under the mouse

Sooner or later something drawn wants to be clicked: a HUD button, a chart's hover, a station's
pad, a tile in a sheet. The obvious tools are the wrong ones. GPU picking reads mesh IDs back from
a render target, and a shape is not a mesh, so it is invisible to it; a physics collider is a body,
not a drawing. Four places in this repository once answered the question by hand - a ray against
a plane, then a rectangle or a disc test in that plane's coordinates - each re-deriving geometry
the batch already held. The batch is the one thing that knows the exact outline it painted, and
the functions it painted it with are the distance functions above. So it answers itself, with the
same functions on the CPU:

```csharp
shapes.Tag = station;                                    // state, like Fill or Glow
shapes.DrawRing(station.Origin, Vector3.UnitY, 5.8f, Color.White);
shapes.Tag = null;

// Next frame, in Update
if (shapes.TryPick(Input.MousePosition, out var hit) && hit.Tag is Station picked)
{
    GoTo(picked);
}
```

`Tag` is per-draw state captured as each call is made, like every other property: set it, draw,
clear it. A shape drawn with no tag can never be picked and costs nothing - the batch records only
tagged shapes, and only what a distance test needs. The answer comes from the frame last drawn,
which is the frame on the screen: a script asks between one draw and the next, and "what is under
the mouse right now" is exactly what the user is looking at.

`TryPick` returns the topmost shape - the nearest to the camera, and at equal depth the one drawn
last, so a HUD's top layer wins; a screen shape is over everything. `PickAll` returns every hit
front to back. A hit is a `ShapeHit`: the tag, the world point where the pick lands (pixels for a
screen shape), the same point in the shape's own plane coordinates - relative to the position it
was drawn at, in the units its vertices were given in, which is what a board turns into a button
index or a chart into a value - the signed distance to the outline, and the depth.

Every kind of shape is tested the way the shader draws it. A flat shape in the world is a ray
against its plane and then the plane's field: the polygon with its rounding radius, the band of a
ring or an annulus, the wedge of a sector, the run of a polyline with its width. A screen shape is
pixels straight from the mouse. A billboard and a pixel-measured marker are placed from the view
the batch was last drawn in, the same right, up and depth the vertex stage used. A space stroke is
measured on its projection, in pixels, like the pixels themselves. The border counts as part of the
shape - it straddles the outline, and it is what you see - and `slackPixels` adds a few more, which
is what makes a one-pixel line or a hairline ring clickable:

```csharp
shapes.TryPick(Input.MousePosition, out var hit, slackPixels: 4f);
```

A ring, an arc or a polyline picks on its stroke, because that is the shape; a pad or a button that
should take a click anywhere inside is a disc or a rectangle with a transparent fill
(`Fill.Set(null, 0f)`), which paints the same outline and picks as the whole area.

A `ShapeComponent` has a `Pickable` switch, since Game Studio cannot store an object as a tag; a
pickable component is its own tag, and its entity is one step away.

What a pick cannot see, so you reach for the right tool:

- **Occlusion by the scene.** The batch has no depth buffer on the CPU, so a depth-tested shape
  behind a wall still picks. GPU picking is the other way round: it sees exactly what is on screen,
  but only meshes. A game that needs both runs both.
- **Dashes.** A dashed outline picks as if solid. The gaps are visual.
- **More than one view.** The batch remembers the last view that drew it; a batch drawn by a
  render-texture camera and the main camera in the same frame answers for whichever came last.
- **The first frame.** Nothing has been drawn yet; `CanPick` says so.

The **Picking** station in the shape gallery draws one of everything with a tag and lights up
whatever the mouse is over, printing the tag, the local coordinates and the distance. The 2D
panels example highlights the panel under the mouse; the easing sheet's tiles, the galleries' pads
and the SignalR board's scheme buttons all pick this way now, where each used to carry its own
ray-and-rectangle maths. `ShapePickTests` holds every kind to a round trip: a known world point
projected to the screen and picked back.

## Where it came from, and where it went

The renderer was born as the Box2D package's debug draw on 2026-08-31, because the mesh approach
above had failed to reproduce the testbed's look. The same day it became clear it had nothing to
do with Box2D - a gauge, a panel and a selection ring are the same shape as a physics body - so it
was promoted, 3D-enabled and named `ShapeBatch`. It went into the core toolkit first and moved out
again *the same day*: the core package had been free of shaders and assets, and one shader in it
would have cost that property for every consumer. It now lives in its own package, mirroring how
`DebugShapes` ships its shader, and pairs with two things in core:

- **[World Text](world-text.md)** for text on a shape. A `WorldTextComponent` with `Billboard = false`
  lies in its entity's XY plane, so it sits on a panel with the panel's rotation; its `GlowSize`
  matches the shapes' glow. Text is the one thing an SDF shader should not attempt.
- **`DisplayScale`** so that "two pixels" means two pixels on a 100% display and three on a 150%
  one, the same rule the overlay and the text renderers follow. `AutoScale = false` opts out.

`ShapeComponent` is the small bridge into the entity system: a shape drawn from an entity's
transform, so a thing can be a shape without a model, and it appears in Game Studio's property grid.
It needs no `AddShapeBatch` call: where a game made one, the component draws through it and inherits
its state; otherwise the processor registers a depth-tested batch of its own, which is also what
draws the shape in the scene editor, where the processor runs and nothing else does.

Writing a shader of your own for a package - where it goes, what the engine already gives you, the dither and the timing scope - is on the contributing page [Shaders in a toolkit package](../../contributing/toolkit/shaders.md).

## Two scars worth knowing about

**The blend state.** For a week every glow looked harsh and a fill's alpha seemed to be ignored.
The shader produced straight alpha - the testbed's convention - and the render feature was blending
it as if premultiplied. One line (`BlendStates.NonPremultiplied`) softened every glow and made
opacity mean opacity, across every example at once. The shader has since moved to premultiplied
compositing, the convention every Stride batch uses: layers add without a division, the alpha left
in a render target is right, and an additive glow (`Glow.Additive`) is just a glow with no alpha.
The lesson stands either way: if a fade "does nothing", check the blend before the shader.

**Colour space.** The palette is sRGB bytes and Stride's backbuffer is sRGB, so the shader decodes
each colour to linear light - with the real sRGB curve, not `pow 2.2`, which crushed every dark
value - and only when the device says the target is linear. A gamma pipeline gets the bytes as they
are. Compositing happens in that linear light, the same space the hardware blends the shape into
the scene, so a border over a fill and a shape over the world are the same kind of mix.

**Colours are integers.** A colour packed as four bytes into a `float` can form a NaN bit pattern
(alpha 255 with blue over 127 does it), and GPUs canonicalise NaNs on read, destroying the red
channel. Every colour in the instance record is a `uint`. The symptom looks exactly like a
struct-layout mismatch; the fix was semantics, not layout.

## What it is not

Honest limits, so you reach for the right tool:

- **Convex fills only.** Any number of vertices, but the fill of a concave polygon is not what
  you would expect. Strokes are the exception: `DrawPolyline` and `DrawPixelPolyline` take a run
  of any length and any shape - a curve, a path, a concave HUD frame, closed or open - as one
  stroke with round joins. What a stroke cannot do is fill the inside of a concave run.
- **Very long translucent strokes bead slightly.** A run of more than 64 points is drawn as a few
  pieces that share a point, because every fragment of a stroke tests every segment of its run;
  where two pieces meet the round cap is drawn twice, and under `Opacity` below one that is a
  faintly brighter dot. The playground's line demo draws a 48-point run at half opacity, which is
  one piece, so nothing shows there. A space stroke also restarts its dash pattern at each piece,
  since a piece cannot know the on-screen length of the pieces before it.
- **Flat, except for strokes.** A space stroke is the one shape drawn through 3D points. A
  sphere outline is still a billboard disc; a wireframe of an arbitrary mesh needs a
  different shader (barycentric, `fwidth()`-based) that does not exist yet.
- **Screen shapes are not clipped.** A `Viewport` rectangle places and sizes; a shape that
  overhangs it is drawn whole. Space strokes are never screen shapes.
- **No text.** Pair with World Text, or render text to a texture and fill a shape with it.
- **One fill source per batch.** A composition is resolved when the effect is built, so a batch
  with two pictures is two batches, or one atlas and a node that selects from it.
- **No layout.** It draws, and it can say which shape is under a point (above), but it has no
  buttons, focus or events: a hover and a click are two lines in your `Update`, and anything with
  layout is Stride UI's job.
- **One sort decision per batch.** A batch is a single render object with a meaningless bounding
  box, so how it sorts against *transparent meshes* is one decision for all its shapes. Use an
  overlay batch (`depthTest: false`) for things that must never be covered rather than trusting
  the sort. The same goes for two batches with shapes in the same frame: the stage orders them, and
  with meaningless bounds that order can differ from one run to the next, so anything that has to
  layer deterministically goes through one batch.

## Which tool, then?

```mermaid
flowchart TD
    Q1{"Is it a menu, form or<br/>something with layout and focus?"} -->|yes| UI["Stride UI"]
    Q1 -->|no| Q2{"A lit, textured object<br/>in the scene?"}
    Q2 -->|yes| Mesh["Mesh + material"]
    Q2 -->|no| Q3{"Wireframe of a collider<br/>or mesh, one pixel is fine?"}
    Q3 -->|yes| DS["Debug Shapes"]
    Q3 -->|no| Q4{"Flat, convex, and the outline<br/>must stay a constant pixel width?"}
    Q4 -->|yes| SB["ShapeBatch<br/>(+ World Text for labels)"]
    Q4 -->|no| Compose["Compose: several shapes,<br/>or a mesh with a ShapeBatch outline"]
```

## One example, mapped

The SignalR example's in-scene console (`E13_SignalR/Station/`) uses every idea on this page:

| Piece | What it is | Idea from this page |
|---|---|---|
| `Board` | A plane in the world with `(u, v)` coordinates and a pick | Flat shapes on a `Fixed` plane; input is yours to add |
| `StationBoard.Draw` | Panel, rail, dividers, buttons, bars - rebuilt every frame | Immediate mode; style state captured per call |
| Scheme buttons | `Fill` solid for the chosen one, `Glow` for the hovered one | Fill and glow are functions of the same distance |
| `DrawCornerTicks` | A quarter `DrawArc` plus two `DrawPixelLine`s per corner | Angular cut; pixel-measured stroke |
| `Labels` | World text entities placed with the board's rotation | Text pairs with shapes, it is not one of them |
| `DeckEffects` rings | `DrawRing` with `Opacity` fading and radius growing | Opacity multiplies the final alpha |
| Starfield | 400 `DrawBillboardCircle`s at 380 m in the same batch | `Screen` plane; thousands of shapes, one draw |
| Screen HUD (shape gallery) | Crosshair, gauge, panel, viewport bar - `Screen = true` in the same batch | Pixels from the top left; `Corner()`; `Viewport` |

Everything above is a few hundred lines of ordinary C# calling a handful of draw methods, which is
the actual answer to "why wasn't it always done this way": it could have been. The technique is
older than the toolkit and lives in every physics testbed and most 2D engines. Stride's rendering
grew up around meshes, materials and a textured UI, so nobody had put an SDF shape shader behind a
sprite-batch-shaped API in it. Once one existed, panels and gauges stopped being a project and
became a draw call.
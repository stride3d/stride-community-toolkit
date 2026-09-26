# GPU picking - what is under the mouse, answered by the renderer

Every click-to-select, hover-to-highlight and drag-this-object interaction starts with one
question: which thing is under this screen point? The toolkit has answered it with physics for as
long as it has had physics, and that answer is right for a lot of games. This page is about the
other answer, the one Game Studio's own viewport uses when you click an entity, and the one that
works when nothing has a collider.

## The ray that goes straight through

```csharp
if (camera.Raycast(input.MousePosition, 100f, out var hit))
{
    // hit.Collidable, hit.Point, hit.Normal
}
```

A raycast shoots a line from the camera through the mouse and asks the physics world what it
crosses first. Fast, exact, and it answers now, on the CPU. It has one blind spot, and the blind
spot is large: **it only sees colliders**. A decorative mesh with no body, an instanced flock of
birds, a procedural model, a skinned character that has an animation but no capsule, a teapot -
the ray goes straight through all of them. Every example in the toolkit that clicks on things
carries a physics package for that reason alone, and the SignalR console asks its shape batch
which button is under the mouse because its buttons are shapes, not bodies.

You can fit a collider to everything, of course. Then every decorative mesh needs a body it never
uses for anything else, the shape is a box around a thing that is not a box, and an instanced
model needs one body per instance. That is a lot of physics for a question about pixels.

## Ask the thing that knows

The renderer already knows exactly which triangle wins every pixel - that is what the depth
buffer is. So ask it. Draw the scene once more into a hidden target, but instead of colours have
each mesh write *its own id*: "I am model component 42, mesh 0, instance 17". Then copy the one
pixel under the mouse back to the CPU and decode it. Whatever id is there is what the eye sees at
that point, per pixel, through the same vertex pipeline that drew the frame - skinning,
instancing, material displacement, all of it, for free.

```csharp
var picker = game.AddGpuPicker();   // once, after SetupBase3D
picker.Continuous = true;           // follow the mouse every frame

// each frame
if (picker.Result is { Hit: true } hit)
{
    // hit.Entity, hit.ModelComponent, hit.MeshIndex, hit.MaterialIndex,
    // hit.InstanceIndex, hit.WorldPosition, hit.Depth
}
```

`Request(position)` asks about one point once; `Continuous` asks at the mouse on every frame that
has no explicit request. `Result` is the last answer, and its `Sequence` says when there is a new
one. The position is normalised over the viewport, with the origin at the top left, which is what
`Input.MousePosition` gives you.

## What the call builds

`AddGpuPicker` adds four things to the compositor, and knowing them is what makes the limits
below make sense:

- A **"Picking" render stage** with a four-channel float target and a depth buffer, so the id
  pass has its own depth test and a mesh in front wins over a mesh behind, exactly as on screen.
- A **stage selector** on the mesh feature that routes every mesh (or the render groups you
  choose) into that stage under the picking effect. The effect is the engine's own forward shading
  effect with the pixel stage swapped for a five-line id writer, so the vertex side - the part
  that decides where things are - is untouched.
- A **sub render feature** on the mesh feature, `GpuPickingRenderFeature`, that hands each mesh its
  id through the per-draw constant buffer and remembers which component each id was.
- A **camera renderer** of its own, `GpuPickingSceneRenderer`, over the main camera slot,
  appended after the main view. On a frame with a request it draws the picking stage into a
  temporary target the size of the view, scissored to the one pixel asked about, copies that pixel
  into a tiny staging texture, and reads back the one written two frames earlier.

Idle, it adds no view, no stage and no work. Game Studio's viewport does the same pass and reads
the pixel back at once, paying a GPU stall on every click; a game cannot afford that, so the
toolkit's version keeps three staging textures in flight and never waits.

The hit point is not read from anywhere: the id writer also stores the pixel's depth, and the
renderer unprojects the pixel through the view that drew it. That turns "which" into "where", and
it is what lets the picker stand in for the raycast's hit point.

## Where the instancing went

The gallery example picks a field of crates drawn as one instanced model, and reports which
instance. That worked on the second try, and the reason it did not work on the first is worth a
paragraph, because it is the kind of thing that costs an evening.

The engine's instancing feature binds two structured buffers to every instanced draw, the
instance world matrices and their inverses, and it binds them by position: first slot world,
second slot inverse. In the forward effect that is the order the compiler lists them. In the
picking effect the pixel stage uses neither, and the compiler lists them the other way round. Every
crate was drawn through its inverse matrix - mirrored, sunk into the floor, invisible - and the
pick fell through to the ground behind. Dumping the whole id target to an image showed the crates
as flat slivers at mirrored positions, which is a picture worth a thousand theories.

The fix is in `GpuPickingRenderFeature`: for its own render nodes it rebinds the two buffers by
name, walking the effect's per-draw resource entries. The engine's own picking has the same gap,
so an instanced entity in Game Studio's viewport probably cannot be clicked either; that is one for
the engine.

The effect file and the id writer are eleven lines between them; what a package needs to carry a shader at all is on [Shaders in a toolkit package](../../contributing/toolkit/shaders.md).

## Honest limits

- **Two frames late.** For hover and click that is invisible. For dragging, keep the object from
  the click and move it by the mouse; do not re-pick every frame and expect it to keep up.
- **Cut-out materials pick as solid.** Only the pixel stage is replaced, so a leaf texture's
  transparent pixels still write the leaf's id. The editor has the same limit.
- **Another pass over the meshes in view.** The GPU part is a handful of scissored pixels. The
  CPU part is the draw submission again, on frames with a request. Hundreds of entities cost
  nothing you will see; an instanced model is one draw however many instances it has. Narrow
  `Pickable` to a render group to keep decorative geometry out.
- **Meshes only.** Sprites, UI, particles, debug shapes and shape-batch shapes are not picked.
  A shape is an analytic function the CPU can test directly, which is what `ShapeBatch.TryPick`
  does - see [ShapeBatch](shape-batch.md#which-shape-is-under-the-mouse) - and the right tool for
  them.
- **The main camera.** The picker reads the camera in the compositor's first slot. A feed drawn
  into a texture would need a picker of its own if its picture were ever clickable.

## Which one, then?

| You want | Use |
|---|---|
| The first body along a line, from anywhere, now | A physics raycast |
| What the eye sees under a screen point, colliders or not | The GPU picker |
| A hit on a shape-batch panel or button | The board's own ray-to-plane test |
| Both: click selects a mesh, then the drag is physical | Pick with the GPU, then drive the body |

The GPU picking example (`E09_3D_GpuPicking`) has no physics package at all: a teapot, pillars,
primitives and a field of instanced crates, hover to highlight, click to select. The gold scene
`picking` pins the pass: five screen points, one on each kind of thing and one on the sky, each
answer drawn where it landed.
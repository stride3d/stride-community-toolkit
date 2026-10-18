# GPU picking

`GpuPicker` reports which mesh is under a screen point, from the rendered frame, as Game Studio's
viewport does. It needs no collider. It is in the `Stride.CommunityToolkit.Effects` package,
namespace `Stride.CommunityToolkit.Effects.Picking`.

A physics raycast answers the same question for colliders only:

```csharp
if (camera.Raycast(input.MousePosition, 100f, out var hit))
{
    // hit.Collidable, hit.Point, hit.Normal
}
```

A mesh without a body, an instanced model, a procedural mesh or a skinned character without a
capsule is invisible to the ray. Fitting colliders to decorative geometry costs a body per mesh and
one per instance.

## How it works

The picker draws the scene once more into a hidden target. Instead of a colour, each mesh writes
its own id: model component, mesh and instance. The pixel under the mouse is copied back to the CPU
and decoded. The id pass uses the same vertex pipeline as the frame, so skinning, instancing and
material displacement are picked as drawn.

```csharp
var picker = game.AddGpuPicker();   // once, in Start
picker.Continuous = true;           // follow the mouse every frame

// each frame
if (picker.Result is { Hit: true } hit)
{
    // hit.Entity, hit.ModelComponent, hit.MeshIndex, hit.MaterialIndex,
    // hit.InstanceIndex, hit.WorldPosition, hit.Depth
}
```

| Member | Description |
|---|---|
| `Request(position)` | Asks about one point once. The position is normalised over the viewport, origin top left, as `Input.MousePosition` |
| `Continuous` | Asks at the mouse on every frame that has no explicit request |
| `Result` | The last answer, a `PickResult`. `Sequence` increments with every new answer |
| `Pickable` | A `RenderGroupMask` of the groups the picker draws. The default is `RenderGroupMask.All` |

`PickResult` has `Hit`, `Entity`, `ModelComponent`, `MeshIndex`, `MaterialIndex`, `InstanceIndex`,
`Depth` and `WorldPosition`. `Depth` is 0 at the near plane and 1 at the far plane, 1 for the
background. `WorldPosition` is rebuilt from the depth through the view that drew it.

## What the call builds

`AddGpuPicker` adds four things to the compositor:

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

Idle, the picker adds no view, no stage and no work. Three staging textures are in flight, so a
read never waits for the GPU and the answer arrives two frames after the request.

- **Instanced models.** The engine's instancing feature binds the instance world matrices and
  their inverses by position. The picking effect lists the two buffers in the other order, so
  `GpuPickingRenderFeature` rebinds them by name for its own render nodes. Without that, every
  instance is drawn through its inverse matrix and the pick misses.

The shader behind the id pass is in the package's `Effects` folder. See
[Shaders in a toolkit package](../../contributing/toolkit/shaders.md).

## Limitations

- **Two frames late.** Invisible for hover and click. For dragging, keep the object from the click
  and move it by the mouse; do not re-pick every frame.
- **Cut-out materials pick as solid.** Only the pixel stage is replaced, so a leaf texture's
  transparent pixels still write the leaf's id. The editor has the same limit.
- **Another pass over the meshes in view.** The GPU draws one scissored pixel. The CPU submits the
  draws again on frames with a request. An instanced model is one draw however many instances it
  has. Narrow `Pickable` to a render group to keep decorative geometry out.
- **Meshes only.** Sprites, UI, particles, debug shapes and shape-batch shapes are not picked. For
  shapes use `ShapeBatch.TryPick`. See [ShapeBatch](shape-batch.md#pick-shapes).
- **The main camera.** The picker reads the camera in the compositor's first slot. A feed drawn
  into a texture needs a picker of its own.

## Raycast or picker

| You want | Use |
|---|---|
| The first body along a line, from anywhere, now | A physics raycast |
| What the eye sees under a screen point, colliders or not | The GPU picker |
| A hit on a shape-batch panel or button | `ShapeBatch.TryPick` |
| Both: click selects a mesh, then the drag is physical | Pick with the GPU, then drive the body |

`E09_3D_GpuPicking` has no physics package: a teapot, pillars, primitives and a field of instanced
crates. Hover highlights, left click selects, <kbd>Escape</kbd> clears the selection. The gold scene
`picking` asks about five screen points, one on the sky, and draws each answer where it landed.
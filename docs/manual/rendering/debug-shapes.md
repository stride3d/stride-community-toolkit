# Debug Shapes

Immediate-mode wireframe and solid shapes for debugging: colliders, ray hits, spawn points, the direction something is facing. They ship in the `Stride.CommunityToolkit.DebugShapes` package.

## Getting the system

Register the renderer once with [`AddDebugShapes()`](xref:Stride.CommunityToolkit.DebugShapes.Code.DebugShapeExtensions.AddDebugShapes(Stride.Engine.Game,Stride.Rendering.RenderGroup)), then resolve `ImmediateDebugRenderSystem` from the game's services. It is registered as a service and as a game system, so you get it the same way from a script or from a code-only `Start` callback.

```csharp
game.AddDebugShapes();

var debugDraw = game.Services.GetService<ImmediateDebugRenderSystem>();
```

> [!IMPORTANT]
> This is **immediate mode**: a shape is drawn for the frame you asked for it and then forgotten. Call
> the `Draw*` methods from your update loop, not once at startup. `duration` is how many extra seconds
> the shape survives without being re-issued; leave it at `0` for the once-per-frame case.
>
> `AddDebugShapes()` sets `Visible = true` only in `DEBUG` builds. In a Release build you have to set
> it yourself, or nothing appears.

## Shared parameters

Every method below ends with the same optional parameters, so they are listed once here rather than repeated:

| Parameter | Default | Meaning |
|---|---|---|
| `color` | `default` | The shape's colour. |
| `duration` | `0.0f` | Seconds the shape stays visible without being re-issued. `0` means this frame only. |
| `depthTest` | `true` | When `false`, the shape draws through geometry in front of it. |
| `solid` | `false` | Filled instead of wireframe. Not available on the line methods. |

## Shapes

| Method | Shape-specific parameters |
|---|---|
| [`DrawLine`](xref:Stride.CommunityToolkit.DebugShapes.Code.ImmediateDebugRenderSystem.DrawLine*) | `start`, `end` - no `solid` |
| [`DrawLines`](xref:Stride.CommunityToolkit.DebugShapes.Code.ImmediateDebugRenderSystem.DrawLines*) | `vertices` - an array drawn as a connected run. No `solid` |
| [`DrawRay`](xref:Stride.CommunityToolkit.DebugShapes.Code.ImmediateDebugRenderSystem.DrawRay*) | `start`, `dir` - a direction rather than an end point. No `solid` |
| [`DrawArrow`](xref:Stride.CommunityToolkit.DebugShapes.Code.ImmediateDebugRenderSystem.DrawArrow*) | `from`, `to`, `coneHeight`, `coneRadius` |
| [`DrawSphere`](xref:Stride.CommunityToolkit.DebugShapes.Code.ImmediateDebugRenderSystem.DrawSphere*) | `position`, `radius` - no `rotation` |
| [`DrawHalfSphere`](xref:Stride.CommunityToolkit.DebugShapes.Code.ImmediateDebugRenderSystem.DrawHalfSphere*) | `position`, `radius`, `rotation` |
| [`DrawCube`](xref:Stride.CommunityToolkit.DebugShapes.Code.ImmediateDebugRenderSystem.DrawCube*) | `start`, `size`, `rotation` |
| [`DrawBounds`](xref:Stride.CommunityToolkit.DebugShapes.Code.ImmediateDebugRenderSystem.DrawBounds*) | `start`, `end`, `rotation` - a box given by two opposite corners |
| [`DrawCapsule`](xref:Stride.CommunityToolkit.DebugShapes.Code.ImmediateDebugRenderSystem.DrawCapsule*) | `position`, `height`, `radius`, `rotation` |
| [`DrawCylinder`](xref:Stride.CommunityToolkit.DebugShapes.Code.ImmediateDebugRenderSystem.DrawCylinder*) | `position`, `height`, `radius`, `rotation` |
| [`DrawCone`](xref:Stride.CommunityToolkit.DebugShapes.Code.ImmediateDebugRenderSystem.DrawCone*) | `position`, `height`, `radius`, `rotation` |
| [`DrawQuad`](xref:Stride.CommunityToolkit.DebugShapes.Code.ImmediateDebugRenderSystem.DrawQuad*) | `position`, `size` - a `Vector2` - and `rotation` |
| [`DrawCircle`](xref:Stride.CommunityToolkit.DebugShapes.Code.ImmediateDebugRenderSystem.DrawCircle*) | `position`, `radius`, `rotation` |

> [!NOTE]
> `DrawCapsule`, `DrawCylinder` and `DrawCone` take **`height` before `radius`**, which is the opposite
> way round from how most people write it. `DrawHalfSphere` takes `color` **before** `rotation`, unlike
> every other rotatable shape. Use named arguments and neither can bite you.

## Where to see it working

[Debug Shapes](../code-only/examples/debug-shapes.md) draws every shape at once, and
[Debug Shapes Usage](../code-only/examples/debug-shapes-usage.md) is the smaller version showing just
the setup.
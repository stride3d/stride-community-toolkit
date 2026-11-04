# Debug Shapes

Immediate-mode wireframe and solid shapes for debugging: colliders, ray hits, spawn points, the direction something is facing. They ship in the `Stride.CommunityToolkit.DebugShapes` package.

## Getting the system

Register the renderer once with [`AddDebugShapes()`](xref:Stride.CommunityToolkit.DebugShapes.DebugShapeExtensions.AddDebugShapes(Stride.Games.IGame,Stride.Rendering.RenderGroup)). It returns the `ImmediateDebugRenderSystem` and registers it as a service and a game system, so a script gets the same instance with `Services.GetService<ImmediateDebugRenderSystem>()`.

```csharp
var debugDraw = game.AddDebugShapes();
```

> [!IMPORTANT]
> This is **immediate mode**: a shape is drawn for the frame you asked for it and then forgotten. Call
> the `Draw*` methods from your update loop, not once at startup. `duration` is how many extra seconds
> the shape survives without being re-issued; leave it at `0` for the once-per-frame case.
>
> The system's `Visible` starts on only when your game's entry assembly, or the toolkit itself, is a
> Debug build. In a Release build, set `Visible = true` on the system, or nothing appears.

> [!NOTE]
> At most `MaxPrimitives` one-frame shapes and `MaxPrimitivesWithLifetime` timed shapes are kept (both
> default to `100`); past that, each new shape drops the oldest. Raise them when drawing many shapes.

## Shared parameters

Every method below ends with the same optional parameters, so they are listed once here rather than repeated:

| Parameter | Default | Meaning |
|---|---|---|
| `color` | `default` (`null` on `DrawLines`) | The shape's colour; the default draws in `PrimitiveColor` (light green). Alpha below 255 draws it transparent. |
| `duration` | `0.0f` | Seconds the shape stays visible without being re-issued. `0` means this frame only. |
| `depthTest` | `true` | When `false`, the shape draws through geometry in front of it. |
| `solid` | `false` | Filled instead of wireframe. Not available on the line methods. |

## Shapes

| Method | Shape-specific parameters |
|---|---|
| [`DrawLine`](xref:Stride.CommunityToolkit.DebugShapes.ImmediateDebugRenderSystem.DrawLine*) | `start`, `end` - no `solid` |
| [`DrawLines`](xref:Stride.CommunityToolkit.DebugShapes.ImmediateDebugRenderSystem.DrawLines*) | `vertices` - each consecutive pair is one separate segment. No `solid` |
| [`DrawRay`](xref:Stride.CommunityToolkit.DebugShapes.ImmediateDebugRenderSystem.DrawRay*) | `start`, `dir` - a direction rather than an end point. No `solid` |
| [`DrawArrow`](xref:Stride.CommunityToolkit.DebugShapes.ImmediateDebugRenderSystem.DrawArrow*) | `from`, `to`, `coneHeight`, `coneRadius` |
| [`DrawSphere`](xref:Stride.CommunityToolkit.DebugShapes.ImmediateDebugRenderSystem.DrawSphere*) | `position`, `radius` - no `rotation` |
| [`DrawHalfSphere`](xref:Stride.CommunityToolkit.DebugShapes.ImmediateDebugRenderSystem.DrawHalfSphere*) | `position`, `radius`, `rotation` |
| [`DrawCube`](xref:Stride.CommunityToolkit.DebugShapes.ImmediateDebugRenderSystem.DrawCube*) | `start` - the centre, not a corner - `size`, `rotation` |
| [`DrawBounds`](xref:Stride.CommunityToolkit.DebugShapes.ImmediateDebugRenderSystem.DrawBounds*) | `start`, `end`, `rotation` - a box given by two opposite corners |
| [`DrawCapsule`](xref:Stride.CommunityToolkit.DebugShapes.ImmediateDebugRenderSystem.DrawCapsule*) | `position`, `height`, `radius`, `rotation` |
| [`DrawCylinder`](xref:Stride.CommunityToolkit.DebugShapes.ImmediateDebugRenderSystem.DrawCylinder*) | `position`, `height`, `radius`, `rotation` |
| [`DrawCone`](xref:Stride.CommunityToolkit.DebugShapes.ImmediateDebugRenderSystem.DrawCone*) | `position`, `height`, `radius`, `rotation` |
| [`DrawQuad`](xref:Stride.CommunityToolkit.DebugShapes.ImmediateDebugRenderSystem.DrawQuad*) | `position`, `size` - a `Vector2` - and `rotation` |
| [`DrawCircle`](xref:Stride.CommunityToolkit.DebugShapes.ImmediateDebugRenderSystem.DrawCircle*) | `position`, `radius`, `rotation` |

> [!NOTE]
> `DrawCapsule`, `DrawCylinder` and `DrawCone` take **`height` before `radius`**, which is the opposite
> way round from how most people write it. `DrawHalfSphere` takes `color` **before** `rotation`, unlike
> every other rotatable shape. Use named arguments. For all three, `height` is the whole shape, caps included.

## See also

- [Debug Shapes](../code-only/examples/debug-shapes.md) example - every shape at once
- [Debug Shapes Usage](../code-only/examples/debug-shapes-usage.md) example - just the setup
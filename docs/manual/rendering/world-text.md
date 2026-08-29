# World Text

`WorldTextComponent` draws text that lives **in** the 3D scene: positioned by its entity's transform,
scaled by perspective, and - by default - hidden by geometry standing in front of it. It is the
counterpart to [Entity Text](entity-text.md), which draws flat pixels **over** the scene.

The shorthand for choosing between them: resize the window and entity text stays the same size; walk
the camera away and world text shrinks.

Two pieces are involved:

- **`WorldTextComponent`** records what to draw and how.
- **`WorldTextRenderer`** draws it. Register it once with `game.AddWorldTextRenderer()`.

`WorldTextComponent` and `TextAnchor` live in the `Stride.CommunityToolkit.Rendering.Text` namespace; the
renderer is in `Stride.CommunityToolkit.Renderers` and the `AddWorldTextRenderer()` extension in
`Stride.CommunityToolkit.Engine`.

```csharp
game.AddWorldTextRenderer();

var sign = new Entity("Sign")
{
    new WorldTextComponent
    {
        Text = "Spawn",
        Height = 0.4f,
        Anchor = TextAnchor.BottomCenter,
    }
};

sign.Transform.Position = new Vector3(0, 0, -3);
sign.Scene = rootScene;
```

> [!IMPORTANT]
> A `WorldTextComponent` without the renderer simply never appears - no error, no log line, just
> absent text. `AddWorldTextRenderer()` is safe to call any number of times; a duplicate renderer is
> never added. Helpers that create world text themselves, such as `AddGroundGizmo` with
> `showAxisName: true`, register it on your behalf.

## Sizing: Height, not FontSize

The two size properties answer different questions, and keeping them apart is the point:

- **`Height`** is how tall the text block is **in world units**. A `Height` of `0.5f` is half a metre,
  however long the string is and however the camera moves. The entity's own scale multiplies on top.
- **`FontSize`** is how sharply the glyphs are **rasterised**, in pixels. Raise it when text viewed
  close up looks soft; it spends glyph-cache space and changes nothing about the size in the world.

## Orientation

| Setup | Behaviour |
|---|---|
| `Billboard = true` (default) | Turns to face the camera. With `KeepUpright = true` (default) it swivels about the world Y axis only, so it never rolls when the camera tilts - how a standing label should behave. |
| `Billboard = true, KeepUpright = false` | Faces the camera squarely from any angle, including from directly above. |
| `Billboard = false` | Keeps the entity's own rotation. Lay it flat on a floor, fix it to a wall - it foreshortens and disappears edge-on like any other surface. |

## Depth

`DepthTest` is what makes this world text rather than an overlay: a wall between the camera and the
text hides the text. Set it to `false` for marker-style text that should be positioned and scaled in
the world but never hidden - the middle ground between the two text systems.

For distance control, `MaxDistance` stops drawing text beyond a range, and `FadeStartDistance`
(together with `MaxDistance`) fades it out on the way there.

## Where it is used in the toolkit

The axis letters on the ground gizmo (`AddGroundGizmo(showAxisName: true)`) are world text: one sharp
camera-facing quad per letter, coloured to match its axis, occluded by the scene exactly as the axis
arrows are. They replaced letter shapes assembled from cylinder meshes - which were being turned to
face the camera every frame anyway, at which point geometry buys nothing a flat quad does not.

## Costs and limits

- **One draw call per text.** The view and projection matrices belong to a sprite batch rather than a
  draw call, so every text is its own `Begin`/`End`. Fine for tens of labels - axis names, markers,
  signs; wrong for thousands.
- The glyphs are rasterised bitmaps, so extreme close-ups show softness no matter the `FontSize`.
  Text as actual geometry - extruded letters that catch the light - is a different feature with
  different costs.
- One font per component; `\n` works, and `Alignment` arranges the lines.
- The renderer uses the first camera in the graphics compositor.

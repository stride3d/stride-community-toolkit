# World Text

`WorldTextComponent` draws text that lives **in** the 3D scene: positioned by its entity's transform,
scaled by perspective, and - by default - hidden by geometry standing in front of it. It is the
counterpart to [Entity Text](entity-text.md), which draws flat pixels **over** the scene.

The shorthand for choosing between them: resize the window and entity text stays the same size; walk
the camera away and world text shrinks.

Two pieces are involved:

- **`WorldTextComponent`** records what to draw and how.
- **`WorldTextRenderer`** draws it. `WorldTextProcessor` adds it to the compositor when no
  `AddWorldTextRenderer()` call has.

The component and the renderer are in the `Stride.CommunityToolkit.Rendering.Text` namespace, `Anchor` in
`Stride.CommunityToolkit.Rendering`; the `AddWorldTextRenderer()` extension is in `Stride.CommunityToolkit.Engine`.

```csharp
var sign = new Entity("Sign")
{
    new WorldTextComponent
    {
        Text = "Spawn",
        Height = 0.4f,
        Anchor = Anchor.BottomCenter,
    }
};

sign.Transform.Position = new Vector3(0, 0, -3);
sign.Scene = rootScene;
```

> [!NOTE]
> `AddWorldTextRenderer()` registers the renderer explicitly, for example to keep a reference to it.
> Calling it any number of times adds no duplicate. Helpers that create world text themselves, such as
> `AddGroundGizmo` with `showAxisName: true`, register it as well.

## Sizing: Height, not FontSize

The two size properties answer different questions:

- **`Height`** is how tall the text block is **in world units**. A `Height` of `0.5f` is half a metre,
  however long the string is and however the camera moves. The entity's own scale multiplies on top.
- **`FontSize`** is how sharply the glyphs are **rasterised**, in pixels. Raise it when text viewed
  close up looks soft; it spends glyph-cache space and changes nothing about the size in the world.

On a scaled display the same `Height` covers that many more pixels, so the renderer rasterises at
`FontSize` times the toolkit's shared `DisplayScale` - text on a 150% laptop is drawn from glyphs
1.5x the size and stays as sharp as on a 100% monitor. Nothing moves or resizes; `GlowSize`, being
in font pixels, scales along so the glow keeps its reach. `AutoScale = false` on the component
rasterises at exactly `FontSize`. This only works when the process is DPI aware; see
[the text overlay page](text-overlay.md#size-and-high-dpi-displays).

## Glow

A `GlowColor` with an alpha above zero and a `GlowSize` above zero draw the text again behind itself
in that colour, in a ring reaching `GlowSize` font pixels out from the letters. Both default to off (a
transparent colour and `0`). A tenth of `FontSize` is a crisp halo; a quarter is a bloom.

## Orientation

| Setup | Behaviour |
|---|---|
| `Billboard = true` (default) | Turns to face the camera. With `KeepUpright = true` (default) it swivels about the world Y axis only, so it never rolls when the camera tilts. |
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
arrows are.

## Costs and limits

- **One draw call per text.** The view and projection matrices belong to a sprite batch rather than a
  draw call, so every text is its own `Begin`/`End`. Fine for tens of labels; wrong for thousands.
- The glyphs are rasterised bitmaps, so extreme close-ups show softness no matter the `FontSize`.
  For lettering as geometry see [3D Text](text-3d.md).
- One font per component; `\n` works, and `Alignment` arranges the lines.
- The renderer uses the first camera in the graphics compositor.

## See also

- [Entity Text](entity-text.md)
- [3D Text](text-3d.md)
- [World Text](../code-only/examples/world-text.md) example
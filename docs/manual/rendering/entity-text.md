# Entity Text

`EntityTextComponent` draws a line of text on the screen for the entity it is attached to, without
using Stride's UI system. It is meant for labels, debug readouts, floating numbers and small HUDs in
code-only projects, where bringing up a UI page for one line of text is more machinery than the job
needs.

This is *screen-space* text: always the same pixel size, always on top of the scene. For text that
lives inside the scene - scaled by perspective and hidden by geometry in front of it - see
[World Text](world-text.md).

Two pieces are involved:

- **`EntityTextComponent`** records *what* to draw and *where*.
- **`EntityTextRenderer`** is a scene renderer that draws it. Nothing appears until one is added to
  the graphics compositor.

`EntityTextComponent`, `TextAnchor` and `TextPositionMode` live in the `Stride.CommunityToolkit.Rendering.Text`
namespace; the renderer is in `Stride.CommunityToolkit.Renderers` and the `AddEntityTextRenderer()`
extension in `Stride.CommunityToolkit.Engine`.

```csharp
game.AddEntityTextRenderer();

var entity = game.Create3DPrimitive(PrimitiveModelType.Cube);

entity.Add(new EntityTextComponent
{
    Text = "Hello",
    Anchor = TextAnchor.BottomCenter,
    Offset = new Vector2(0, -12),
    EnableShadow = true
});

entity.Scene = rootScene;
```

Text is drawn with a `SpriteBatch` and no depth testing, so it always appears over the scene rather
than being hidden by geometry in front of it.

## Where the text goes

`PositionMode` picks between three ways of deciding the position, and they behave quite differently.

| Mode | Position comes from | Culled when off screen | Use it for |
|---|---|---|---|
| `World` (default) | The entity's world position, projected to the screen | Yes | Labels on objects in the scene |
| `Screen` | `ScreenPosition`, in pixels from the top-left | No | Fixed placement you want full control over |
| `Anchored` | A window corner, given by `ScreenAnchor` | No | A HUD that must survive the window being resized |

`Offset` is applied in all three modes. In `Anchored` it acts as a margin and always points *inwards*,
so `new Vector2(16, 16)` is sixteen pixels in from the chosen corner whichever corner that is.

`Anchored` also takes its `Anchor` from the corner rather than from the property, so text pinned to
the top-right grows to the left and stays on screen. Use `Screen` when you want to choose the
position and the anchor independently.

## Anchor is not Alignment

This is the part that catches people, so it is worth stating plainly:

- **`Anchor`** decides which point of the text sits on the position. This is what "centre the label on
  the object" means, and it is almost certainly the property you want.
- **`Alignment`** is Stride's `TextAlignment`. It only decides how the lines of a *multi-line* string
  sit relative to one another. **On single-line text it does nothing at all** - `Left`, `Center` and
  `Right` all produce identical output, because the alignment is computed against the width of the
  whole string, which for one line is the width of that line.

So a label that should float above a point wants `Anchor = TextAnchor.BottomCenter`, not
`Alignment = TextAlignment.Center`.

## Readability

Text drawn over a 3D scene has no control over what ends up behind it. Two options:

- **`EnableShadow`** draws the string a second time in `ShadowColor`, offset by `ShadowOffset`. Cheap,
  and usually enough.
- **`EnableBackground`** fills a rectangle behind the text, sized from the text and expanded by
  `Padding`. `BackgroundColor` defaults to a dark, half-transparent panel.

The background is axis-aligned and does not turn with `Rotation`.

## Animating

`Scale`, `Opacity`, `Rotation` and `LayerDepth` exist to be changed per frame:

- **`Scale`** rather than `FontSize`. Changing the font size re-rasterises glyphs and re-measures the
  string; scaling does neither, and it scales about `Anchor` so centred text grows evenly.
- **`Opacity`** multiplies into the text, shadow and background alpha, leaving the configured colours
  untouched - so a fade can be restarted without having to remember the original colours.
- **`LayerDepth`** orders the texts against each other. Higher is drawn on top.

`IsVisible` hides the text without removing the component, which keeps the cached measurement alive.

For world text, `FadeStartDistance` and `MaxDistance` fade labels out with distance, or simply stop
drawing them past a cutoff when only `MaxDistance` is set.

## Fonts

`Font` is optional; leave it `null` and Stride's default font is used. Setting it per component lets
different text use different faces in the same scene.

## How it is collected

Components are gathered by `EntityTextProcessor`, registered automatically through the component's
`DefaultEntityComponentProcessor` attribute. Nothing needs to add it.

This matters for one reason worth knowing: because the engine reports components as they are added
and removed, **text on child entities is drawn**, and cached measurements are released when a
component goes away. An earlier version of the renderer walked the scene's top-level entity list
instead, so labels parented to another entity - the obvious way to attach a label to a thing - never
drew, and every short-lived label leaked a cache entry.

## The debug renderer

`EntityDebugSceneRenderer` is the other side of the same coin. Where `EntityTextComponent` draws text
an entity **opts into**, the debug renderer labels **every** entity automatically with its name and/or
position, in one shared style:

```csharp
game.AddEntityDebugSceneRenderer(new()
{
    ShowEntityPosition = true,
    IncludeChildEntities = true,
    PositionColor = Color.DarkBlue,
    EnableBackground = true,
});
```

It is a debugging overlay you switch on, not authored content, which is why it stays a separate
renderer. Both share their drawing - projection, anchoring, background, shadow - so those behave
identically in each.

Options worth knowing:

- **`IncludeChildEntities`** is off by default. A scene built from composed entities can hold far more
  children than top-level entities, and labelling all of them at once is usually unreadable.
- **`EntityFilter`** is the cheapest way to make a busy scene legible - narrow to one name or one
  component type instead of reading every label on screen.
- **`PositionColor`** gives the coordinates their own colour, and moves them onto a line beneath the
  name. Two colours on one line means measuring and chaining the parts, and a stack reads better.
- **`MaxDistance`** drops labels past a given range.

> [!IMPORTANT]
> A default background only makes sense paired with a default text colour. The debug renderer's text
> defaults to **black** on a **light** panel; `EntityTextComponent` defaults to **white** on a **dark**
> one. The two used to share a single default background, so darkening it to suit white text silently
> turned every debug label into black-on-black. If you change one, change the other.

## Limitations

- One `SpriteFont` per component, no rich text or per-character colour.
- No word wrapping. `\n` in the string works and `Alignment` then applies.
- The background does not rotate with the text.
- The renderer uses the first camera in the graphics compositor.

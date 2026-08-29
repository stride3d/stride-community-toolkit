# Debug Overlay

`DebugOverlay` is one block of on-screen text, assembled from **sections** contributed by whatever has
something to say - the camera controller's key help, your own instructions, a live counter - with one
position and one hide key for the lot. It is a game system, so it survives scene swaps and draws
itself every frame; nothing calls it.

The tempting alternative is `game.DebugTextSystem.Print(...)` from every script's `Update`. That works
for one line. With three scripts each printing at a hand-picked pixel position, the lines overlap on
the first window resize, the camera help draws on top of your counter, and every one of them is 8 by 16
pixel text that a 4K display makes unreadable. The overlay exists so that text has one owner.

`DebugOverlay`, `DebugOverlaySection`, `TextElement` and `DisplayPosition` live in the
`Stride.CommunityToolkit.Scripts.Utilities` namespace.

## Adding text

Get the shared instance and add a section. The callback runs every frame the overlay is drawn, so
values that change need no pushing - this is `Example01_Basic3DScene_Primitives`:

```csharp
var overlay = DebugOverlay.GetOrCreate(game);

overlay.Position = DisplayPosition.BottomLeft;

overlay.AddSection("Game", static () =>
[
    new("INSTRUCTIONS"),
    new("Press P to see collidables"),
    new("Press F11 to see debug meshes"),
    new("Press R to reset the scene", Color.Yellow),
]);
```

Each `TextElement` is a line and an optional colour; a line without one uses `DefaultTextColor`.
Sections are separated by a blank line and sorted by `order`. The 3D camera controller registers its
help at order `-100`, so anything added without an order lands below it.

A section can collapse to a single title line and expand again on a key:

```csharp
overlay.AddCollapsibleSection("Physics", "Physics", Keys.F5, () => [ ... ], collapsed: true);
```

That is how `F2 - Camera controls [+]` works. `AddSection` returns the `DebugOverlaySection`, so a
section can be disabled, collapsed or removed later.

> [!TIP]
> Something that already knows how to describe itself as lines - `DebugTextDropdown.GetLines()`, for
> example - plugs straight in: `overlay.AddSection("Spawn", () => spawnMenu.GetLines())`. That is what
> keeps the spawn menu in `Example01_Basic2DScene_SpawnMenu` in the same block as the camera help
> instead of being a second patch of text somewhere else.

## Keys and position

| Key or property | Default | Does |
|---|---|---|
| `ToggleKey` | <kbd>F4</kbd> | Hides and shows the whole overlay. |
| `RepositionKey` | <kbd>F3</kbd> | Cycles the corner: top-left, top-right, bottom-right, bottom-left. |
| `Position` | `TopRight` | The corner. `DisplayPosition.Custom` uses `CustomPosition` instead; `None` draws nothing. |
| `Margin` | `5, 10` | Distance from the corner, in unscaled pixels. |

Section keys are read even while the overlay is hidden, so a collapse toggle pressed with everything
off still takes effect.

> [!NOTE]
> The camera controllers claim <kbd>W</kbd> <kbd>A</kbd> <kbd>S</kbd> <kbd>D</kbd>, <kbd>Q</kbd> <kbd>E</kbd>,
> the arrow keys, <kbd>H</kbd>, <kbd>F2</kbd> and <kbd>F3</kbd>. Bind section toggles and example
> keys elsewhere; safe single letters include <kbd>G</kbd> <kbd>J</kbd> <kbd>K</kbd> <kbd>L</kbd>
> <kbd>M</kbd> <kbd>N</kbd> <kbd>P</kbd> <kbd>R</kbd> <kbd>T</kbd> <kbd>Z</kbd>.

## Size, and high-DPI displays

The overlay draws with a real font, rasterised at the size asked for, so it can be any size and is
sharp at every one of them. Two properties control it:

- **`Scale`** multiplies everything - text, line spacing, margins, padding - so the block keeps its
  layout. Set it from the display's DPI factor and the overlay reads the same on a 4K screen at 200%
  as on a 1080p one. This is `Example_Charts_Playground`:

  ```csharp
  overlay.Scale = MathF.Max(1f, WindowsDpiManager.GetPrimaryScale() ?? 1f);
  ```

  `WindowsDpiManager` is in the `Stride.CommunityToolkit.Windows` package and returns `null` off
  Windows, so the line is safe everywhere. Any factor works, including `1.5`.
- **`FontSize`** is the text height at scale 1, in pixels. Defaults to 16, the height of Stride's own
  debug text.

Why not just scale Stride's debug text? Because it is a bitmap: an 8 by 16 pixel glyph sheet that
`DebugTextSystem` draws at exactly that size, with a grey strip baked behind every glyph. The first
attempt at enlarging it - a transform over the small texture - came out blurred; the second - doubling
every pixel - came out sharp but blocky next to the vector text everywhere else on a 4K desktop. A
rasterised font is what makes debug text look like the rest of the screen, and it is what the toolkit's
[entity text](entity-text.md) already used.

## Fonts

Stride's default font asset is **bold**. The overlay looks for an installed system font instead,
picked by `FontFamily`:

| `FontFamily` | Windows | macOS | Linux |
|---|---|---|---|
| `Monospace` (default) | Consolas, Cascadia Mono, Courier New | Menlo, Courier New | DejaVu Sans Mono, Liberation Mono, Courier New |
| `SansSerif` | Segoe UI, Arial | Helvetica, Arial | Liberation Sans, DejaVu Sans, Arial |

The first family found in the system font folders wins. Monospace is the default for the same reason
Stride's own debug font is fixed-width: columns line up, a number does not shift its neighbours as its
digits change, and text aligned with spaces keeps its shape - `Example22_Instancing_EntityTransform`
lays out its comparison table with `{count,6}` and would fall apart in a proportional font.

To be specific rather than pick a family:

- `FontName = "Cascadia Mono"` names an installed family; `FontStyle` chooses `Regular`, `Bold`,
  `Italic` or `BoldItalic`.
- `FontFile = "/path/to/font.ttf"` points at a file that is not in the system font folders.
- `Font = someSpriteFont` supplies a font asset of your own and overrides all of the above.
- `FontName = null` with a family none of whose fonts are installed falls back to Stride's default
  font. So does any font that fails to load - with one line in the log saying which font was wanted,
  never a blank overlay.

The lookup finds files by the common naming conventions (`consola.ttf`, `LiberationMono-Regular.ttf`,
`DejaVuSansMono.ttf`); it does not consult fontconfig, so an unusually named file needs `FontFile`.

## Background

Each line gets a strip behind it, exactly as wide as its text:

| Property | Default | Notes |
|---|---|---|
| `BackgroundColor` | black at 49% alpha | The look of Stride's debug text. `Color.Transparent` draws no strip. |
| `BackgroundPadding` | `3, 1` | How far the strip extends beyond the text, in unscaled pixels. |
| `DefaultTextColor` | `LightGreen` | For lines that give no colour. |
| `TitleColor` | `null` | Section titles; `null` means `DefaultTextColor`. |

On a light scene the default strip is too faint to carry light-coloured text. Darken it, or invert
the scheme:

```csharp
// The charts playground, paper-white 2D scene
overlay.BackgroundColor = new Color(0, 0, 0, 200);

// Or a light strip with dark text
overlay.BackgroundColor = new Color(255, 255, 255, 230);
overlay.DefaultTextColor = Color.Black;
```

The strip is drawn by the overlay, not by the font. This is worth knowing only because Stride's own
debug text is the other way round - its strip is baked into the glyphs at a fixed 49% black and cannot
be changed, which is why a text-only fix was never going to make it readable on white.

## Line spacing

`LineHeight` is `null` by default, which means the distance between lines is worked out from the
font: text height, plus the padding above and below, plus `LineSpacing` (default `2` pixels). Change
`FontSize` and the lines follow. `LineSpacing = 0` makes the strips touch; a fixed `LineHeight` in
unscaled pixels overrides the calculation entirely.

## What it is not

- **Not part of the graphics compositor.** It draws straight to the back buffer after everything else,
  so it is always on top and unaffected by post effects, and it needs nothing registered in the
  compositor.
- **Not the `DebugTextDropdown.Draw(DebugTextSystem)` path.** A dropdown drawn standalone that way
  still uses Stride's bitmap text at its own position; hand its `GetLines()` to the overlay instead to
  get the font, scale, background and hide key.
- **Not a UI toolkit.** One block of lines in a corner. Anything laid out in the scene wants
  [entity text](entity-text.md) or [world text](world-text.md); anything interactive wants Stride UI
  or the ImGui package.

## Where it is used in the toolkit

| Piece | In `Example_Charts_Playground` | Concept |
|---|---|---|
| `DebugOverlay.GetOrCreate(game)` | `Program.cs`, `Start` | one shared instance, registered as a service |
| `overlay.Scale = ... GetPrimaryScale()` | `Program.cs` | DPI-independent size |
| `overlay.BackgroundColor = new Color(0, 0, 0, 200)` | `Program.cs`, 2D branch | readable on a white scene |
| `overlay.AddSection("Chart", () => [...])` | `Program.cs` | a live line: `Press G to toggle the grid (on)` |
| `Add3DCameraController()` | via `SetupBase3DScene` | the collapsible `F2 - Camera controls` section, order `-100` |

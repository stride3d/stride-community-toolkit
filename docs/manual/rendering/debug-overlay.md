# Debug Overlay

`DebugOverlay` is one block of on-screen text, assembled from **sections** contributed by whatever has
something to say - the camera controller's key help, your own instructions, a live counter - with one
position and one hide key for the lot. It is a game system, so it survives scene swaps and draws
itself every frame; nothing calls it.

Use it instead of `game.DebugTextSystem.Print(...)` from several scripts, whose hand-placed lines
overlap and are unreadable on a high-DPI display.

`DebugOverlay`, `DebugOverlaySection` and `TextElement` live in the
`Stride.CommunityToolkit.Rendering.Text` namespace; `ScreenAnchor` in `Stride.CommunityToolkit.Rendering`.

## Adding text

Get the shared instance and add a section. The callback runs every frame the overlay is drawn, so
values that change need no pushing - this is `E02_3D_Primitives`:

```csharp
var overlay = DebugOverlay.GetOrCreate(game);

overlay.AddSection("Game", static () =>
[
    new("P", "Show or hide the collidable gizmos", Color.Gold),
    new("F11", "Show or hide the debug meshes", Color.Gold),
    new("R", "Reset the scene", Color.Gold),
]);
```

Each `TextElement` is a line, an optional key and an optional colour; a line without a colour uses
`DefaultTextColor`. Sections are sorted by `order` and separated by `SectionGap` blank lines (default
`1`; `0` runs them together). The camera controllers register their help at order `-100`, so anything
added without an order lands below it.

A section can collapse to a single title line and expand again on a key:

```csharp
overlay.AddCollapsibleSection("Physics", "Physics", Keys.F5, () => [ ... ], collapsed: true);
```

That is how `[+] [F2] Camera controls` is drawn: the marker, then the key, then the title, with the body indented one marker. `AddSection` and `AddCollapsibleSection` return the `DebugOverlaySection`, so a section can be disabled, collapsed or removed later.

Keys are data, not text. A help line names them - `new("H", "Reset camera", Color.Gold)`, or
`new(["Q", "E"], "Ascend / descend")` for keys that do the same thing, or `new("Arrow keys", "Move")` -
and the overlay decorates them when it draws: each key through `KeyFormat` (default `[{0}]`), joined by
`KeySeparator`, in `KeyColor`, which defaults to the line's colour blended halfway to white.
`overlay.KeyFormat = "{0}:"` restyles every help line from `[F2]` to `F2:`. A line with no keys is
drawn as given.

`BlockBounds` is the rectangle the block was last drawn in, in screen pixels; `E02_2D_Easing` reads it to place its panel under the block each frame.

> [!TIP]
> Something that already knows how to describe itself as lines - `DebugTextDropdown.GetLines()`, for
> example - plugs straight in: `overlay.AddSection("Spawn", () => spawnMenu.GetLines())`, as
> `E04_2D_SpawnMenu` does.

## Keys and position

| Key or property | Default | Does |
|---|---|---|
| `ToggleKey` | <kbd>F4</kbd> | Hides and shows the whole overlay. |
| `RepositionKey` | <kbd>F3</kbd> | Cycles the corners: top-left, top-right, bottom-right, bottom-left. |
| `Position` | `TopRight` | The `ScreenAnchor`: a corner, the middle of an edge or the centre. `null` uses `CustomPosition` instead; `Visible = false` draws nothing. |
| `Margin` | `5, 10` | Distance from the edges, in unscaled pixels. |

Section keys are read even while the overlay is hidden, so a collapse toggle pressed with everything
off still takes effect.

> [!NOTE]
> The camera controllers claim <kbd>W</kbd> <kbd>A</kbd> <kbd>S</kbd> <kbd>D</kbd>, <kbd>Q</kbd> <kbd>E</kbd>,
> the arrow keys, the numeric keypad, <kbd>H</kbd> and <kbd>F2</kbd>; the overlay itself claims
> <kbd>F3</kbd> and <kbd>F4</kbd>. Bind section toggles and example
> keys elsewhere; <kbd>G</kbd> is the reference grid's and <kbd>T</kbd> the grabbers'. Safe single
> letters include <kbd>J</kbd> <kbd>K</kbd> <kbd>L</kbd> <kbd>M</kbd> <kbd>N</kbd> <kbd>P</kbd> <kbd>R</kbd> <kbd>Z</kbd>.

## Size, and high-DPI displays

The overlay draws with a font rasterised at the size asked for, so it is sharp at any size. It
follows the display by default: on a laptop at 150% it draws one and
a half times larger than on a monitor at 100%, so it reads the same size to the eye on both. The
factor comes from `DisplayScale`, which is shared with everything else that draws in pixels and is
re-read when the window moves to a differently scaled monitor. Three properties control the rest:

- **`AutoScale`** is that behaviour, on by default. Turn it off to draw at exactly `Scale`, for a
  screenshot at a known size or when the game applies its own UI-scale setting.
- **`Scale`** multiplies everything - text, line spacing, margins, padding - on top of the display's
  factor, so the block keeps its layout. It is a preference, "a bit bigger", not a DPI figure:
  `1.25` on a 150% display draws at 1.875.
- **`FontSize`** is the text height at scale 1 on a 100% display, in pixels. Defaults to 14.

The display factor is only right if the process is DPI aware - otherwise Windows is already
stretching the whole window and the factor correctly reads 1. Declare awareness in an
`app.manifest`, as the Stride templates do, or call `WindowsDpiManager.EnablePerMonitorV2()` (in the core
package, namespace `Stride.CommunityToolkit.Engine`) before the game is created; see the
[DPI-aware example](../code-only/examples/dpi-aware.md).

## Fonts

Stride's default font asset is **bold**. The overlay looks for an installed system font instead,
picked by `FontFamily`:

| `FontFamily` | Windows | macOS | Linux |
|---|---|---|---|
| `Monospace` (default) | Consolas, Cascadia Mono, Courier New, DejaVu Sans Mono | Menlo, Courier New, DejaVu Sans Mono | DejaVu Sans Mono, Liberation Mono, Courier New |
| `SansSerif` | Segoe UI, Arial, DejaVu Sans | Helvetica, Arial, DejaVu Sans | Liberation Sans, DejaVu Sans, Arial |

The first family found in the system font folders wins. Monospace is the default so that columns
line up and a number does not shift its neighbours as its digits change.

To be specific rather than pick a family:

- `FontName = "Cascadia Mono"` names an installed family; `FontStyle` chooses `Regular`, `Bold`,
  `Italic` or `BoldItalic`.
- `FontFile = "/path/to/font.ttf"` points at a file that is not in the system font folders.
- `Font = someSpriteFont` supplies a font asset of your own and overrides all of the above.
- When none of the candidates is installed, or the named font fails to load, the overlay falls back to
  Stride's default font and logs one warning naming the fonts tried.

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
// E11_2D_Charts, a white 2D scene
overlay.BackgroundColor = new Color(0, 0, 0, 200);

// Or a light strip with dark text
overlay.BackgroundColor = new Color(255, 255, 255, 230);
overlay.DefaultTextColor = Color.Black;
```

## Line spacing

`LineHeight` is `null` by default, which means the distance between lines is worked out from the
font: text height, plus the padding above and below, plus `LineSpacing` (default `2` pixels). Change
`FontSize` and the lines follow. `LineSpacing = 0` makes the strips touch; a fixed `LineHeight` in
unscaled pixels overrides the calculation entirely.

## What it is not

- **Not part of the graphics compositor.** It draws straight to the back buffer after everything else,
  so it is always on top and unaffected by post effects, and it needs nothing registered in the
  compositor.
- **Not a UI toolkit.** One block of lines in a corner. Anything laid out in the scene wants
  [entity text](entity-text.md) or [world text](world-text.md); anything interactive wants Stride UI
  or the ImGui package.

## Where it is used in the toolkit

| Piece | In `E11_2D_Charts` | Concept |
|---|---|---|
| `DebugOverlay.GetOrCreate(game)` | `Program.cs`, `Start` | one shared instance, registered as a service |
| `WindowsDpiManager.EnablePerMonitorV2()` | `Program.cs`, before `new Game()` | a sharp window; the overlay's size then follows `DisplayScale` on its own |
| `overlay.BackgroundColor = new Color(0, 0, 0, 200)` | `Program.cs`, `Start` | readable on a white scene |
| `overlay.AddSection("Chart", () => [...])` | `Program.cs`, `Start` | a live key line: `[G] Grid on` |
| `SceneSetup2D` | `Program.cs`, `game.Run(...)` | adds the 2D camera controller, whose collapsible `[+] [F2] Camera controls` section is at order `-100` |
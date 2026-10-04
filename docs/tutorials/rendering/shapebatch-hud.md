# Build a simple HUD with ShapeBatch

**Level:** Beginner

In this tutorial you draw a HUD over a running game, one piece at a time: a panel, a health bar, an
energy dial, a crosshair, labels, a warning light and a damage flash. Each step adds a few lines and
one idea. The last step moves the finished HUD out of `Program.cs` into files of its own.

![The finished HUD: a health panel, an energy dial, a crosshair and a warning light](media/shapebatch-hud-final.webp)

You will learn how to:

- Draw shapes on the screen, in pixels, anchored to a corner of the window.
- Control a shape's outline and fill.
- Redraw a shape from a value every frame, so it always shows the current state of the game.
- Draw arcs, rings and lines.
- Add text beside shapes.
- Use glow and opacity.
- Layer shapes by the order in which you draw them.
- Organise a HUD as a class.

This is the first part. It only draws. A later part will make the HUD respond to the mouse.

## Before you start

You need a project that runs a toolkit scene. If you have none, follow
[Create Project](../../manual/code-only/create-project.md) first, then add the shapes package:

```
dotnet add package Stride.CommunityToolkit.Shapes --prerelease
```

The finished program is the example
[HUD Basics (Step by Step)](../../manual/code-only/examples/hud-basics.md). Run it and press the keys
**1** to **8** to see the HUD as it stands after each step. Every code block on this page is taken
from that program, so what you read here is code that compiles and runs.

Two things make the steps easy to follow:

- A step only adds code. Nothing written in an earlier step is changed later.
- Each piece of the HUD is one small function. A step writes the function, and you add one call to it.

## The scene behind the HUD

A HUD sits over a game, so the program starts with something to look at: the thirty falling shapes
of [Basic 2D Scene (Falling Shapes)](../../manual/code-only/examples/falling-shapes-2d.md). Any scene
will do.

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/Program.cs#Scene)]

The program has the two functions every toolkit example has. `Start` runs once and builds the scene.
`Update` runs every frame. The HUD is created in `Start` and drawn in `Update`.

## Step 1: A panel in a corner

`ShapeBatch` draws shapes: rectangles, discs, rings, arcs and lines. Create one batch in `Start` and
keep it.

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/Program.cs#Step1Create)]

The panel needs a few values. Put them at the top of the file.

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/Program.cs#Step1Values)]

Now the drawing. `PanelCentre` works out where the panel goes, and `DrawPanel` draws.

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/Program.cs#Step1)]

Call it from `Update`, between the two lines that switch screen drawing on and off:

```csharp
shapes.Screen = true;

DrawPanel();

shapes.Screen = false;
```

![Step 1: a dark rounded panel in the bottom left corner](media/shapebatch-hud-step1.webp)

What to notice:

- **Pixels.** With `Screen` set to `true`, positions and sizes are window pixels. The origin is the top
  left corner and Y points down, which is why the panel's offset from the bottom corner has a minus.
- **A corner and an offset.** `Corner(ScreenCorner.BottomLeft)` is asked for every frame, so the panel
  stays in its corner when the window is resized. Try it.
- **Outline and fill.** Every shape is an outline and a fill. The outline takes the colour you pass
  to the draw call. `BorderWidth` sets how wide it is, and `Fill` sets what is inside: here a colour of
  its own, at 80% so the scene shows through. Left at its default, the fill is the outline's colour at
  full strength, which draws a solid shape.
- **After the post effects.** `afterPostEffects: true` draws the batch after the scene has been tone
  mapped, so the colours you write are the colours you get.

More: [Create a batch and draw](../../manual/rendering/shape-batch.md#create-a-batch-and-draw),
[Draw on the screen](../../manual/rendering/shape-batch.md#draw-on-the-screen),
[Draw after post effects](../../manual/rendering/shape-batch.md#draw-after-post-effects).

## Step 2: A health bar

The bar is two rectangles: a track that is an outline, and a bar inside it whose width is the health.

Add the values:

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/Program.cs#Step2Values)]

Add two keys in `Update`, so there is a health to watch change:

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/Program.cs#Step2Update)]

Then the drawing. The two helpers at the top set the outline and the fill for the two kinds of shape
a HUD is mostly made of.

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/Program.cs#Step2)]

Add `DrawHealthBar();` after `DrawPanel();`.

![Step 2: a green health bar inside the panel](media/shapebatch-hud-step2.webp)

What to notice:

- **Nothing is stored.** The batch keeps no shapes between frames. Every frame you draw the bar again,
  as wide as the health is at that moment. That is why there is no code to "update" the bar: press
  **J** and **K** and it follows.
- **The state is set before the draw call.** `BorderWidth` and `Fill` are settings of the batch. A draw
  call takes them as they are at that moment. `Outline()` and `Solid()` are just names for the two
  settings used most.
- **`Fill.Set(null, ...)`.** A fill colour of `null` means "the colour of the draw call". The second
  value is the strength: 0 leaves the shape empty, 1 fills it.

More: [How it works](../../manual/rendering/shape-batch.md#how-it-works),
[Draw state](../../manual/rendering/shape-batch.md#draw-state).

## Step 3: An energy dial

A second value, shown as a ring that fills clockwise, in the opposite corner.

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/Program.cs#Step3Values)]

The game has no energy yet, so `Update` makes one that moves by itself:

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/Program.cs#Step3Update)]

The dial is two arcs: the whole circle in a dim colour, and over it the part that is full.

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/Program.cs#Step3)]

Add `DrawEnergyDial();` after `DrawHealthBar();`.

![Step 3: an orange energy dial in the bottom right corner](media/shapebatch-hud-step3.webp)

What to notice:

- **Angles.** An arc takes a start angle and a sweep, in radians. Zero points right and a positive
  angle turns clockwise, so `-MathF.PI * 0.5f` is the top. `MathF.Tau` is a full turn.
- **An arc is a band.** Its last argument is the band's width. The band is filled like any other
  shape, which is why the function starts with `Solid()`.
- **Set what you need.** The state stays as the last piece left it. Here the bar happened to leave
  `Solid()` behind, and the dial would look right without its own call. It would also break the day
  you draw something else in between. Each piece sets its own state.

More: [Draw on the screen](../../manual/rendering/shape-batch.md#draw-on-the-screen).

## Step 4: A crosshair

A ring, four ticks and a dot, in the middle of the window.

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/Program.cs#Step4)]

Add `DrawCrosshair();` after `DrawEnergyDial();`.

![Step 4: a white crosshair in the middle of the window](media/shapebatch-hud-step4.webp)

What to notice:

- **`ScreenSize`.** Half of it is the middle of the window, whatever the window's size.
- **A ring is the outline of a disc.** `Outline()` gives it a width of 2 pixels and nothing inside.
- **Pixel lines.** `DrawPixelLine` takes two ends and a width in pixels.
- **Sharp at any size.** The shapes are computed per pixel, not built from triangles, so a 2 pixel
  ring has a smooth edge.

## Step 5: Labels

`ShapeBatch` does not draw text. Text comes from a component, `EntityTextComponent`, placed at a
position on the screen. Register its renderer and make two labels in `Start`:

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/Program.cs#Step5Create)]

`AddLabel` creates a label, and `UpdateLabels` writes its text and moves it:

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/Program.cs#Step5)]

Add `UpdateLabels();` after `DrawCrosshair();`.

![Step 5: the health and energy values as text](media/shapebatch-hud-step5.webp)

What to notice:

- **A label is kept, a shape is not.** A shape exists for one frame: stop drawing it and it is gone.
  A label is an entity in the scene: it stays until you change or remove it. So labels are made once
  and then only rewritten.
- **The same pixels.** `ScreenPosition` uses the same coordinates as the shapes, so the label is
  placed from `PanelCentre()` like the bar under it.
- **Anchors.** `TextAnchor.BottomLeft` puts the position at the text's bottom left corner, so the
  label sits on a line above the bar. `MiddleCenter` centres the number in the dial.

More: [Text, display scale and components](../../manual/rendering/shape-batch.md#text-display-scale-and-components),
[Entity Text](../../manual/rendering/entity-text.md).

## Step 6: A warning light

A light at the top of the window that comes on when the health is low, with a soft halo, and pulses.

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/Program.cs#Step6)]

Add `DrawWarningLight(seconds);` after `UpdateLabels();`, where `seconds` is the game's total time:

```csharp
var seconds = (float)time.Total.TotalSeconds;
```

Press **J** until the bar turns red.

![Step 6: low health, a red bar and a glowing warning light](media/shapebatch-hud-step6.webp)

What to notice:

- **Glow.** `Glow.Set(18f)` adds a halo 18 pixels wide outside the shape, in the shape's colour.
  `Glow.Strength` sets how strong it is.
- **Opacity.** `Opacity` multiplies the transparency of everything a draw call draws: outline, fill
  and glow. Driving it with a sine of the time makes the light pulse.
- **Put the state back.** Glow and opacity stay set like every other state. If the function did not
  clear them, next frame's panel would glow and pulse too. `Outline()` and `Solid()` do not touch
  them, so the function that sets them clears them.
- **Not drawing is hiding.** When the health is fine the function returns before it draws. There is
  nothing to switch off.

More: [Glow](../../manual/rendering/shape-batch.md#glow).

## Step 7: A damage flash

When a hit lands, the whole window flashes red and fades.

Add a value for how strong the flash is:

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/Program.cs#Step7Values)]

Start it on a hit and fade it, in `Update`:

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/Program.cs#Step7Update)]

Draw it as one rectangle the size of the window:

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/Program.cs#Step7)]

This call does not go at the end. It goes first:

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/Program.cs#DrawOrder)]

That block is `Update`'s drawing as the example has it. The `if (step >= ...)` in front of each call
is there so the number keys can show the earlier steps. In your own program leave the `if`s out.

![Step 7: the window tinted red by a hit](media/shapebatch-hud-step7.webp)

What to notice:

- **Order is depth.** Shapes are drawn in the order you submit them. The flash is submitted first,
  so the panel, the dial and the crosshair are drawn over it and stay clear.
- **A value that fades.** The flash is one number that a hit sets to 1 and time takes back to 0.
  The drawing only reads it.

## Step 8: Move the HUD into its own files

The HUD is finished. `Program.cs` now holds four things: the values, the keys, the game's numbers
and the drawing. That is fine for learning and awkward for a game, where the HUD should be something
you create and call. This step moves it out. Nothing about the drawing changes.

**The values go into a style.** The sizes and colours from the top of `Program.cs` become properties
of one class, `HudStyle.cs`:

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/HudStyle.cs)]

**The drawing goes into a class.** `SimpleHud.cs` takes the batch and a style, keeps the labels, and
offers three numbers to set and one method to call:

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/SimpleHud.cs#Step8Class)]

The rest of the class is the functions from steps 1 to 7, moved as they are. The only difference is
where values come from: `panelSize` is now `_style.PanelSize`, and `health` is the `Health` property.
See the whole file in the [example](../../manual/code-only/examples/hud-basics.md).

**`Program.cs` creates it and calls it.** In `Start`:

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/Program.cs#Step8Create)]

In `Update`:

[!code-csharp[](../../../examples/code-only/E03_2D_HUD_Basics/Program.cs#Step8Update)]

Press **8** in the example: the picture is the same as at step 7, drawn by the class.

What to notice:

- **The HUD does not know about the game.** It has no keys and no idea how health is worked out. The
  game sets three numbers and the HUD shows them. That is what lets you move it to another game.
- **A second look is a second style.** `new HudStyle { PanelEdge = Color.Orange }` gives a HUD with an
  orange edge, without touching the drawing.
- **One piece per method.** Each part of the HUD is still one small method. A larger HUD goes one
  step further and gives each part its own class and file, as the full example below does.

## The finished program

The example [HUD Basics (Step by Step)](../../manual/code-only/examples/hud-basics.md) is the whole
program. From a clone of the repository:

```
dotnet run --project examples/code-only/E03_2D_HUD_Basics
```

| Key | Action |
|---|---|
| 1 to 8 | Show the HUD as it stands after that step |
| J | Take a hit |
| K | Heal |

## Where next

- [ShapeBatch](../../manual/rendering/shape-batch.md), the reference for everything used here and
  for what this part left out: dashes, gradients, textured fills, shapes in the world.
- [ShapeBatch gallery](../../manual/code-only/examples/shape-batch.md), every feature as a station you can walk up to.
- [Ship HUD](../../manual/code-only/examples/hud.md), a full HUD in the same style: one widget per file,
  colour schemes, and panels that answer the mouse.
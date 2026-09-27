---
generated: true
slug: hud
---

# Ship HUD

A cockpit HUD composed from the toolkit's shapes and world text, with one widget per file. A
frame with cut corners holds three columns of panels: a radar, a contacts table and a comms log on the
left, the heading tape, the sight with its pitch ladder, the speed and altitude tapes, two
traces, a spectrum, ring gauges and the systems bars in the middle, and the target, the wing,
the power triangle and the mode buttons on the right. Panels are interactive: click a contact
to target it, a wing tile to select it, a mode button to switch it, a corner of the triangle to
move power there. The panels' glass is a shader fill with two patterns, lines and squares. Ten
colour schemes switch live, nine in one colour and one that colours by function. Every shape is in one draw call and the ship flies
itself.

The `Program.cs` file shows how to:

- Splitting a HUD into one widget per file, each drawing inside the rectangle it is given
- A layout of rows and columns with one padding, so a widget fits any column
- An immediate-mode canvas - labels created on first use and hidden when not drawn
- Hover and click through ShapeBatch picking - tag a shape, ask the batch what is under the pointer
- Invisible tagged discs as hit areas for small things
- A shader class as the batch's fill source, sampled only by the shapes drawn as textured
- One shader with two patterns, chosen by a value composed in from C#
- A theme that answers roles, so one scheme can colour by function
- Values that ease towards their target for hover, selection and a fade-in on a scheme change
- A trace sampled as a function of time, so it scrolls every frame
- Additive glows for the things that emit light

![Ship HUD](media/hud.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E03_2D_HUD).

[!code-csharp[](../../../../examples/code-only/E03_2D_HUD/Program.cs?start=1&end=184)]
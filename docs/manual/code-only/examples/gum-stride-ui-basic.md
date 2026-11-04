---
generated: true
slug: gum-stride-ui-basic
---

# Basic Gum UI Setup

Initialise Gum UI in Stride using the official Gum.Stride runtime and the Stride Community Toolkit.
Demonstrates setting up GumService with the Game instance, whose renderer ticks the UI every frame,
and building interactive UI layouts in C# with Gum Forms controls such as StackPanel, Label, Button,
CheckBox, ComboBox, and ListBox, complete with styling and event handling.

The `Program.cs` file shows how to:

- Initialising Gum UI via GumService.Default.Initialize(game)
- No per-frame Update call: the renderer Initialize registers ticks Gum itself
- Setting up scene graphics compositing and a 2D camera with Stride Community Toolkit
- Constructing UI hierarchy using Gum.Forms controls (StackPanel, Label, Button, CheckBox, ComboBox, ListBox)
- Handling UI events and modifying visual properties such as background colour

![Basic Gum UI Setup](media/gum-stride-ui-basic.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E04_GumUI).

[!code-csharp[](../../../../examples/code-only/E04_GumUI/Program.cs?start=1&end=72)]
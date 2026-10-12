---
generated: true
slug: gum-stride-ui-basic
---

# Basic Gum UI Setup

Initialize Gum UI in Stride using the official Gum.Stride runtime and the Stride Community Toolkit.
Demonstrates setting up GumService with the Game instance, updating the UI layout in the game loop,
and building interactive UI layouts in C# with Gum Forms controls such as StackPanel, Label, Button,
CheckBox, ComboBox, and ListBox, complete with styling and event handling.

The `Program.cs` file shows how to:

- Initializing Gum UI via GumService.Default.Initialize(game)
- Driving Gum layout updates using GumService.Default.Update(gameTime)
- Setting up scene graphics compositing and a 2D camera with Stride Community Toolkit
- Constructing UI hierarchy using Gum.Forms controls (StackPanel, Label, Button, CheckBox, ComboBox, ListBox)
- Handling UI events and modifying visual properties such as background color

![Basic Gum UI Setup](media/gum-stride-ui-basic.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E04_GumUI).

[!code-csharp[](../../../../examples/code-only/E04_GumUI/Program.cs?start=1&end=89)]
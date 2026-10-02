---
generated: true
slug: material-preview
---

# Material Preview

Game Studio's material thumbnail rebuilt in code, with the editor's own numbers: the forward renderer
with no post effects on a grey background, a camera pitched and turned the same way, the three
thumbnail lights, and the model scaled to its unit bounding sphere. Pick a shape and a material, then
press G to see the same material the way a game shows it, with post effects and a skybox - and why a
metal that looks dull in the editor shines in the game.

The `Program.cs` file shows how to:

- The editor's thumbnail rig - compositor, camera, lights and framing - as code
- A compositor without post effects, and swapping compositors at runtime
- Why a metal needs an environment, and why the thumbnail barely has one
- Scaling any model to its unit bounding sphere so every shape frames the same
- Keyboard dropdowns for shape and material with DebugTextDropdown

![Material Preview](media/material-preview.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E02_3D_MaterialPreview).

[!code-csharp[](../../../../examples/code-only/E02_3D_MaterialPreview/Program.cs?start=1&end=252)]
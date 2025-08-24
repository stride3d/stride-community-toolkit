# ImGui UI

This example demonstrates how to integrate ImGui with the Stride game engine to render an immediate‑mode UI overlay for in‑game tools, debugging, and live controls.

The `Program.cs` file shows how to:
- Initialize and configure the ImGui integration
- Create a scene and camera and hook UI rendering into the frame loop
- Draw ImGui windows, menus, and controls every frame
- Handle input events and toggle UI visibility
- Display real‑time stats or debug panels

[!INCLUDE [note-additional-packages](../../../includes/manual/examples/note-additional-packages.md)]

![ImGui UI Example](media/stride-game-engine-example11-imgui-ui.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/Example11_ImGui).

[!code-csharp[](../../../../examples/code-only/Example11_ImGui/Program.cs)]
---
generated: true
slug: imgui-net
---

# ImGui.NET Text Rendering

Render debug text with ImGui.NET, both in screen space and anchored to positions in the 3D scene.
A kinematic Bepu body follows a circular path and knocks over stacks of dynamic boxes, showing why
a velocity moves a physics body while writing Transform.Position does not. The ImGui font atlas
is rebuilt for the monitor's DPI so the overlay stays crisp on high-DPI displays.

The `Program.cs` file shows how to:

- Drawing screen-space text with DrawText
- Anchoring text to a world-space position
- Driving a kinematic BodyComponent with LinearVelocity from a per-frame update
- Why writing Transform.Position does not move a physics body
- Rebuilding the ImGui font atlas for the window DPI
- Using helpers: AddImGuiNet, SetupBase3DScene, AddProfiler

> [!NOTE]
> This example references `Stride.CommunityToolkit.ImGuiNet`, which is not on NuGet yet. Run it from a clone of the
> repository, where the package is a project reference; a copy of the project on its own will not build.

![ImGui.NET Text Rendering](media/imgui-net.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E04_ImGuiNet).

[!code-csharp[](../../../../examples/code-only/E04_ImGuiNet/Program.cs?start=1&end=231)]
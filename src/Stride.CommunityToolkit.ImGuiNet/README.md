# Stride Community Toolkit - ImGui.NET Integration

Dear ImGui for the Stride Game Engine through ImGui.NET, with the text drawing calls of the Box2D.NET samples. It is an alternative to `Stride.CommunityToolkit.ImGui`, which uses Hexa.NET.ImGui.

## Features

- **Box2D.NET-style API**: `DrawString` at a screen position or a world position, as in the Box2D.NET samples
- **Screen and world coordinates**: world positions are projected through the scene's camera
- **DPI aware**: the font atlas is rebuilt when the display scale changes, so text stays sharp
- **Chainable**: the `DrawText` extensions return the system

## Installation

Add a reference to the `Stride.CommunityToolkit.ImGuiNet` project in your Stride application.

```xml
<ProjectReference Include="path\to\Stride.CommunityToolkit.ImGuiNet\Stride.CommunityToolkit.ImGuiNet.csproj" />
```

## Quick Start

```csharp
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.ImGuiNet;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

using var game = new Game();

ImGuiNetSystem? imGuiSystem = null;

game.Run(new SceneSetup3D(), start: Start, update: Update);

void Start(Scene scene)
{
    imGuiSystem = game.AddImGuiNet();
}

void Update(Scene scene, GameTime gameTime)
{
    if (imGuiSystem is null) return;

    // Text at a screen position, in pixels from the top left
    imGuiSystem.DrawString(10, 10, "Hello, ImGui.NET!");

    // Text at a world position, projected through the camera
    imGuiSystem.DrawString(Vector3.Zero, "World Text");
}
```

### Drawing Text at Screen Coordinates

The counterpart of Box2D.NET's `DrawString(int x, int y, string message)`:

```csharp
imGuiSystem.DrawString(10, 50, "Frame Time: 16.7ms");

// Coloured text through the extension methods, red, green, blue and alpha from 0 to 255
imGuiSystem.DrawText(10, 70, "Red Text", 255, 0, 0);
imGuiSystem.DrawText(10, 90, "Semi-transparent", 255, 255, 255, 128);
```

### Drawing Text at World Coordinates

The counterpart of Box2D.NET's `DrawString(B2Vec2 p, string message)`:

```csharp
// Text that follows an entity
imGuiSystem.DrawString(entity.Transform.Position + Vector3.UnitY, "Object Label");

imGuiSystem.DrawText(new Vector3(5, 0, 0), "Waypoint", 255, 255, 0);
```

## API Reference

### `ImGuiNetSystem`

| Member | Description |
| --- | --- |
| `DrawString(int x, int y, string message, Vector4? color = null)` | Text at a screen position. The colour is an ImGui `Vector4`; light grey when omitted |
| `DrawString(Vector3 worldPosition, string message, Vector4? color = null)` | Text at a world position |
| `ShowUI` | Whether the text is drawn; the counterpart of Box2D.NET's `m_showUI` |
| `IsInitialized` | `false` before the system has initialised, and for a game without a graphics device |
| `FontPath`, `FontSize` | A TrueType file to use in place of the default font, and its size in pixels |
| `AutoScaleFonts` | Rebuild the font atlas when the display scale changes. On by default |

### Extension Methods

| Method | Description |
| --- | --- |
| `game.AddImGuiNet()` | Adds the system to the game and returns it |
| `game.GetImGuiNetSystem()` | The system added earlier, or `null` |
| `DrawText(x, y, text)` | White text at a screen position |
| `DrawText(x, y, text, r, g, b, a = 255)` | Coloured text at a screen position |
| `DrawText(worldPosition, text)` | White text at a world position |
| `DrawText(worldPosition, text, r, g, b, a = 255)` | Coloured text at a world position |

## Comparison with Box2D.NET

| Box2D.NET | Stride Community Toolkit ImGui.NET |
|-----------|-------------------------------------|
| `draw.DrawString(10, 20, "text")` | `imGuiSystem.DrawString(10, 20, "text")` |
| `draw.DrawString(worldPos, "text")` | `imGuiSystem.DrawString(worldPos, "text")` |
| `draw.m_showUI` | `imGuiSystem.ShowUI` |
| `ImGui.Begin("Overlay", ImGuiWindowFlags.NoTitleBar \| ...)` | Handled by the system |

## Technical Notes

- **Queueing**: draw calls are queued, drawn once in the frame they were made and then cleared, so call them every frame
- **Threading**: call from the game thread, as for the rest of Stride

## Example Projects

See `examples/code-only/E04_ImGuiNet/Program.cs` for a complete working example.

## Dependencies

- ImGui.NET 1.91.6.1
- Stride.CommunityToolkit
- Stride Game Engine 4.4

## Relationship to Existing ImGui Integration

`Stride.CommunityToolkit.ImGui` (Hexa.NET.ImGui) is the package for windows, widgets and the debug tools. This one draws text through ImGui.NET with the Box2D.NET calls, for a project that already depends on ImGui.NET or ports a Box2D.NET sample.
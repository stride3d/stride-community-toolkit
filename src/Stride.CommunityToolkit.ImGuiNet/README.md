# Stride Community Toolkit - ImGui.NET Integration

This library provides ImGui.NET integration for the Stride Game Engine, offering Box2D.NET-style text rendering capabilities. It serves as an alternative to the existing Hexa.NET.ImGui implementation in the toolkit.

## Features

- **Box2D.NET-style API**: Familiar `DrawString` methods similar to those found in Box2D.NET samples
- **Screen and World Coordinates**: Render text at both screen coordinates and world positions
- **ImGui.NET Integration**: Uses the popular ImGui.NET library for immediate mode GUI rendering
- **Fluent API**: Chainable extension methods following Stride Community Toolkit patterns
- **Lightweight**: Focuses specifically on text rendering scenarios

## Installation

Add a reference to the `Stride.CommunityToolkit.ImGuiNet` project in your Stride application.

```xml
<ProjectReference Include="path\to\Stride.CommunityToolkit.ImGuiNet\Stride.CommunityToolkit.ImGuiNet.csproj" />
```

## Quick Start

### Basic Setup

```csharp
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.ImGuiNet;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

ImGuiNetSystem? imguiSystem = null;

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    game.SetupBase3DScene();
    
    // Initialize ImGui.NET system
    imguiSystem = game.AddImGuiNet();
}

void Update(Scene scene, GameTime gameTime)
{
    // Draw text at screen coordinates (like Box2D.NET)
    imguiSystem?.DrawString(10, 10, "Hello, ImGui.NET!");
    
    // Draw text at world coordinates
    imguiSystem?.DrawString(Vector3.Zero, "World Text");
}
```

### Drawing Text at Screen Coordinates

Similar to Box2D.NET's `DrawString(int x, int y, string message)` method:

```csharp
// Basic white text
imguiSystem.DrawString(10, 50, "Frame Time: 16.7ms");

// Colored text using extension methods
imguiSystem.DrawText(10, 70, "Red Text", 255, 0, 0);
imguiSystem.DrawText(10, 90, "Green Text", 0, 255, 0);
imguiSystem.DrawText(10, 110, "Blue Text", 0, 0, 255);
imguiSystem.DrawText(10, 130, "Semi-transparent", 255, 255, 255, 128);
```

### Drawing Text at World Coordinates  

Similar to Box2D.NET's `DrawString(B2Vec2 p, string message)` method:

```csharp
// Text that follows a 3D object in the world
var entityPosition = myEntity.Transform.Position;
imguiSystem.DrawString(entityPosition + Vector3.UnitY, "Object Label");

// Colored world text
imguiSystem.DrawText(new Vector3(5, 0, 0), "Waypoint", 255, 255, 0);
```

### Advanced Usage

```csharp
void Update(Scene scene, GameTime gameTime)
{
    if (imguiSystem?.IsInitialized != true) return;

    // Control UI visibility (similar to Box2D.NET's m_showUI)
    imguiSystem.ShowUI = showDebugInfo;

    // Performance information
    imguiSystem.DrawString(10, 10, $"FPS: {1.0 / gameTime.Elapsed.TotalSeconds:F1}");
    imguiSystem.DrawString(10, 30, $"Entities: {scene.Entities.Count}");
    
    // Object tracking
    foreach (var entity in scene.Entities.Where(e => e.Name.StartsWith("TrackedObject")))
    {
        var worldPos = entity.Transform.Position;
        imguiSystem.DrawText(worldPos, entity.Name, 255, 255, 0);
    }

    // Debug information
    var camera = scene.Entities.GetCamera();
    if (camera != null)
    {
        var camPos = camera.Transform.Position;
        imguiSystem.DrawString(10, Game.Window.ClientBounds.Height - 20, 
            $"Camera: ({camPos.X:F1}, {camPos.Y:F1}, {camPos.Z:F1})");
    }
}
```

## API Reference

### Core Methods

#### `DrawString(int x, int y, string message, Vector4? color = null)`
Draws text at screen coordinates.
- `x, y`: Screen pixel coordinates
- `message`: Text to display
- `color`: Optional ImGui Vector4 color (defaults to light gray)

#### `DrawString(Vector3 worldPosition, string message, Vector4? color = null)`
Draws text at world coordinates.
- `worldPosition`: 3D world position (Stride Vector3)
- `message`: Text to display  
- `color`: Optional ImGui Vector4 color (defaults to light gray)

### Extension Methods

#### `AddImGuiNet(this Game game)`
Adds and initializes the ImGui.NET system to your game.

#### `DrawText(this ImGuiNetSystem system, int x, int y, string text)`
Draws white text at screen coordinates.

#### `DrawText(this ImGuiNetSystem system, int x, int y, string text, byte r, byte g, byte b, byte a = 255)`
Draws colored text at screen coordinates using RGB values (0-255).

#### `DrawText(this ImGuiNetSystem system, Vector3 worldPosition, string text)`
Draws white text at world coordinates.

#### `DrawText(this ImGuiNetSystem system, Vector3 worldPosition, string text, byte r, byte g, byte b, byte a = 255)`
Draws colored text at world coordinates using RGB values (0-255).

### Properties

#### `bool ShowUI`
Gets or sets whether UI elements should be displayed (similar to Box2D.NET's `m_showUI`).

#### `bool IsInitialized`
Gets whether the ImGui system is initialized and ready for use.

## Comparison with Box2D.NET

This implementation provides a familiar API for developers coming from Box2D.NET:

| Box2D.NET | Stride Community Toolkit ImGui.NET |
|-----------|-------------------------------------|
| `draw.DrawString(10, 20, "text")` | `imguiSystem.DrawString(10, 20, "text")` |
| `draw.DrawString(worldPos, "text")` | `imguiSystem.DrawString(worldPos, "text")` |
| `draw.m_showUI` | `imguiSystem.ShowUI` |
| `ImGui.Begin("Overlay", ImGuiWindowFlags.NoTitleBar \| ...)` | Handled automatically |

## Technical Notes

- **Rendering**: Currently provides the ImGui.NET integration structure; full rendering pipeline integration is a future enhancement
- **World-to-Screen Conversion**: Uses a simplified approximation; proper matrix transformations can be added for precise positioning
- **Performance**: Draw commands are queued and processed each frame, then cleared
- **Threading**: Must be called from the main thread, following Stride's threading requirements

## Example Projects

See `examples/code-only/Example11_ImGuiNet/Program.cs` for a complete working example.

## Dependencies

- ImGui.NET 1.91.6.1+
- Stride.CommunityToolkit
- Stride Game Engine 4.2+

## Relationship to Existing ImGui Integration

This library complements the existing `Stride.CommunityToolkit.ImGui` (which uses Hexa.NET.ImGui) by providing:
- A different ImGui backend (ImGui.NET vs Hexa.NET.ImGui)
- Focus on text rendering scenarios
- Box2D.NET-compatible API
- Alternative for developers who prefer ImGui.NET

Choose the implementation that best fits your project's needs.
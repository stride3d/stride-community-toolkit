# Code-Only Examples

This directory contains code-only examples for the Stride Community Toolkit, demonstrating various features and capabilities of the Stride game engine without requiring the Game Studio editor.

## Level System

Examples are organized into four difficulty levels to help you progress from basic concepts to advanced implementations:

### 🌱 Getting Started (Complexity 1-2)

**Perfect for absolute beginners**

- Single concept demonstrations
- Minimal code (< 30 lines)
- No external dependencies beyond toolkit
- Single file, simple setup

**Examples:** Basic 2D/3D Scene, Simple Primitives, Material creation

### 🎓 Beginners (Complexity 3-5)

**Building on the basics**

- Multiple concepts combined
- Introduces external libraries or patterns
- Moderate code (30-100 lines)
- Simple interactions or updates

**Examples:** Jitter2 Physics, Box2D Physics, Basic UI, Camera controllers

### 🔧 Intermediate (Complexity 6-8)

**Ready for more complex scenarios**

- Complex interactions and systems
- Multiple files/classes
- Advanced patterns (managers, factories)
- State management, custom rendering

**Examples:** Cube Clicker (save/load), UI DragAndDrop, Custom renderers, Raycast interactions

### 🚀 Advanced (Complexity 9-10)

**Production-ready implementations**

- Complete game mechanics
- Multiple systems integration
- Performance optimization
- Production-ready patterns

**Examples:** Cubicle Calamity (full game), Custom shaders, Advanced networking

## Categories

Examples are organized into the following categories:

- **Shapes** - Basic primitive creation and manipulation
- **Physics** - Physics engine integration (Bepu, Bullet, Jitter2, Box2D)
- **Rendering** - Custom rendering, shaders, materials
- **UI** - User interface examples
- **Input** - Keyboard, mouse, touch interactions
- **Interaction** - Raycasting, picking, object manipulation
- **Audio** - Sound effects, music
- **Gameplay** - Complete game mechanics
- **Performance** - Optimization techniques
- **Integration** - External library examples

## Complexity Scoring Guide

| Complexity | Lines of Code | Characteristics |
|------------|---------------|-----------------|
| 1-2 | < 30 | Single concept, minimal setup |
| 3-4 | 30-100 | 2-3 concepts combined |
| 5-6 | 100-200 | Multiple files, state management |
| 7-8 | 200-500 | Complex interactions, custom classes |
| 9-10 | 500+ | Complete systems, production patterns |

## Example Metadata

Each example includes metadata at the end of its `Program.cs` file in YAML format:

```
/* ---example-metadata
title:
  en: Example Title
  cs: Název příkladu
level: Beginners
category: Physics
complexity: 4
description:
  en: | English description of what the example demonstrates
  cs: | Czech description of what the example demonstrates
concepts:
  - First concept demonstrated
  - Second concept demonstrated related:
  - RelatedExample1
  - RelatedExample2 
tags:
  - Tag1
  - Tag2 
order: 1
enabled: true
created: 2025-12-13
---
*/
```

## Running Examples

Each example can be run using:

```bash
dotnet run --project path/to/example/Example.csproj
```

Or by using the Stride Community Toolkit Examples Launcher application.

## Contributing

Before starting, check the [example backlog](../TODO.md) - the idea may already be listed, already
built, or previously declined for a reason worth knowing. It also records which categories have no
examples yet, if you are looking for something useful to write.

When adding new examples:

1. Follow the existing code style and structure
2. Include complete example metadata
3. Assign appropriate level and complexity
4. Add beginner-friendly comments where helpful
5. Keep examples focused on demonstrating specific concepts
6. Test your example before submitting

For more information, see the [Contributing Guide](../../docs/contributing/examples/index.md).
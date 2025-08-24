# Mesh Outline

This example demonstrates how to render an outline effect around meshes using a custom root render feature or shader pass to highlight selected or interactive objects.

The `Program.cs` file shows how to:
- Set up a 3D scene with camera and lighting
- Add the outline render feature to the root compositor
- Configure outline width/color and enable/disable per entity
- Render a few sample meshes with and without outlines

[!INCLUDE [note-additional-packages](../../../includes/manual/examples/note-additional-packages.md)]

![Mesh Outline Example](media/stride-game-engine-example13-mesh-outline.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/Example13_MeshOutline).

[!code-csharp[](../../../../examples/code-only/Example13_MeshOutline/Program.cs)]
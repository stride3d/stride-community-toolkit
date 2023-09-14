---
uid: Stride.CommunityToolkit.Rendering.Compositing.GraphicsCompositorExtensions.AddCleanUIStage(Stride.Rendering.Compositing.GraphicsCompositor)
example: [*content]
---

```csharp
public static GraphicsCompositor AddGraphicsCompositor(this Game game)
{
    // Create a default GraphicsCompositor with enabled post-effects.
    var graphicsCompositor = GraphicsCompositorHelper.CreateDefault(true);

    // Add UI stage and white text effect.
    graphicsCompositor.AddCleanUIStage();

    // Set the GraphicsCompositor for the game's SceneSystem
    game.SceneSystem.GraphicsCompositor = graphicsCompositor;

    return graphicsCompositor;
}
```
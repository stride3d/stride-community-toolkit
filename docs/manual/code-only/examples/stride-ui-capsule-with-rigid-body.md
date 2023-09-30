# Capsule with rigid body and window

In this example, we demonstrate how to set up a 3D scene that includes a capsule with a rigid body as well as a simple window displaying a text message.

This example is organized into multiple methods for better readability and maintainability. It is structured as follows:

- `Start(Scene rootScene)` This is the entry point for setting up the scene. It calls other methods to set up the 3D scene, add the capsule, load the font, and add the window.
- `AddCapsule(Scene rootScene)` This method creates a 3D capsule and adds it to the scene at a specific position.
- `LoadFont()` This method loads the font that will be used for the UI window.
- `AddWindow(Scene rootScene)` This method calls `CreateUIEntity()` to create an entity with a UI component, and then adds this entity to the root scene.
- `CreateUIEntity()` This method creates an entity that has a UI component. The UI component includes a canvas as its root element.
- `CreateCanvas()` This method creates a canvas element that will be the root of the UI component.
- `CreateTextBlock(SpriteFont? _font)` This method creates a `TextBlock` element that displays the message "Hello, World". It uses the loaded font and sets other properties like color and size.

This modular approach makes the code easier to understand and maintain. Each method has a clear responsibility.

![Stride UI Example](media/stride-game-engine-example03-stride-ui-basic-window.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/Example03_StrideUI_CapsuleAndWindow).

[!code-csharp[](../../../../examples/code-only/Example03_StrideUI_CapsuleAndWindow/Program.cs)]
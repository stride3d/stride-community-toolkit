# Myra UI - Draggable Window, GetService()

This example showcases how to integrate [Myra](https://github.com/rds1983/Myra), an external UI library, into your game developed with Stride. Myra provides a rich set of widgets and functionalities to enhance the graphical user interface of your game.

Key features in this example:

- **Draggable Window**: The example demonstrates how to implement a draggable window within the game using Myra's UI components. This draggable window serves as a movable and interactive element that can host other widgets, thus offering a dynamic interface experience for the player.

- **Health Bar**: This example features two distinct health bars. The first one is statically defined within the `MainView` class, while the second is dynamically added during runtime. Both bars can be customized to represent a variety of in-game attributes, such as player health, experience, or other performance metrics.

- **Dynamic Initialization**: The UI components, including the health bar, are initialized dynamically during the game's runtime. This allows for greater flexibility and responsiveness in the game's UI.

- **Service Retrieval**: The example illustrates the use of `GetService()` to retrieve services dynamically, thus fostering loose coupling between various components of the game. This practice promotes code reusability and easier maintenance.

By following this example, you will gain insights into how to extend your game's capabilities by leveraging external libraries for UI and best practices for service retrieval and dynamic UI component initialization.

*ToDo: Add a screenshot*

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/Example04_MyraUI).

[!code-csharp[](../../../../examples/code-only/Example04_MyraUI/Program.cs)]

## MyraSceneRenderer.cs

This class provides functionality for rendering Myra-based user interfaces in a Stride game.

[!code-csharp[](../../../../examples/code-only/Example04_MyraUI/MyraSceneRenderer.cs)]

## MainView.cs

This class creates the main UI window `MainView` and the health bar using `HorizontalProgressBar`.

[!code-csharp[](../../../../examples/code-only/Example04_MyraUI/MainView.cs)]

## UIUtils.cs

This class contains helper methods to create UI elements, which are used in multiple places in the example.

[!code-csharp[](../../../../examples/code-only/Example04_MyraUI/UIUtils.cs)]

## Other Examples

[Using Myra in Stride Engine Tutorial](https://github.com/rds1983/Myra/wiki/Using-Myra-in-Stride-Engine-Tutorial)
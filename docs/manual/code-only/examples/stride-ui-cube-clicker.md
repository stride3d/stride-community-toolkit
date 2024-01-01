# Cube clicker

Cube Clicker is an instructive example using the Stride game engine, showcasing several key features:

- **Game Data Management**: Utilizes the `NexVYaml` serializer for saving and loading game data, demonstrating effective data persistence techniques.
- **Stride UI Demonstration**: Illustrates the use of Stride's UI elements, specifically Grid, TextBlock, and Button, to create interactive and user-friendly interfaces.
- **Scripting in Stride**: Employs both `SyncScript` and `AsyncScript`, providing examples of how to implement synchronous and asynchronous logic in a Stride game.
- **Separation of Concerns**: The game's architecture demonstrates good practice in separating different areas of logic, making the code more maintainable and scalable.

When the game starts, it automatically loads the click data and cube positions from the previous session. The player interacts with dynamically generated cubes, with the game tracking left and right mouse clicks.

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/Example07_CubeClicker).

To explore the entire project, follow the link above. Below is the `Program.cs` file from the project for a quick overview.

[!code-csharp[](../../../../examples/code-only/Example07_CubeClicker/Program.cs)]
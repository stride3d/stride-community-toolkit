# Stride + SignalR

This example demonstrates real-time, two-way communication between a Stride application and a Blazor web application using a SignalR hub. It enables sending and receiving messages at runtime to synchronize game state and trigger in-game actions from the web UI.

This project shows how to:
- Connect to a SignalR hub and register message handlers
- Send commands and events from Stride to the Blazor app, and react to incoming messages
- Drive in-game behaviors (such as creating entities) from the Blazor web UI

To run this example, first start the Blazor server app to host the SignalR hub (using IIS Express). Then, run the Stride application, which will connect to the hub and begin exchanging messages with the web UI.

For the full solution, including both the Stride project and the minimal Blazor server app with a SignalR hub, see the GitHub repository links below.

[!INCLUDE [note-additional-packages](../../../includes/manual/examples/note-additional-packages.md)]

![Stride + SignalR Example](media/stride-game-engine-example17-signalr.webp)

View the Stride example on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/Example17_SignalR), and the minimal Blazor server app with SignalR hub [here](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/Example17_SignalR_Blazor).

[!code-csharp[](../../../../examples/code-only/Example17_SignalR/Program.cs)]
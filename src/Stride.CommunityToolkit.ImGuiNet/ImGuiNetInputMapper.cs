using ImGuiNET;
using Stride.Core.Mathematics;
using Stride.Input;

namespace Stride.CommunityToolkit.ImGuiNet;

/// <summary>
/// Feeds one frame of Stride input into ImGui.NET: mouse position and buttons, text and wheel events. Keys
/// and modifiers are not forwarded; the overlay this system draws takes no keyboard input, and the legacy
/// modifier flags ImGui 1.87 and later recompute at every frame would be overwritten anyway. Stateless.
/// </summary>
internal static class ImGuiNetInputMapper
{
    internal static void Update(InputManager inputManager)
    {
        var io = ImGui.GetIO();

        if (inputManager.HasMouse && !inputManager.IsMousePositionLocked)
        {
            var mousePos = inputManager.AbsoluteMousePosition;

            io.MousePos = new Vector2(mousePos.X, mousePos.Y);
            io.MouseDown[0] = inputManager.IsMouseButtonDown(MouseButton.Left);
            io.MouseDown[1] = inputManager.IsMouseButtonDown(MouseButton.Right);
            io.MouseDown[2] = inputManager.IsMouseButtonDown(MouseButton.Middle);
        }

        // By index: the list is an interface here, and a foreach over it boxes an enumerator every frame
        var events = inputManager.Events;

        for (var i = 0; i < events.Count; i++)
        {
            var inputEvent = events[i];

            switch (inputEvent)
            {
                case TextInputEvent textEvent when textEvent.Text != "\t":
                    io.AddInputCharactersUTF8(textEvent.Text);
                    break;
                case MouseWheelEvent wheelEvent:
                    io.MouseWheel += wheelEvent.WheelDelta;
                    break;
            }
        }
    }
}
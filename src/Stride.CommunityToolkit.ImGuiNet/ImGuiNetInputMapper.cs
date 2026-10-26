using ImGuiNET;
using Stride.Core.Mathematics;
using Stride.Input;

namespace Stride.CommunityToolkit.ImGuiNet;

/// <summary>
/// Feeds one frame of Stride input into ImGui.NET: mouse position and buttons, text and wheel
/// events, and the modifier keys. Split out of <see cref="ImGuiNetSystem"/>; stateless.
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

        foreach (var inputEvent in inputManager.Events)
        {
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

        io.KeyAlt = inputManager.IsKeyDown(Keys.LeftAlt) || inputManager.IsKeyDown(Keys.RightAlt);
        io.KeyShift = inputManager.IsKeyDown(Keys.LeftShift) || inputManager.IsKeyDown(Keys.RightShift);
        io.KeyCtrl = inputManager.IsKeyDown(Keys.LeftCtrl) || inputManager.IsKeyDown(Keys.RightCtrl);
        io.KeySuper = inputManager.IsKeyDown(Keys.LeftWin) || inputManager.IsKeyDown(Keys.RightWin);
    }
}
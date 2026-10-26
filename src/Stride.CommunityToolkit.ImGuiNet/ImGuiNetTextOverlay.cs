using ImGuiNET;
using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;

namespace Stride.CommunityToolkit.ImGuiNet;

/// <summary>
/// The Box2D.NET-style text overlay of <see cref="ImGuiNetSystem"/>: buffers the frame's DrawString
/// requests and draws them into one fullscreen, input-transparent ImGui window. Split out of
/// <see cref="ImGuiNetSystem"/>, which owns the frame and forwards its DrawString API here.
/// </summary>
internal sealed class ImGuiNetTextOverlay
{
    private static readonly Vector4 DefaultColor = new(0.9f, 0.9f, 0.9f, 1.0f);

    private readonly List<DrawCommand> _drawCommands = [];

    internal void AddScreenText(int x, int y, string message, Vector4? color)
        => _drawCommands.Add(new DrawCommand(DrawCommandType.ScreenText, new Vector2(x, y), default, message, color ?? DefaultColor));

    internal void AddWorldText(Vector3 worldPosition, string message, Vector4? color)
        => _drawCommands.Add(new DrawCommand(DrawCommandType.WorldText, default, worldPosition, message, color ?? DefaultColor));

    /// <summary>Draws the buffered strings into one overlay window and clears the buffer for the next frame.</summary>
    internal void Draw(bool showUI, CameraComponent? camera, GraphicsDevice graphicsDevice)
    {
        if (!showUI || _drawCommands.Count == 0) return;

        // One fullscreen, transparent overlay window holds every item
        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize);
        ImGui.SetNextWindowBgAlpha(0.0f);

        ImGui.Begin("Overlay", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings);

        foreach (var command in _drawCommands)
        {
            var screenPos = command.Type == DrawCommandType.ScreenText ? command.ScreenPosition : WorldToScreen(camera, graphicsDevice, command.WorldPosition);

            ImGui.SetCursorPos(screenPos);
            ImGui.TextColored(command.Color, command.Message);
        }

        ImGui.End();

        _drawCommands.Clear();
    }

    private static Vector2 WorldToScreen(CameraComponent? camera, GraphicsDevice graphicsDevice, Vector3 worldPosition)
        => camera is null ? Vector2.Zero : camera.WorldToScreenPoint(ref worldPosition, graphicsDevice);

    private enum DrawCommandType
    {
        ScreenText,
        WorldText
    }

    private readonly record struct DrawCommand(DrawCommandType Type, Vector2 ScreenPosition, Vector3 WorldPosition, string Message, Vector4 Color);
}
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.ImGuiNet;

/// <summary>
/// Extension methods for integrating ImGui.NET with Stride Game Engine.
/// </summary>
public static class ImGuiNetExtensions
{
    /// <summary>
    /// Adds ImGui.NET system to the game for Box2D.NET-style text rendering.
    /// </summary>
    /// <param name="game">The game instance.</param>
    /// <returns>The ImGui.NET system instance.</returns>
    public static ImGuiNetSystem AddImGuiNet(this Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        var imguiSystem = new ImGuiNetSystem(game.Services);
        return imguiSystem;
    }

    /// <summary>
    /// Draws text at screen coordinates using ImGui.NET.
    /// </summary>
    /// <param name="system">The ImGui system.</param>
    /// <param name="x">X coordinate in screen space.</param>
    /// <param name="y">Y coordinate in screen space.</param>
    /// <param name="text">The text to display.</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="a">Alpha component (0-255, default 255).</param>
    /// <returns>The ImGui system for fluent chaining.</returns>
    public static ImGuiNetSystem DrawText(this ImGuiNetSystem system, int x, int y, string text,
        byte r, byte g, byte b, byte a = 255)
    {
        ArgumentNullException.ThrowIfNull(system);
        ArgumentNullException.ThrowIfNull(text);

        var color = new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
        system.DrawString(x, y, text, color);
        return system;
    }

    /// <summary>
    /// Draws text at world coordinates using ImGui.NET.
    /// </summary>
    /// <param name="system">The ImGui system.</param>
    /// <param name="worldPosition">Position in world space.</param>
    /// <param name="text">The text to display.</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="a">Alpha component (0-255, default 255).</param>
    /// <returns>The ImGui system for fluent chaining.</returns>
    public static ImGuiNetSystem DrawText(this ImGuiNetSystem system, Vector3 worldPosition, string text,
        byte r, byte g, byte b, byte a = 255)
    {
        ArgumentNullException.ThrowIfNull(system);
        ArgumentNullException.ThrowIfNull(text);

        var color = new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
        system.DrawString(worldPosition, text, color);
        return system;
    }

    /// <summary>
    /// Draws white text at screen coordinates.
    /// </summary>
    /// <param name="system">The ImGui system.</param>
    /// <param name="x">X coordinate in screen space.</param>
    /// <param name="y">Y coordinate in screen space.</param>
    /// <param name="text">The text to display.</param>
    /// <returns>The ImGui system for fluent chaining.</returns>
    public static ImGuiNetSystem DrawText(this ImGuiNetSystem system, int x, int y, string text)
    {
        ArgumentNullException.ThrowIfNull(system);
        ArgumentNullException.ThrowIfNull(text);

        system.DrawString(x, y, text);
        return system;
    }

    /// <summary>
    /// Draws white text at world coordinates.
    /// </summary>
    /// <param name="system">The ImGui system.</param>
    /// <param name="worldPosition">Position in world space.</param>
    /// <param name="text">The text to display.</param>
    /// <returns>The ImGui system for fluent chaining.</returns>
    public static ImGuiNetSystem DrawText(this ImGuiNetSystem system, Vector3 worldPosition, string text)
    {
        ArgumentNullException.ThrowIfNull(system);
        ArgumentNullException.ThrowIfNull(text);

        system.DrawString(worldPosition, text);
        return system;
    }

    /// <summary>
    /// Gets the ImGuiNetSystem from a Game instance for convenient access.
    /// </summary>
    /// <param name="game">The game instance.</param>
    /// <returns>The ImGuiNetSystem, or null if not found.</returns>
    public static ImGuiNetSystem? GetImGuiNetSystem(this Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        return game.Services.GetService<ImGuiNetSystem>();
    }
}
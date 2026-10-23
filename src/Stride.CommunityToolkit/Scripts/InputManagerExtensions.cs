using Stride.Input;

namespace Stride.CommunityToolkit.Scripts;

/// <summary>
/// The input questions the camera controllers all ask.
/// </summary>
internal static class InputManagerExtensions
{
    /// <summary>
    /// Whether either shift key is held: the sprint and fast-zoom modifier of every controller.
    /// </summary>
    internal static bool IsShiftDown(this InputManager input)
        => input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift);
}
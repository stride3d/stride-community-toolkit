using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace Example18_Box2DPhysics.Helpers;

/// <summary>
/// Helper class for managing input and coordinate transformations
/// </summary>
public class InputManager
{
    private readonly Game _game;
    private readonly CameraComponent _camera;

    public InputManager(Game game, CameraComponent camera)
    {
        _game = game;
        _camera = camera;
    }

    /// <summary>
    /// Converts mouse screen coordinates to world coordinates
    /// </summary>
    /// <param name="mousePosition">Mouse position in screen coordinates</param>
    /// <returns>World position, or null if conversion fails</returns>
    public Vector2? GetWorldPointFromMouse(Vector2 mousePosition)
    {
        var ray = _camera.CalculateRayPlaneIntersectionPoint(mousePosition);
        return ray;
    }

    /// <summary>
    /// Checks if a key was just pressed (not held)
    /// </summary>
    /// <param name="key">The key to check</param>
    /// <returns>True if key was just pressed</returns>
    public bool IsKeyJustPressed(Keys key)
    {
        return _game.Input.IsKeyPressed(key);
    }

    /// <summary>
    /// Gets the current mouse position in screen coordinates
    /// </summary>
    /// <returns>Mouse position</returns>
    public Vector2 GetMousePosition()
    {
        return _game.Input.MousePosition;
    }
}
namespace Stride.CommunityToolkit.Scripts;

/// <summary>
/// The geometry behind <see cref="Scripts.Basic2DCameraController"/>, as pure functions.
/// </summary>
/// <remarks>
/// These two calculations are the only parts of the controller that can be reasoned about, and
/// checked, without a running game: they read no camera, no input and no clock. Keeping them here
/// says which parts of the controller are maths and which are plumbing.
/// </remarks>
internal static class Camera2DMath
{
    /// <summary>
    /// Works out which way an RTS-style camera should pan for a cursor near the window edges.
    /// </summary>
    /// <param name="mousePosition">The cursor position, normalised to 0..1 across the window.</param>
    /// <param name="screenWidth">Back-buffer width in pixels.</param>
    /// <param name="screenHeight">Back-buffer height in pixels.</param>
    /// <param name="borderWidth">How many pixels in from an edge still count as being at it.</param>
    /// <returns>A direction whose components are -1, 0 or 1, and zero when no edge is near.</returns>
    /// <remarks>
    /// A cursor outside the window pans nothing. The far edges are treated as one pixel short of the
    /// window, because the cursor keeps reporting the boundary value after it has actually left.
    /// </remarks>
    internal static Vector3 ScreenEdgeDirection(Vector2 mousePosition, int screenWidth, int screenHeight, float borderWidth)
    {
        var direction = Vector3.Zero;

        var x = mousePosition.X * screenWidth;
        var y = mousePosition.Y * screenHeight;

        if (x <= 0 || x >= screenWidth - 1 || y <= 0 || y >= screenHeight - 1) return direction;

        if (x < borderWidth) direction.X--;
        if (x > screenWidth - borderWidth) direction.X++;

        // Screen Y grows downwards while world Y grows upwards, so the top edge pans the camera up.
        if (y < borderWidth) direction.Y++;
        if (y > screenHeight - borderWidth) direction.Y--;

        return direction;
    }

    /// <summary>
    /// The camera shift that keeps the world point under the cursor in place as the view rescales.
    /// </summary>
    /// <param name="mousePosition">The cursor position, normalised to 0..1 across the window.</param>
    /// <param name="aspect">The back buffer's width divided by its height.</param>
    /// <param name="oldSize">The orthographic size before the zoom.</param>
    /// <param name="newSize">The orthographic size after it.</param>
    /// <returns>The world-space movement to apply to the camera.</returns>
    /// <remarks>
    /// The cursor's offset from the view centre in world units scales with the view, by
    /// <c>newSize / oldSize</c>. Moving the camera by the difference leaves the world point exactly
    /// where it was on screen.
    /// </remarks>
    internal static Vector3 ZoomToCursorShift(Vector2 mousePosition, float aspect, float oldSize, float newSize)
    {
        var offset = new Vector3((mousePosition.X - 0.5f) * oldSize * aspect, (0.5f - mousePosition.Y) * oldSize, 0f);

        return offset * (1f - newSize / oldSize);
    }
}
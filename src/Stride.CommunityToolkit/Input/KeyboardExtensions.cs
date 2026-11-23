using Stride.Input;

namespace Stride.CommunityToolkit.Input;

/// <summary>
/// Keyboard reads on <see cref="InputManager"/>: the modifier keys as one question each, and a plain key told from a chord.
/// </summary>
public static class KeyboardExtensions
{
    /// <summary>
    /// Whether either Shift key is held: the sprint and fast-zoom modifier of every camera controller.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <returns><see langword="true"/> while the left or the right Shift is down.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static bool IsShiftDown(this InputManager input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift);
    }

    /// <summary>
    /// Whether either Ctrl key is held: the modifier of the profiler's chords.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <returns><see langword="true"/> while the left or the right Ctrl is down.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static bool IsCtrlDown(this InputManager input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.IsKeyDown(Keys.LeftCtrl) || input.IsKeyDown(Keys.RightCtrl);
    }

    /// <summary>
    /// Whether either Alt key is held.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <returns><see langword="true"/> while the left or the right Alt is down.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static bool IsAltDown(this InputManager input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.IsKeyDown(Keys.LeftAlt) || input.IsKeyDown(Keys.RightAlt);
    }

    /// <summary>
    /// Whether <paramref name="key"/> was pressed this frame on its own, with neither Ctrl nor Alt held.
    /// </summary>
    /// <remarks>
    /// The toolkit binds its plain keys (the camera help's F2, the overlay's F3 and F4) and its Ctrl chords (the
    /// profiler's Ctrl+F1 to Ctrl+F4) on the same keys. Read through <see cref="InputManager.IsKeyPressed"/>, the
    /// plain binding fires along with the chord; read through this, it does not. Shift is not a modifier here, so a
    /// key typed in upper case still counts.
    /// </remarks>
    /// <param name="input">The input manager.</param>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> on the frame the key went down with no Ctrl or Alt held.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static bool IsKeyPressedUnmodified(this InputManager input, Keys key)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (!input.IsKeyPressed(key)) return false;

        return !input.IsCtrlDown() && !input.IsAltDown();
    }
}
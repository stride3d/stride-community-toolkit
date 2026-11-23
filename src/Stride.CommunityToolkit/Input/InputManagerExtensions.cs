using Stride.Input;

namespace Stride.CommunityToolkit.Input;

/// <summary>
/// Gamepad reads on <see cref="InputManager"/>: the thumbsticks, the triggers and the buttons of the default pad,
/// of the pad at an index, or of every connected pad at once, each with a dead zone so a stick that has sprung
/// back reads as zero.
/// </summary>
/// <remarks>
/// <para>
/// Stride hands a pad's state back raw, and a stick at rest reports a few hundredths off centre, which a game that
/// integrates it turns into drift. The dead zone is radial for a stick and linear for a trigger, and what is left
/// past it is stretched back to the full range, so motion starts from zero instead of jumping.
/// </para>
/// <para>
/// The default pad is <see cref="InputManager.DefaultGamePad"/>, the first one connected. The <c>Any</c> reads
/// average the pads that are past the dead zone, so two pads on one game do not double a reading and a pad left on
/// the desk does not pull the others down; a button is down on any pad when at least one holds it.
/// </para>
/// </remarks>
public static class InputManagerExtensions
{
    /// <summary>
    /// The dead zone used when none is given: a stick or a trigger under a fifth of its travel reads as zero.
    /// </summary>
    public const float DefaultDeadZone = 0.2f;

    /// <summary>
    /// Whether <paramref name="button"/> is held down on the default pad.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <param name="button">The button, or several combined; all of them must be down.</param>
    /// <returns><see langword="true"/> while the button is down; <see langword="false"/> with no pad connected.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static bool IsGamePadButtonDown(this InputManager input, GamePadButton button)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.DefaultGamePad is { } pad && (pad.State.Buttons & button) == button;
    }

    /// <summary>
    /// Whether <paramref name="button"/> is held down on the pad at <paramref name="index"/>.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <param name="button">The button, or several combined; all of them must be down.</param>
    /// <param name="index">The pad's index, as <see cref="IGamePadDevice.Index"/> assigns it.</param>
    /// <returns><see langword="true"/> while the button is down; <see langword="false"/> when no pad has that index.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static bool IsGamePadButtonDown(this InputManager input, GamePadButton button, int index)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.GetGamePadByIndex(index) is { } pad && (pad.State.Buttons & button) == button;
    }

    /// <summary>
    /// Whether <paramref name="button"/> is held down on any connected pad.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <param name="button">The button, or several combined; all of them must be down on the same pad.</param>
    /// <returns><see langword="true"/> while at least one pad holds the button.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static bool IsGamePadButtonDownAny(this InputManager input, GamePadButton button)
    {
        ArgumentNullException.ThrowIfNull(input);

        var pads = input.GamePads;

        for (var i = 0; i < pads.Count; i++)
        {
            if ((pads[i].State.Buttons & button) == button) return true;
        }

        return false;
    }

    /// <summary>
    /// Whether <paramref name="button"/> went down this frame on the default pad.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <param name="button">The button.</param>
    /// <returns><see langword="true"/> on the frame the button is pressed; <see langword="false"/> with no pad connected.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static bool IsGamePadButtonPressed(this InputManager input, GamePadButton button)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.DefaultGamePad is { } pad && pad.IsButtonPressed(button);
    }

    /// <summary>
    /// Whether <paramref name="button"/> went down this frame on the pad at <paramref name="index"/>.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <param name="button">The button.</param>
    /// <param name="index">The pad's index, as <see cref="IGamePadDevice.Index"/> assigns it.</param>
    /// <returns><see langword="true"/> on the frame the button is pressed; <see langword="false"/> when no pad has that index.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static bool IsGamePadButtonPressed(this InputManager input, GamePadButton button, int index)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.GetGamePadByIndex(index) is { } pad && pad.IsButtonPressed(button);
    }

    /// <summary>
    /// Whether <paramref name="button"/> went down this frame on any connected pad.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <param name="button">The button.</param>
    /// <returns><see langword="true"/> on the frame at least one pad presses the button.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static bool IsGamePadButtonPressedAny(this InputManager input, GamePadButton button)
    {
        ArgumentNullException.ThrowIfNull(input);

        var pads = input.GamePads;

        for (var i = 0; i < pads.Count; i++)
        {
            if (pads[i].IsButtonPressed(button)) return true;
        }

        return false;
    }

    /// <summary>
    /// The default pad's left stick, past the dead zone and stretched back to the unit range.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <param name="deadZone">The radius under which the stick reads as zero.</param>
    /// <returns>X to the right and Y up, each -1 to 1; <see cref="Vector2.Zero"/> with no pad connected.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static Vector2 GetLeftThumb(this InputManager input, float deadZone = DefaultDeadZone)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.DefaultGamePad is { } pad ? PastDeadZone(pad.State.LeftThumb, deadZone) : Vector2.Zero;
    }

    /// <summary>
    /// The left stick of the pad at <paramref name="index"/>, past the dead zone and stretched back to the unit range.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <param name="index">The pad's index, as <see cref="IGamePadDevice.Index"/> assigns it.</param>
    /// <param name="deadZone">The radius under which the stick reads as zero.</param>
    /// <returns>X to the right and Y up, each -1 to 1; <see cref="Vector2.Zero"/> when no pad has that index.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static Vector2 GetLeftThumb(this InputManager input, int index, float deadZone = DefaultDeadZone)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.GetGamePadByIndex(index) is { } pad ? PastDeadZone(pad.State.LeftThumb, deadZone) : Vector2.Zero;
    }

    /// <summary>
    /// The left sticks of every connected pad past the dead zone, averaged.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <param name="deadZone">The radius under which a stick reads as zero and is left out of the average.</param>
    /// <returns>X to the right and Y up, each -1 to 1; <see cref="Vector2.Zero"/> when no stick is past the dead zone.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static Vector2 GetLeftThumbAny(this InputManager input, float deadZone = DefaultDeadZone)
    {
        ArgumentNullException.ThrowIfNull(input);

        return AverageSticks(input.GamePads, static state => state.LeftThumb, deadZone);
    }

    /// <summary>
    /// The default pad's right stick, past the dead zone and stretched back to the unit range.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <param name="deadZone">The radius under which the stick reads as zero.</param>
    /// <returns>X to the right and Y up, each -1 to 1; <see cref="Vector2.Zero"/> with no pad connected.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static Vector2 GetRightThumb(this InputManager input, float deadZone = DefaultDeadZone)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.DefaultGamePad is { } pad ? PastDeadZone(pad.State.RightThumb, deadZone) : Vector2.Zero;
    }

    /// <summary>
    /// The right stick of the pad at <paramref name="index"/>, past the dead zone and stretched back to the unit range.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <param name="index">The pad's index, as <see cref="IGamePadDevice.Index"/> assigns it.</param>
    /// <param name="deadZone">The radius under which the stick reads as zero.</param>
    /// <returns>X to the right and Y up, each -1 to 1; <see cref="Vector2.Zero"/> when no pad has that index.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static Vector2 GetRightThumb(this InputManager input, int index, float deadZone = DefaultDeadZone)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.GetGamePadByIndex(index) is { } pad ? PastDeadZone(pad.State.RightThumb, deadZone) : Vector2.Zero;
    }

    /// <summary>
    /// The right sticks of every connected pad past the dead zone, averaged.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <param name="deadZone">The radius under which a stick reads as zero and is left out of the average.</param>
    /// <returns>X to the right and Y up, each -1 to 1; <see cref="Vector2.Zero"/> when no stick is past the dead zone.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static Vector2 GetRightThumbAny(this InputManager input, float deadZone = DefaultDeadZone)
    {
        ArgumentNullException.ThrowIfNull(input);

        return AverageSticks(input.GamePads, static state => state.RightThumb, deadZone);
    }

    /// <summary>
    /// The default pad's left trigger, past the dead zone and stretched back to the unit range.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <param name="deadZone">The travel under which the trigger reads as zero.</param>
    /// <returns>0 released to 1 fully pulled; 0 with no pad connected.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static float GetLeftTrigger(this InputManager input, float deadZone = DefaultDeadZone)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.DefaultGamePad is { } pad ? PastDeadZone(pad.State.LeftTrigger, deadZone) : 0f;
    }

    /// <summary>
    /// The left trigger of the pad at <paramref name="index"/>, past the dead zone and stretched back to the unit range.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <param name="index">The pad's index, as <see cref="IGamePadDevice.Index"/> assigns it.</param>
    /// <param name="deadZone">The travel under which the trigger reads as zero.</param>
    /// <returns>0 released to 1 fully pulled; 0 when no pad has that index.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static float GetLeftTrigger(this InputManager input, int index, float deadZone = DefaultDeadZone)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.GetGamePadByIndex(index) is { } pad ? PastDeadZone(pad.State.LeftTrigger, deadZone) : 0f;
    }

    /// <summary>
    /// The left triggers of every connected pad past the dead zone, averaged.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <param name="deadZone">The travel under which a trigger reads as zero and is left out of the average.</param>
    /// <returns>0 to 1; 0 when no trigger is past the dead zone.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static float GetLeftTriggerAny(this InputManager input, float deadZone = DefaultDeadZone)
    {
        ArgumentNullException.ThrowIfNull(input);

        return AverageTriggers(input.GamePads, static state => state.LeftTrigger, deadZone);
    }

    /// <summary>
    /// The default pad's right trigger, past the dead zone and stretched back to the unit range.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <param name="deadZone">The travel under which the trigger reads as zero.</param>
    /// <returns>0 released to 1 fully pulled; 0 with no pad connected.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static float GetRightTrigger(this InputManager input, float deadZone = DefaultDeadZone)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.DefaultGamePad is { } pad ? PastDeadZone(pad.State.RightTrigger, deadZone) : 0f;
    }

    /// <summary>
    /// The right trigger of the pad at <paramref name="index"/>, past the dead zone and stretched back to the unit range.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <param name="index">The pad's index, as <see cref="IGamePadDevice.Index"/> assigns it.</param>
    /// <param name="deadZone">The travel under which the trigger reads as zero.</param>
    /// <returns>0 released to 1 fully pulled; 0 when no pad has that index.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static float GetRightTrigger(this InputManager input, int index, float deadZone = DefaultDeadZone)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.GetGamePadByIndex(index) is { } pad ? PastDeadZone(pad.State.RightTrigger, deadZone) : 0f;
    }

    /// <summary>
    /// The right triggers of every connected pad past the dead zone, averaged.
    /// </summary>
    /// <param name="input">The input manager.</param>
    /// <param name="deadZone">The travel under which a trigger reads as zero and is left out of the average.</param>
    /// <returns>0 to 1; 0 when no trigger is past the dead zone.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static float GetRightTriggerAny(this InputManager input, float deadZone = DefaultDeadZone)
    {
        ArgumentNullException.ThrowIfNull(input);

        return AverageTriggers(input.GamePads, static state => state.RightTrigger, deadZone);
    }

    /// <summary>The sticks <paramref name="read"/> picks, past the dead zone, averaged over the pads that are.</summary>
    private static Vector2 AverageSticks(IReadOnlyList<IGamePadDevice> pads, Func<GamePadState, Vector2> read, float deadZone)
    {
        var total = Vector2.Zero;
        var count = 0;

        for (var i = 0; i < pads.Count; i++)
        {
            var stick = PastDeadZone(read(pads[i].State), deadZone);

            if (stick == Vector2.Zero) continue;

            total += stick;
            count++;
        }

        return count > 1 ? total / count : total;
    }

    /// <summary>The triggers <paramref name="read"/> picks, past the dead zone, averaged over the pads that are.</summary>
    private static float AverageTriggers(IReadOnlyList<IGamePadDevice> pads, Func<GamePadState, float> read, float deadZone)
    {
        var total = 0f;
        var count = 0;

        for (var i = 0; i < pads.Count; i++)
        {
            var trigger = PastDeadZone(read(pads[i].State), deadZone);

            if (trigger == 0f) continue;

            total += trigger;
            count++;
        }

        return count > 1 ? total / count : total;
    }

    /// <summary>
    /// A stick past a radial dead zone, stretched so the first readable deflection is zero and full deflection stays 1.
    /// </summary>
    private static Vector2 PastDeadZone(Vector2 stick, float deadZone)
    {
        if (deadZone <= 0f) return stick;

        var length = stick.Length();

        if (length <= deadZone) return Vector2.Zero;

        var scaled = (length - deadZone) / (1f - deadZone);

        return stick * (MathF.Min(scaled, 1f) / length);
    }

    /// <summary>
    /// A trigger past a linear dead zone, stretched so the first readable pull is zero and a full pull stays 1.
    /// </summary>
    private static float PastDeadZone(float trigger, float deadZone)
    {
        if (deadZone <= 0f) return trigger;

        if (trigger <= deadZone) return 0f;

        return MathF.Min((trigger - deadZone) / (1f - deadZone), 1f);
    }
}
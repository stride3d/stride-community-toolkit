using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;

namespace E04_StrideUI_ButtonHoverAnimation;

/// <summary>
/// Grows an underline out from the centre of a button while the mouse is over it, and shrinks it back
/// when the mouse leaves.
/// </summary>
/// <remarks>
/// <para>
/// Stride hit-tests <see cref="Stride.UI.UIElement.MouseOverState"/> whenever the mouse moves.
/// <see cref="Track"/> also sets <see cref="Stride.UI.UIElement.RequiresMouseOverUpdate"/>, which
/// re-tests it while the cursor is still; the engine clears that flag when the pointer leaves.
/// </para>
/// <para>
/// One script drives every button rather than one script per button. A <see cref="SyncScript"/> is an
/// entity component, so the per-button alternative would mean an entity each, and the UI tree already
/// gives the buttons their identity.
/// </para>
/// </remarks>
public class ButtonHoverAnimation : SyncScript
{
    /// <summary>How quickly the underline catches up with its target width. Higher is snappier.</summary>
    public float Speed { get; set; } = 8f;

    /// <summary>The underline's full width, as a fraction of the button's rendered width.</summary>
    public float WidthFactor { get; set; } = 0.8f;

    private readonly List<(Button Button, Border Underline)> _tracked = [];

    /// <summary>
    /// Registers a button together with the underline that belongs to it.
    /// </summary>
    /// <param name="button">The button to watch the pointer over.</param>
    /// <param name="underline">The border to animate, which should start at zero width.</param>
    public void Track(Button button, Border underline)
    {
        ArgumentNullException.ThrowIfNull(button);
        ArgumentNullException.ThrowIfNull(underline);

        // Re-tests the hover state even while the cursor is still
        button.RequiresMouseOverUpdate = true;

        _tracked.Add((button, underline));
    }

    /// <inheritdoc/>
    public override void Update()
    {
        var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;

        // Clamped because Speed * deltaTime is only a valid interpolation factor up to 1: a long frame
        // would otherwise overshoot the target and the underline would visibly spring past its width.
        var amount = MathUtil.Clamp(Speed * deltaTime, 0f, 1f);

        foreach (var (button, underline) in _tracked)
        {
            // Not == MouseOverElement: a hittable child under the pointer leaves the button at MouseOverChild
            var hovering = button.MouseOverState != MouseOverState.MouseOverNone;

            // RenderSize is the size the last layout pass gave the button, so the underline tracks a
            // button that changes width - with the window, or with a longer label.
            var target = hovering ? button.RenderSize.X * WidthFactor : 0f;

            // Lerping the live width toward a target, rather than stepping a stored progress value,
            // means an interrupted animation reverses from wherever it got to instead of snapping.
            underline.Width = MathUtil.Lerp(underline.Width, target, amount);
        }
    }
}
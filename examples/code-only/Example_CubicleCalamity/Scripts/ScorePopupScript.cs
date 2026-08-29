using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example_CubicleCalamity.Scripts;

/// <summary>
/// Animates one floating score above the cube that was cleared, then removes it.
/// </summary>
/// <remarks>
/// <para>
/// The shape of this is the whole point: a fast burst that settles, not a slow drift. It rises with
/// an ease-<em>out</em>, so most of the travel happens immediately and the number is where the eye
/// expects it before it has finished moving; it overshoots its size and springs back, which is what
/// reads as impact; and it fades over the last stretch so it leaves without needing to be noticed
/// leaving.
/// </para>
/// <para>
/// The previous version did the opposite of all three: an ease-<em>in</em>, so it crept away slowly
/// and then accelerated, over four seconds and twenty-five world units, with no fade. It also used
/// its travel distance as an absolute height at which to delete itself, so popups spawned high up
/// vanished part-way through their animation while ones near the ground played almost to the end.
/// </para>
/// </remarks>
public class ScorePopupScript : SyncScript
{
    private const float Duration = 1.2f;
    private const float RiseDistance = 2.2f;
    private const float FadeStart = 0.6f;
    private const float StartScale = 0.4f;
    private const float PopDuration = 0.25f;

    private Vector3 _startPosition;
    private EntityTextComponent? _text;
    private float _elapsed;

    /// <summary>
    /// Gets or sets a sideways drift, so several popups at once do not stack into one unreadable pile.
    /// </summary>
    public float HorizontalDrift { get; set; }

    /// <inheritdoc />
    public override void Start()
    {
        _startPosition = Entity.Transform.Position;
        _text = Entity.Get<EntityTextComponent>();
    }

    /// <inheritdoc />
    public override void Update()
    {
        _elapsed += (float)Game.UpdateTime.Elapsed.TotalSeconds;

        var progress = Math.Clamp(_elapsed / Duration, 0f, 1f);

        // Ease out: nearly all of the rise is spent in the first third of the time
        var rise = MathUtilEx.Interpolate(0f, RiseDistance, progress, EasingFunction.QuadraticEaseOut);

        Entity.Transform.Position = _startPosition + new Vector3(HorizontalDrift * progress, rise, 0);

        if (_text is not null)
        {
            _text.Scale = GetScale();
            _text.Opacity = GetOpacity(progress);
        }

        // Removed on elapsed time, so every popup plays its whole animation wherever it started
        if (progress >= 1f)
        {
            Entity.Scene = null;
        }
    }

    /// <summary>
    /// Returns the size for this frame: a spring past full size and back, then steady.
    /// </summary>
    /// <remarks>
    /// <see cref="EasingFunction.BackEaseOut"/> travels past its target and settles back onto it, so
    /// interpolating straight to full size gives the overshoot for free. That momentary too-big is
    /// what reads as impact; easing smoothly up to size just looks like it is growing.
    /// </remarks>
    private float GetScale()
        => _elapsed >= PopDuration
            ? 1f
            : MathUtilEx.Interpolate(StartScale, 1f, _elapsed / PopDuration, EasingFunction.BackEaseOut);

    private static float GetOpacity(float progress)
        => progress < FadeStart ? 1f : 1f - (progress - FadeStart) / (1f - FadeStart);
}
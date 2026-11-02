using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// A fade or a wipe over the whole window: to a colour and back round a scene change, a cut, a bar that
/// sweeps across. <see cref="ScreenFadeExtensions.AddScreenFade"/> puts one in the scene; the transition
/// methods start it, and it draws itself every frame through a shape batch after the post effects, so
/// nothing else in the frame is affected.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Transition"/> and <see cref="Wipe"/> take the action to run while the screen is covered,
/// which is where a scene is switched, a level loaded or a player moved: it happens between the two
/// halves, and the player sees the colour and then the result. <see cref="FadeOut"/> and <see cref="FadeIn"/>
/// are the halves on their own, and <see cref="Cut"/> covers or clears at once.
/// </para>
/// <para>
/// A transition started while another runs replaces it from the cover the screen has at that moment.
/// </para>
/// </remarks>
[Display("Screen Fade")]
[ComponentCategory("Rendering")]
public sealed class ScreenFade : SyncScript
{
    private ShapeBatch? _batch;
    private Phase _phase;
    private bool _wipe;
    private float _duration;
    private float _elapsed;
    private float _from;
    private float _inSeconds;
    private Action? _between;

    private enum Phase { Idle, Out, In }

    /// <summary>Gets or sets the colour the screen fades to. Defaults to black.</summary>
    public Color Color { get; set; } = Color.Black;

    /// <summary>Gets how much of the screen is covered now, 0 clear to 1 covered.</summary>
    public float Covered { get; private set; }

    /// <summary>Gets whether a transition is in progress.</summary>
    public bool IsRunning => _phase != Phase.Idle;

    /// <summary>Fades to the colour over <paramref name="seconds"/> and stays covered.</summary>
    public void FadeOut(float seconds) => Begin(wipe: false, Phase.Out, seconds, between: null, inSeconds: 0f);

    /// <summary>Fades from the colour to the scene over <paramref name="seconds"/>.</summary>
    public void FadeIn(float seconds) => Begin(wipe: false, Phase.In, seconds, between: null, inSeconds: 0f);

    /// <summary>
    /// Fades to the colour over <paramref name="outSeconds"/>, runs <paramref name="between"/> while the screen is
    /// covered, then fades back over <paramref name="inSeconds"/>.
    /// </summary>
    public void Transition(float outSeconds, Action between, float inSeconds) => Begin(wipe: false, Phase.Out, outSeconds, between, inSeconds);

    /// <summary>
    /// A bar of the colour sweeps in from the left over <paramref name="outSeconds"/> until it covers the screen,
    /// <paramref name="between"/> runs, and the bar sweeps on out to the right over <paramref name="inSeconds"/>.
    /// </summary>
    public void Wipe(float outSeconds, Action between, float inSeconds) => Begin(wipe: true, Phase.Out, outSeconds, between, inSeconds);

    /// <summary>Covers or clears the screen at once, ending any transition.</summary>
    public void Cut(bool covered)
    {
        _phase = Phase.Idle;
        _between = null;
        Covered = covered ? 1f : 0f;
    }

    /// <summary>Takes the batch the fade draws with.</summary>
    public override void Start() => _batch = Game.AddShapeBatch(afterPostEffects: true);

    /// <summary>Advances the transition and draws the cover.</summary>
    public override void Update()
    {
        Advance((float)Game.UpdateTime.Elapsed.TotalSeconds);

        if (_batch is { } batch && Covered > 0f) Draw(batch);
    }

    private void Begin(bool wipe, Phase phase, float seconds, Action? between, float inSeconds)
    {
        _wipe = wipe;
        _phase = phase;
        _duration = MathF.Max(seconds, 0f);
        _elapsed = 0f;
        _from = Covered;
        _between = between;
        _inSeconds = inSeconds;
    }

    private void Advance(float elapsed)
    {
        if (_phase == Phase.Idle) return;

        _elapsed += elapsed;

        var progress = _duration <= 0f ? 1f : MathF.Min(_elapsed / _duration, 1f);

        // Out closes the gap from where the cover was to full; In opens it from where it was to none
        Covered = _phase == Phase.Out ? _from + (1f - _from) * progress : _from * (1f - progress);

        if (progress < 1f) return;

        if (_phase == Phase.Out)
        {
            var between = _between;

            _between = null;
            between?.Invoke();

            if (_inSeconds > 0f || between is not null)
            {
                Begin(_wipe, Phase.In, _inSeconds, between: null, inSeconds: 0f);
                return;
            }
        }

        _phase = Phase.Idle;
    }

    private void Draw(ShapeBatch batch)
    {
        var size = batch.ScreenSize;

        batch.Screen = true;
        batch.BorderWidth = 0f;

        if (_wipe)
        {
            // The bar grows in from the left while covering and slides off to the right while clearing
            var width = size.X * Covered;
            var left = _phase == Phase.In ? size.X - width : 0f;

            batch.Fill.Set(Color, 1f);
            batch.DrawRectangle(new Vector3(left + width * 0.5f, size.Y * 0.5f, 0f), Vector3.UnitX, Vector3.UnitY, new Vector2(width, size.Y), Color);
        }
        else
        {
            batch.Fill.Set(Color, Covered);
            batch.DrawRectangle(new Vector3(size * 0.5f, 0f), Vector3.UnitX, Vector3.UnitY, size, Color);
        }

        batch.Fill.Set(null, 1f);
        batch.Screen = false;
    }
}
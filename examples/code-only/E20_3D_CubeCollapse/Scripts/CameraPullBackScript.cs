using Stride.CommunityToolkit.Mathematics;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace CubeCollapse.Scripts;

/// <summary>
/// Eases the camera back from the board when the game ends, so the falling words and the menu above
/// the board are all in view. Removes itself when it arrives.
/// </summary>
/// <remarks>
/// The camera moves along the line from the board's centre through itself, so it keeps looking where
/// it looked. Only the position is written, which the camera controller adds its own movement to, so
/// the two do not fight. An ease-out: most of the way at once, then a slow settle.
/// </remarks>
public class CameraPullBackScript : SyncScript
{
    private Vector3 _from;
    private Vector3 _to;
    private float _elapsed;
    private float _eased;

    /// <summary>The point the camera moves away from: the board's centre.</summary>
    public Vector3 Centre { get; set; }

    /// <summary>How far from <see cref="Centre"/> the camera ends up. A camera already further away stays where it is.</summary>
    public float Distance { get; set; } = 18.2f;

    /// <summary>How long the move takes, in seconds.</summary>
    public float Duration { get; set; } = 1.5f;

    /// <inheritdoc />
    public override void Start()
    {
        _from = Entity.Transform.Position;

        var offset = _from - Centre;
        var length = offset.Length();

        _to = length < Distance && length > MathUtil.ZeroTolerance ? Centre + offset / length * Distance : _from;
    }

    /// <inheritdoc />
    public override void Update()
    {
        _elapsed += (float)Game.UpdateTime.Elapsed.TotalSeconds;

        var progress = Math.Clamp(_elapsed / Duration, 0f, 1f);
        var eased = MathUtilEx.Interpolate(0f, 1f, progress, EasingFunction.CubicEaseOut);

        // The step since last frame, not an absolute position: whatever the controller moved the
        // camera by this frame stays
        Entity.Transform.Position += (_to - _from) * (eased - _eased);
        _eased = eased;

        if (progress >= 1f) Entity.Remove(this);
    }
}
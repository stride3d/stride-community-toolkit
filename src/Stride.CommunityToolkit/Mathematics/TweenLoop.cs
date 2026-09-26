namespace Stride.CommunityToolkit.Mathematics;

/// <summary>
/// What a <see cref="Tween"/> does when its clock reaches the end of its duration.
/// </summary>
public enum TweenLoop
{
    /// <summary>Stops at the end and stays there; <see cref="Tween.IsComplete"/> turns on.</summary>
    None,

    /// <summary>Jumps back to the start and runs again, for a spin or a pulse that never stops.</summary>
    Repeat,

    /// <summary>Runs back to the start along the same curve, then forward again, for a lift or a breathing glow.</summary>
    PingPong,
}
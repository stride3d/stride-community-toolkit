namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// The shape a <see cref="CharacterMover2D"/> casts downward to find the ground under its feet.
/// </summary>
public enum PogoShape
{
    /// <summary>A single point below the capsule's centre: cheapest, and slips off ledges soonest.</summary>
    Point,

    /// <summary>A circle of half the capsule's radius: smooths over small steps.</summary>
    Circle,

    /// <summary>A horizontal segment three quarters of the radius each side: stands on ledges the point would miss. The default.</summary>
    Segment,
}
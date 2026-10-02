using Stride.Core.Mathematics;
using Stride.Engine.Events;

namespace E20_2D_Pong;

/// <summary>One side of the court, and the player on it.</summary>
public enum Side
{
    Left,
    Right,
}

/// <summary>
/// The court's measures, in world units, and the two events the scripts talk through. The camera
/// shows 10 units of height, so the court fills a 16:9 window with a little room around it.
/// </summary>
public static class Court
{
    /// <summary>Half the court's width: the ball is out beyond it.</summary>
    public const float HalfWidth = 8f;

    /// <summary>Half the court's height: the ball bounces off it.</summary>
    public const float HalfHeight = 4.2f;

    /// <summary>How far from the centre line each paddle stands.</summary>
    public const float PaddleX = 7.2f;

    /// <summary>A paddle's thickness and length.</summary>
    public static readonly Vector2 PaddleSize = new(0.3f, 1.6f);

    /// <summary>The ball's diameter.</summary>
    public const float BallSize = 0.3f;

    /// <summary>The points that win a game.</summary>
    public const int WinningScore = 7;

    /// <summary>
    /// Sent by the ball when it leaves the court, with the side that scored. The ball does not know
    /// who listens: the referee does today, and a sound or a camera shake could tomorrow without
    /// the ball changing.
    /// </summary>
    public static readonly EventKey<Side> PointScored = new("Pong", "Point scored");

    /// <summary>Sent by the referee to put the ball in play, with the side it is served towards.</summary>
    public static readonly EventKey<Side> Serve = new("Pong", "Serve");
}
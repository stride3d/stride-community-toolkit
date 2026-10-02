using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;

namespace E20_2D_Pong;

/// <summary>
/// The ball. It moves in a straight line, bounces off the top and bottom of the court and off the
/// paddles, and says so when it leaves the court. No physics engine: a bounce is one sign flip, and
/// the angle off a paddle is a rule of the game, not of nature.
/// </summary>
public class Ball : SyncScript
{
    private const float ServeSpeed = 7f;
    private const float TopSpeed = 16f;

    /// <summary>How much faster the ball leaves a paddle than it arrived.</summary>
    private const float SpeedUp = 1.06f;

    /// <summary>The steepest angle the ball leaves a paddle at, from a hit on the paddle's very end.</summary>
    private const float SteepestDegrees = 50f;

    private readonly Random _random = new(7);
    private EventReceiver<Side>? _serve;

    /// <summary>The left paddle's entity.</summary>
    public required Entity LeftPaddle { get; init; }

    /// <summary>The right paddle's entity.</summary>
    public required Entity RightPaddle { get; init; }

    /// <summary>Where the ball is heading, in units per second. Zero while it waits for a serve.</summary>
    public Vector2 Velocity { get; private set; }

    /// <summary>A receiver listens from the moment it is made, so it is made when the script starts.</summary>
    public override void Start() => _serve = new EventReceiver<Side>(Court.Serve);

    /// <summary>And it listens until it is disposed: a script that leaves the scene must let go of it.</summary>
    public override void Cancel() => _serve?.Dispose();

    public override void Update()
    {
        if (_serve is not null && _serve.TryReceive(out var towards)) Serve(towards);

        if (Velocity == Vector2.Zero) return;

        // A long frame - a window being dragged - must not throw the ball across the court
        var elapsed = MathF.Min((float)Game.UpdateTime.Elapsed.TotalSeconds, 1f / 30f);
        var before = Entity.Transform.Position;
        var position = before + new Vector3(Velocity * elapsed, 0f);

        BounceOffWalls(ref position);
        BounceOffPaddle(LeftPaddle, before.X, ref position);
        BounceOffPaddle(RightPaddle, before.X, ref position);

        Entity.Transform.Position = position;

        // Out on the left is a point for the right, and the other way round
        if (MathF.Abs(position.X) > Court.HalfWidth)
        {
            Velocity = Vector2.Zero;

            Court.PointScored.Broadcast(position.X < 0f ? Side.Right : Side.Left);
        }
    }

    /// <summary>Puts the ball on the centre spot and sends it towards a side, a little up or down.</summary>
    private void Serve(Side towards)
    {
        var angle = MathUtil.DegreesToRadians(_random.Next(-20, 21));
        var direction = towards == Side.Left ? -1f : 1f;

        Entity.Transform.Position = Vector3.Zero;
        Velocity = new Vector2(direction * MathF.Cos(angle), MathF.Sin(angle)) * ServeSpeed;
    }

    private void BounceOffWalls(ref Vector3 position)
    {
        var limit = Court.HalfHeight - Court.BallSize * 0.5f;

        if (MathF.Abs(position.Y) <= limit) return;

        // Mirrored back in by as much as it went out, so the path stays a straight reflection
        position.Y = MathF.Sign(position.Y) * (2f * limit - MathF.Abs(position.Y));
        Velocity = new Vector2(Velocity.X, -Velocity.Y);
    }

    /// <summary>
    /// A paddle is a line the ball must not cross: its face, the side towards the centre. The test is
    /// whether the ball crossed that line during this frame, not whether it overlaps the paddle now,
    /// so a fast ball cannot skip through a thin paddle between two frames.
    /// </summary>
    private void BounceOffPaddle(Entity paddle, float xBefore, ref Vector3 position)
    {
        var centre = paddle.Transform.Position;

        // Towards the centre line from the paddle: +1 for the left paddle, -1 for the right
        var inward = -MathF.Sign(centre.X);
        var face = centre.X + inward * (Court.PaddleSize.X + Court.BallSize) * 0.5f;

        var approaching = Velocity.X * inward < 0f;
        var crossed = (xBefore - face) * inward >= 0f && (position.X - face) * inward < 0f;

        if (!approaching || !crossed) return;

        // Where along the paddle it hit: 0 in the middle, 1 or -1 at the ends
        var reach = (Court.PaddleSize.Y + Court.BallSize) * 0.5f;
        var offset = (position.Y - centre.Y) / reach;

        if (MathF.Abs(offset) > 1f) return;

        // The rule that makes Pong a game: the further from the middle, the steeper the return, so
        // a player aims with where the paddle meets the ball
        var angle = MathUtil.DegreesToRadians(offset * SteepestDegrees);
        var speed = MathF.Min(Velocity.Length() * SpeedUp, TopSpeed);

        Velocity = new Vector2(inward * MathF.Cos(angle), MathF.Sin(angle)) * speed;
        position.X = face;
    }
}
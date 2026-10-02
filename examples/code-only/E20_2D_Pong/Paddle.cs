using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace E20_2D_Pong;

/// <summary>
/// A paddle: moved up and down by two keys, or by the computer when nobody plays that side.
/// </summary>
public class Paddle : SyncScript
{
    private const float Speed = 9f;

    /// <summary>Slower than a player, so the computer can be beaten with a steep shot.</summary>
    private const float ComputerSpeed = 6f;

    /// <summary>The key that moves the paddle up.</summary>
    public required Keys Up { get; init; }

    /// <summary>The key that moves the paddle down.</summary>
    public required Keys Down { get; init; }

    /// <summary>The ball, which the computer watches.</summary>
    public required Ball Ball { get; init; }

    /// <summary>Whether the computer plays this paddle.</summary>
    public bool Computer { get; set; }

    public override void Update()
    {
        var elapsed = (float)Game.UpdateTime.Elapsed.TotalSeconds;
        var position = Entity.Transform.Position;

        var step = Computer ? ComputerStep(position) * ComputerSpeed : PlayerStep() * Speed;
        var limit = Court.HalfHeight - Court.PaddleSize.Y * 0.5f;

        position.Y = MathUtil.Clamp(position.Y + step * elapsed, -limit, limit);

        Entity.Transform.Position = position;
    }

    /// <summary>Up is 1, down is -1, both or neither is 0.</summary>
    private float PlayerStep()
        => (Input.IsKeyDown(Up) ? 1f : 0f) - (Input.IsKeyDown(Down) ? 1f : 0f);

    /// <summary>
    /// The computer follows the ball while the ball comes its way, and drifts back to the middle
    /// while it goes away. The step shrinks near the target, so the paddle settles and does not
    /// shake around it.
    /// </summary>
    private float ComputerStep(Vector3 position)
    {
        var approaching = Ball.Velocity.X * position.X > 0f;
        var target = approaching ? Ball.Entity.Transform.Position.Y : 0f;

        return MathUtil.Clamp((target - position.Y) * 4f, -1f, 1f);
    }
}
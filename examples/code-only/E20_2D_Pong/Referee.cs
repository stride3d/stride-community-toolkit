using Stride.CommunityToolkit.Rendering.Text;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Input;

namespace E20_2D_Pong;

/// <summary>
/// The referee runs the match: it serves, counts the points and names the winner. It is an
/// <see cref="AsyncScript"/>, so the whole match reads top to bottom as one method - wait for a
/// key, serve, wait for a point - where a <see cref="SyncScript"/> would need a state variable and
/// a switch to remember where it was each frame.
/// </summary>
public class Referee : AsyncScript
{
    private readonly int[] _scores = new int[2];
    private bool _inMatch;

    /// <summary>The left player's paddle.</summary>
    public required Paddle LeftPaddle { get; init; }

    /// <summary>The right player's paddle.</summary>
    public required Paddle RightPaddle { get; init; }

    /// <summary>The left player's score on the court.</summary>
    public required WorldTextComponent LeftScore { get; init; }

    /// <summary>The right player's score on the court.</summary>
    public required WorldTextComponent RightScore { get; init; }

    /// <summary>The line under the court that says what to press.</summary>
    public required WorldTextComponent Message { get; init; }

    /// <summary>Whether a match is on. False during the demo the game opens with.</summary>
    public bool InMatch => _inMatch;

    /// <summary>Whether a second player has the right paddle. The computer plays it otherwise.</summary>
    public bool TwoPlayers
    {
        get;
        set
        {
            field = value;

            // In the demo the computer keeps both paddles whatever this says
            if (_inMatch) RightPaddle.Computer = !value;
        }
    }

    public override async Task Execute()
    {
        using var point = new EventReceiver<Side>(Court.PointScored);

        // One frame, so that every script has started: a broadcast nobody listens to yet is lost
        await Script.NextFrame();

        await Demo(point);

        while (Game.IsRunning)
        {
            var winner = await Match(point);

            Message.Text = $"{winner} wins - Space for a new game";

            await KeyPressed(Keys.Space);
        }
    }

    /// <summary>Until someone presses Space the computer plays both sides, so the game shows itself.</summary>
    private async Task Demo(EventReceiver<Side> point)
    {
        LeftPaddle.Computer = true;
        RightPaddle.Computer = true;
        Message.Text = "Space to play";

        Court.Serve.Broadcast(Side.Right);

        while (Game.IsRunning && !Input.IsKeyPressed(Keys.Space))
        {
            // Nobody keeps score in the demo: a point is just the next serve, towards who lost it
            if (point.TryReceive(out var scorer)) Court.Serve.Broadcast(Other(scorer));

            await Script.NextFrame();
        }
    }

    /// <summary>One match, from 0:0 to the winning score. Returns the winner.</summary>
    private async Task<Side> Match(EventReceiver<Side> point)
    {
        _scores[0] = _scores[1] = 0;
        ShowScores();

        // A point from the demo, scored on the very frame Space went down, must not count
        point.Reset();

        _inMatch = true;
        LeftPaddle.Computer = false;
        RightPaddle.Computer = !TwoPlayers;

        var towards = Side.Right;

        while (Game.IsRunning)
        {
            Message.Text = string.Empty;

            Court.Serve.Broadcast(towards);

            // The await ends when the ball broadcasts the point, however many frames that takes
            var scorer = await point.ReceiveAsync();

            _scores[(int)scorer]++;
            ShowScores();

            if (_scores[(int)scorer] >= Court.WinningScore) return scorer;

            // The side that lost the point receives the next serve
            towards = Other(scorer);
            Message.Text = "Space to serve";

            await KeyPressed(Keys.Space);
        }

        return Side.Left;
    }

    private async Task KeyPressed(Keys key)
    {
        // The frame after the one that ended a wait on the same key, or one press would count twice
        await Script.NextFrame();

        while (Game.IsRunning && !Input.IsKeyPressed(key)) await Script.NextFrame();
    }

    private void ShowScores()
    {
        LeftScore.Text = $"{_scores[(int)Side.Left]}";
        RightScore.Text = $"{_scores[(int)Side.Right]}";
    }

    private static Side Other(Side side) => side == Side.Left ? Side.Right : Side.Left;
}
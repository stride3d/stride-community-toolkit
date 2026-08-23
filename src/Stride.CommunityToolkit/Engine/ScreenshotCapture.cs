using Stride.Engine;
using Stride.Games;
using Stride.Graphics;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Captures a screenshot of a running game after a set number of frames, then exits.
/// </summary>
/// <remarks>
/// <para>
/// This exists so the documentation's example screenshots can be produced automatically. It is opt-in
/// through environment variables and does nothing at all unless
/// <see cref="OutputPathVariable"/> is set, so a game shipping against the toolkit carries one
/// environment-variable read per <c>Run</c> and nothing else.
/// </para>
/// <para>
/// It is wired into <c>GameExtensions.Run</c> rather than into the scene helpers because running is the
/// one thing every example does. All of them reach the loop through <c>Run</c> - including the F# and
/// Visual Basic ports - whereas the scene helpers are each used by only a subset.
/// </para>
/// <para>
/// <b>Capture is scheduled by frame, not by elapsed time.</b> A fixed delay would photograph a
/// different moment on every run, which matters because most examples are things falling, spinning or
/// settling. Frame scheduling alone is still not enough: <see cref="GameBase.IsFixedTimeStep"/>
/// defaults to <see langword="false"/>, so frame N arrives after a different amount of simulated time
/// on a fast machine than a slow one. Capture therefore forces a fixed timestep, which is what makes
/// the output reproducible rather than merely consistent.
/// </para>
/// </remarks>
public static class ScreenshotCapture
{
    /// <summary>Set to a file path to enable capture. Nothing happens when it is unset.</summary>
    public const string OutputPathVariable = "STRIDE_TOOLKIT_CAPTURE";

    /// <summary>Optional. Which frame to capture; defaults to <see cref="DefaultFrame"/>.</summary>
    public const string FrameVariable = "STRIDE_TOOLKIT_CAPTURE_FRAME";

    /// <summary>
    /// The frame captured when <see cref="FrameVariable"/> is not set.
    /// </summary>
    /// <remarks>
    /// Late enough for the first shaders to have compiled and for a scene to have settled into
    /// something worth looking at, short enough that capturing sixty examples is not an afternoon.
    /// </remarks>
    public const int DefaultFrame = 240;

    /// <summary>
    /// Schedules a capture if the environment asks for one.
    /// </summary>
    /// <param name="game">The game about to run.</param>
    /// <returns><see langword="true"/> if a capture was scheduled.</returns>
    public static bool TrySchedule(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        if (Environment.GetEnvironmentVariable(OutputPathVariable) is not { Length: > 0 } outputPath)
        {
            return false;
        }

        var frame = Environment.GetEnvironmentVariable(FrameVariable) is { Length: > 0 } raw
            && int.TryParse(raw, out var parsed)
            && parsed > 0
                ? parsed
                : DefaultFrame;

        // Without this, frame N is a different instant on every machine and every run.
        game.IsFixedTimeStep = true;
        game.IsDrawDesynchronized = false;

        game.GameSystems.Add(new ScreenshotSystem(game, outputPath, frame));

        return true;
    }

    /// <summary>
    /// Counts frames and saves one, from the end of the draw phase.
    /// </summary>
    /// <remarks>
    /// A game system rather than a script coroutine, and the choice is not cosmetic. Saving the render
    /// target submits the command list; done from a script - which runs in the update phase, mid-frame -
    /// it cuts across anything holding paired begin/end state across the frame. ImGui detects exactly
    /// that and aborts the process with "Forgot to call Render() or EndFrame() at the end of the
    /// previous frame?", so the two ImGui examples were the only ones that could not be captured.
    /// Drawing last, after the scene renderer has finished with the frame, there is nothing to cut
    /// across. Stride's own regression harness schedules its screenshots the same way.
    /// </remarks>
    private sealed class ScreenshotSystem : GameSystemBase
    {
        private readonly Game _game;
        private readonly string _outputPath;
        private readonly int _targetFrame;
        private int _frame;
        private bool _captured;

        public ScreenshotSystem(Game game, string outputPath, int targetFrame)
            : base(game.Services)
        {
            _game = game;
            _outputPath = outputPath;
            _targetFrame = targetFrame;

            Enabled = true;
            Visible = true;

            // Last in the frame, so the scene has already been rendered into the target being saved.
            DrawOrder = int.MaxValue;
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (_captured || ++_frame < _targetFrame)
            {
                return;
            }

            _captured = true;

            try
            {
                var directory = Path.GetDirectoryName(Path.GetFullPath(_outputPath));

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Saves the GPU render target, not the screen: no window handle, no DPI scaling, no
                // foreground requirement, and the window may sit behind others while it happens.
                _game.TakeScreenShot(_outputPath, ImageFileType.Png);
            }
            catch (Exception ex)
            {
                // The orchestrator decides what a failure means; the game's job is to not hang.
                Console.Error.WriteLine($"Screenshot capture failed: {ex.Message}");
            }

            _game.Exit();
        }
    }
}
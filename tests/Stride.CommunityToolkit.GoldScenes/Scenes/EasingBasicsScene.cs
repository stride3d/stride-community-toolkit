using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.GoldScenes.Scenes;

/// <summary>
/// The four lanes of the easing basics example, drawn from the simulated time: linear by hand, the
/// cubic ease-out by hand, the same curve through <see cref="EasingFunctionExtensions"/>, and the
/// same curve through a <see cref="Tween"/>. The last three discs must sit on one vertical line;
/// a drift between the hand formula, the library and the tween moves a disc and fails the golden.
/// </summary>
internal sealed class EasingBasicsScene : IGoldScene
{
    private const float Duration = 2f;
    private const float StartX = -10f;
    private const float EndX = 10f;

    private readonly Tween _tween = Tween.Run(Duration, EasingFunction.CubicEaseOut);
    private ShapeBatch? _shapes;
    private float _elapsed;

    public void Start(Game game, Scene scene)
    {
        game.SetupBase2D(new Color(24, 26, 32));

        var camera = scene.GetCamera() ?? throw new InvalidOperationException("The 2D scene has no camera.");

        camera.OrthographicSize = 16f;

        _shapes = game.AddShapeBatch();
    }

    public void Update(Game game, Scene scene, GameTime time)
    {
        if (_shapes is not { } shapes) return;

        var dt = (float)time.Elapsed.TotalSeconds;

        _elapsed += dt;
        _tween.Update(dt);

        var t = Math.Clamp(_elapsed / Duration, 0f, 1f);

        float[] positions =
        [
            MathUtil.Lerp(StartX, EndX, t),
            MathUtil.Lerp(StartX, EndX, 1f - (1f - t) * (1f - t) * (1f - t)),
            EasingFunction.CubicEaseOut.Interpolate(StartX, EndX, _elapsed / Duration),
            _tween.Lerp(StartX, EndX),
        ];

        for (var lane = 0; lane < positions.Length; lane++)
        {
            var y = 4.5f - lane * 3f;
            var colour = lane == 0 ? new Color(160, 166, 180) : new Color(90, 200, 255);

            shapes.DrawPixelLine(new Vector3(StartX, y, 0f), new Vector3(EndX, y, 0f), 2f, new Color(70, 76, 90));

            for (var i = 0; i <= 10; i++)
            {
                var s = lane == 0 ? i / 10f : Easing.CubicEaseOut(i / 10f);

                shapes.DrawPixelDisc(new Vector3(MathUtil.Lerp(StartX, EndX, s), y - 0.75f, 0f), 2.5f, colour);
            }

            shapes.Fill.Set(colour, 1f);
            shapes.DrawDisc(new Vector3(positions[lane], y, 0f), Vector3.UnitZ, 0.45f, Color.White);
            shapes.Fill.Set(null, 1f);
        }
    }
}
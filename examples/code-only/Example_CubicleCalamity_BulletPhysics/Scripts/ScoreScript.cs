using Stride.CommunityToolkit.Mathematics;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example_CubicleCalamity.Scripts;

public class ScoreScript : SyncScript
{
    private float _maxHeight = 25f;
    private TimeSpan _elapsed = TimeSpan.Zero;
    private TimeSpan _duration = TimeSpan.FromSeconds(3);
    private Vector3 _startPosition = new(0, 8, 0);

    public override void Start()
    {
        _startPosition = Entity.Transform.Position;
    }

    public override void Update()
    {
        var topPosition = _startPosition + new Vector3(0, _maxHeight, 0);

        var progress = (float)(_elapsed.TotalSeconds / _duration.TotalSeconds);

        //if (progress > 1.0f)
        //{
        //    progress = 1.0f;
        //}

        var position = MathUtilEx.Interpolate(_startPosition, topPosition, progress, EasingFunction.SineEaseIn);

        Entity.Transform.Position = position;

        // Check if the entity has reached a certain height
        if (Entity.Transform.Position.Y > _maxHeight)
        {
            // Remove the entity from the scene
            Entity.Scene = null;
        }

        _elapsed += Game.UpdateTime.Elapsed;
    }
}
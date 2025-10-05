using Stride.CommunityToolkit.Games;
using Stride.Engine;
using System.Numerics;

namespace Example02_GiveMeACube;

public class RotationComponentScript : SyncScript
{
    private Vector3 _initialPosition = Vector3.Zero;
    private float _rotateSpeed = 1f;
    private float _radius = 3f;
    private float _angle;

    public override void Start()
    {
        Log.Warning("Start Logging");

        _initialPosition = Entity.Transform.Position;
    }

    public override void Update()
    {
        _angle += _rotateSpeed * Game.DeltaTime();

        var offset = new Vector3((float)Math.Sin(_angle), 0, (float)Math.Cos(_angle)) * _radius;

        Entity.Transform.Position = _initialPosition + offset;
    }
}
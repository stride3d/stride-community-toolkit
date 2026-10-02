using Stride.BepuPhysics;
using Stride.Engine;
using System.Numerics;

namespace E02_3D_SyncScript;

public class RotationComponentScript : SyncScript
{
    private Vector3 _initialPosition = Vector3.Zero;
    private readonly float _rotateSpeed = 2f;
    private readonly float _radius = 3f;
    private float _angle;
    private BodyComponent? _body;

    public override void Start()
    {
        _body = Entity.Get<BodyComponent>();

        if (_body is not null)
            _body.Kinematic = true;

        _initialPosition = Entity.Transform.Position;
    }

    public override void Update()
    {
        _angle += _rotateSpeed * (float)Game.UpdateTime.Elapsed.TotalSeconds;

        var offset = new Vector3((float)Math.Sin(_angle), 0, (float)Math.Cos(_angle)) * _radius;
        var targetPosition = _initialPosition + offset;

        _body?.SetTargetPose(targetPosition, Entity.Transform.Rotation);
    }
}
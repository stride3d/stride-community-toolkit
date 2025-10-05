using Stride.BepuPhysics;
using Stride.Engine;
using System.Numerics;

namespace Example01_Basic3DScene_SyncScript;

public class RotationComponentScript : SyncScript
{
    private Vector3 _initialPosition = Vector3.Zero;
    private float _rotateSpeed = 2f;
    private float _radius = 3f;
    private float _angle;
    BodyComponent? _sphereBody;

    public override void Start()
    {
        _sphereBody = Entity.Get<BodyComponent>();

        if (_sphereBody is { })
            _sphereBody.Kinematic = true;

        _initialPosition = Entity.Transform.Position;
    }

    public override void Update()
    {
        _angle += _rotateSpeed * (float)Game.UpdateTime.Elapsed.TotalSeconds;

        var offset = new Vector3((float)Math.Sin(_angle), 0, (float)Math.Cos(_angle)) * _radius;
        var targetPosition = _initialPosition + offset;

        _sphereBody?.SetTargetPose(targetPosition, Entity.Transform.Rotation);
    }
}
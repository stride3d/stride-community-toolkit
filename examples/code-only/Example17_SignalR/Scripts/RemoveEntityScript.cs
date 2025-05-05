using Example17_SignalR.Core;
using Stride.Engine;

namespace Example17_SignalR.Scripts;

public class RemoveEntityScript : SyncScript
{
    RobotComponent? _robotComponent;

    public override void Start()
    {
        _robotComponent = Entity.Get<RobotComponent>();
    }

    public override void Update()
    {
        if (_robotComponent == null) return;

        if (!_robotComponent.IsDeleted) return;

        Entity.Scene = null;
    }
}
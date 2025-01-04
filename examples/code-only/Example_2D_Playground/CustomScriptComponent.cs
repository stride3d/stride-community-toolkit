using Stride.Engine;
using Stride.Physics;

namespace Example_2D_Playground;

public class CustomScriptComponent : SyncScript
{
    private readonly Action<Entity>? _startupScriptLogic;
    private readonly Action<Entity>? _updateScriptLogic;

    private bool _isShapesRendering;

    public CustomScriptComponent() { }

    public CustomScriptComponent(Action<Entity>? startupScriptLogic = null, Action<Entity>? updateScriptLogic = null)
    {
        _updateScriptLogic = updateScriptLogic;
        _startupScriptLogic = startupScriptLogic;
    }

    public override void Start()
    {
        var simulation = this.GetSimulation();

        if (simulation is not null && !_isShapesRendering)
        {
            simulation.ColliderShapesRendering = true;

            _isShapesRendering = true;
        }

        if (_startupScriptLogic is null) return;

        _startupScriptLogic.Invoke(Entity);
    }

    public override void Update()
    {
        if (_updateScriptLogic is null) return;

        _updateScriptLogic.Invoke(Entity);
    }
}
using Stride.Engine;

namespace Stride.CommunityToolkit.Engine;

internal readonly struct ScriptDelegateWatcher
{
    private readonly ScriptComponent? _script;

    public ScriptDelegateWatcher(Delegate? @delegate)
    {
        ArgumentNullException.ThrowIfNull(@delegate);

        var invocationList = @delegate.GetInvocationList();

        if (invocationList.Length == 1 && invocationList[0].Target is ScriptComponent scriptComponent)
        {
            _script = scriptComponent;
        }
        else
        {
            _script = null;
        }
    }

    public bool IsActive => _script is null || (_script.Entity != null && (_script.SceneSystem?.SceneInstance?.Contains(_script.Entity) == true));
}

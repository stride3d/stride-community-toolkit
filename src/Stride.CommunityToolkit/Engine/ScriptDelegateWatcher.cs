using Stride.Engine;

namespace Stride.CommunityToolkit.Engine;

internal readonly struct ScriptDelegateWatcher
{
    private readonly ScriptComponent script;

    public ScriptDelegateWatcher(Delegate @delegate)
    {
        if (@delegate == null)
        {
            throw new ArgumentNullException(nameof(@delegate));
        }

        var invocationList = @delegate.GetInvocationList();

        if (invocationList.Length == 1 && invocationList[0].Target is ScriptComponent scriptComponent)
        {
            script = scriptComponent;
        }
        else
        {
            script = null;
        }

    }

    public bool IsActive => script == null || (script.Entity != null && (script.SceneSystem?.SceneInstance?.Contains(script.Entity) == true));
}

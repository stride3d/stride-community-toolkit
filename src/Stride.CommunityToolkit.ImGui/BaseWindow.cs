using Hexa.NET.ImGui;
using Stride.Core;
using Stride.Engine;
using Stride.Games;
using System.Numerics;
using static Hexa.NET.ImGui.ImGui;
using static Stride.CommunityToolkit.ImGui.ImGuiExtension;

namespace Stride.CommunityToolkit.ImGui;

/// <summary>
/// Base class for creating ImGui windows in Stride.
/// </summary>
public abstract class BaseWindow : GameSystem
{
    /// <summary>
    /// The UI scale of the owning <see cref="ImGuiSystem"/>, or <c>1</c> while no system is registered yet.
    /// Multiply pixel sizes by it so windows look the same at every DPI.
    /// </summary>
    public float Scale => _imGui?.Scale ?? 1f;

    private static Dictionary<string, uint> _windowId = new Dictionary<string, uint>();

    /// <summary>
    /// Bound to the window's close button. Set it to <see langword="false"/> to close the window; once closed the
    /// system is disabled and disposed.
    /// </summary>
    protected bool Open = true;

    /// <summary>
    /// A number distinguishing this instance from other windows of the same type: <c>1</c> for the first, then counting
    /// up. It is appended to the window title so ImGui treats each instance as a separate window.
    /// </summary>
    protected uint Id;

    /// <summary>
    /// The flags passed to ImGui when the window is begun. Override to change borders, movement, saved settings and so on.
    /// </summary>
    protected virtual ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.None;

    /// <summary>
    /// The position applied before the window is begun each frame, or <see langword="null"/> to let ImGui place it.
    /// </summary>
    protected virtual Vector2? WindowPos => null;

    /// <summary>
    /// The size applied before the window is begun each frame, or <see langword="null"/> to let ImGui size it.
    /// </summary>
    protected virtual Vector2? WindowSize => null;
    private readonly string _uniqueName;
    private ImGuiSystem? _imGui;

    ///<inheritdoc />
    protected BaseWindow(IServiceRegistry services) : base(services)
    {
        Game.GameSystems.Add(this);
        Enabled = true;
        var n = GetType().Name;
        lock (_windowId)
        {
            if (_windowId.TryGetValue(n, out Id) == false)
            {
                Id = 1;
                _windowId.Add(n, Id);
            }

            _windowId[n] = Id + 1;
        }
        _uniqueName = Id == 1 ? n : $"{n}({Id})";

        _imGui ??= Services.GetService<ImGuiSystem>();
    }

    ///<inheritdoc />
    public override void Update(GameTime gameTime)
    {
        // Allow for some leeway to avoid throwing
        // if imGui as not been set yet
        _imGui ??= Services.GetService<ImGuiSystem>();

        if (_imGui is null) return;

        // This component must run after imGui to
        // avoid throwing and single frame lag
        if (UpdateOrder <= _imGui.UpdateOrder)
        {
            UpdateOrder = _imGui.UpdateOrder + 1;
            return;
        }

        if (WindowPos != null)
        {
            SetNextWindowPos(WindowPos.Value);
        }

        if (WindowSize != null)
        {
            SetNextWindowSize(WindowSize.Value);
        }

        using (Window(_uniqueName, ref Open, out bool collapsed, WindowFlags))
        {
            OnDraw(collapsed);
        }

        if (!Open)
        {
            Enabled = false;
            Dispose();
        }
    }

    ///<inheritdoc />
    protected abstract void OnDraw(bool collapsed);

    ///<inheritdoc />
    protected abstract void OnDestroy();

    ///<inheritdoc />
    protected override void Destroy()
    {
        Game.GameSystems.Remove(this);
        OnDestroy();
        base.Destroy();
    }
}
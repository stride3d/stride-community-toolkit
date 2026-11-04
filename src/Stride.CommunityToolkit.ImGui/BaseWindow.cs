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
    // The next number for each window type, so two windows of one type get distinct titles
    private static readonly Dictionary<string, uint> _windowId = [];

    private readonly string _uniqueName;
    private ImGuiSystem? _imGui;

    /// <summary>
    /// The UI scale of the owning <see cref="ImGuiSystem"/>, or <c>1</c> while no system is registered yet.
    /// Multiply pixel sizes by it so windows look the same at every DPI.
    /// </summary>
    public float Scale => _imGui?.Scale ?? 1f;

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
    /// Where the window starts, applied once per run, or <see langword="null"/> to let ImGui place it. The user can drag it from there.
    /// </summary>
    protected virtual Vector2? WindowPos => null;

    /// <summary>
    /// The size the window starts at, applied once per run, or <see langword="null"/> to let ImGui size it. The user can resize it from there.
    /// </summary>
    protected virtual Vector2? WindowSize => null;

    ///<inheritdoc />
    protected BaseWindow(IServiceRegistry services) : base(services)
    {
        Game.GameSystems.Add(this);
        Enabled = true;

        var name = GetType().Name;

        lock (_windowId)
        {
            Id = _windowId.TryGetValue(name, out var next) ? next : 1;
            _windowId[name] = Id + 1;
        }

        _uniqueName = Id == 1 ? name : $"{name}({Id})";
        _imGui = Services.GetService<ImGuiSystem>();
    }

    ///<inheritdoc />
    public override void Update(GameTime gameTime)
    {
        // The system may be registered after the window, so look again until it is there
        _imGui ??= Services.GetService<ImGuiSystem>();

        if (_imGui is null) return;

        // The window must run after the ImGui system, or it draws into a frame that has not begun
        if (UpdateOrder <= _imGui.UpdateOrder)
        {
            UpdateOrder = _imGui.UpdateOrder + 1;
            return;
        }

        // Once, not every frame: a position set every frame pins the window where the user cannot drag it
        if (WindowPos is { } position) SetNextWindowPos(position, ImGuiCond.Once);
        if (WindowSize is { } size) SetNextWindowSize(size, ImGuiCond.Once);

        using (Window(_uniqueName, ref Open, out var collapsed, WindowFlags))
        {
            OnDraw(collapsed);
        }

        if (!Open)
        {
            Enabled = false;
            Dispose();
        }
    }

    /// <summary>Builds the window's content each frame. Skip drawing when <paramref name="collapsed"/> is <see langword="true"/>.</summary>
    protected abstract void OnDraw(bool collapsed);

    /// <summary>Called once when the window is destroyed, to release what it owns.</summary>
    protected abstract void OnDestroy();

    ///<inheritdoc />
    protected override void Destroy()
    {
        Game.GameSystems.Remove(this);
        OnDestroy();
        base.Destroy();
    }
}
using Hexa.NET.ImGui;
using Stride.Graphics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using static Hexa.NET.ImGui.ImGui;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;
using IDisposable = System.IDisposable;

namespace Stride.CommunityToolkit.ImGui;

/// <summary>
/// Helpers that wrap Dear ImGui's <c>Begin*</c>/<c>End*</c> pairs in disposables so a UI scope can be written as a
/// <see langword="using"/> block, plus a few Stride-flavoured widgets (textures, <see cref="Stride.Core.Mathematics.Color3"/>).
/// </summary>
/// <remarks>
/// Import with <c>using static Stride.CommunityToolkit.ImGui.ImGuiExtension;</c> and pair with
/// <c>using static Hexa.NET.ImGui.ImGui;</c>, then write the UI as nested <see langword="using"/> blocks:
/// <code>
/// using (Window("Stats", ref open, out var collapsed))
/// {
///     if (!collapsed)
///         using (UColumns(2)) { TextUnformatted("FPS"); NextColumn(); TextUnformatted("60"); }
/// }
/// </code>
/// Each helper returns a struct whose <c>Dispose</c> calls the matching <c>End*</c>, so the pair cannot be mismatched.
/// </remarks>
public class ImGuiExtension
{
    // Dictionary to hold textures
    private static readonly List<Texture> _textureRegistry = [];

    /// <summary>
    /// Gets a pointer to the Texture and adds it to the <see cref="_textureRegistry"/> if it was not previously added.
    /// </summary>
    /// <param name="texture"></param>
    /// <returns></returns>
    internal static ImTextureRef GetTextureKey(Texture texture)
    {
        _textureRegistry.Add(texture);
        ulong id = (ulong)_textureRegistry.Count;

        return new ImTextureRef { TexID = (ImTextureID)(nint)id };
    }

    /// <summary>
    /// Attempts to convert a pointer to a texture if its in the <see cref="_textureRegistry"/>
    /// </summary>
    /// <param name="key"></param>
    /// <param name="texture"></param>
    /// <returns></returns>
    internal static bool TryGetTexture(ulong key, [NotNullWhen(true)] out Texture? texture)
    {
        int index = (int)key - 1;
        if (index >= 0 && index < _textureRegistry.Count)
        {
            texture = _textureRegistry[index];
            return true;
        }
        texture = null;
        return false;
    }

    /// <summary>
    /// Clears the <see cref="_textureRegistry"/> that maps textures to their reference ids.
    /// </summary>
    internal static void ClearTextures()
    {
        _textureRegistry.Clear();
    }

    /// <summary>
    /// Pushes <paramref name="id"/> onto the ID stack; disposing the result pops it.
    /// </summary>
    /// <param name="id">The identifier to push, used to keep widgets with the same label distinct.</param>
    /// <returns>A scope whose <see cref="DisposableImGui.Dispose"/> calls <c>PopID</c>.</returns>
    public static DisposableImGui ID(string id)
    {
        PushID(id);
        return new DisposableImGui(true, DisposableTypes.ID);
    }

    /// <summary>
    /// Pushes <paramref name="id"/> onto the ID stack; disposing the result pops it.
    /// </summary>
    /// <param name="id">The identifier to push, used to keep widgets with the same label distinct.</param>
    /// <returns>A scope whose <see cref="DisposableImGui.Dispose"/> calls <c>PopID</c>.</returns>
    public static DisposableImGui ID(int id)
    {
        PushID(id);
        return new DisposableImGui(true, DisposableTypes.ID);
    }

    /// <summary>
    /// Begins a combo box; disposing the result ends it when it was opened.
    /// </summary>
    /// <param name="label">The widget label.</param>
    /// <param name="previewValue">The text shown in the closed combo.</param>
    /// <param name="open">Set to <see langword="true"/> when the combo is open and its items should be emitted.</param>
    /// <param name="flags">Combo behaviour flags.</param>
    /// <returns>A scope whose <see cref="DisposableImGui.Dispose"/> calls <c>EndCombo</c> only if <paramref name="open"/> was <see langword="true"/>.</returns>
    public static DisposableImGui UCombo(string label, string previewValue, out bool open, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        return new DisposableImGui(open = BeginCombo(label, previewValue, flags), DisposableTypes.Combo);
    }

    /// <summary>
    /// Begins a tooltip; disposing the result ends it.
    /// </summary>
    /// <returns>A scope whose <see cref="DisposableImGui.Dispose"/> calls <c>EndTooltip</c>.</returns>
    public static DisposableImGui Tooltip()
    {
        BeginTooltip();
        return new DisposableImGui(true, DisposableTypes.Tooltip);
    }

    /// <summary>
    /// Indents the following widgets; disposing the result unindents by the same amount.
    /// </summary>
    /// <param name="size">The indentation in pixels, or <c>0</c> for ImGui's default indent spacing.</param>
    /// <returns>A scope whose <see cref="DisposableImGuiIndent.Dispose"/> calls <c>Unindent</c>.</returns>
    public static DisposableImGuiIndent UIndent(float size = 0f) => new DisposableImGuiIndent(size);

    /// <summary>
    /// Splits the following widgets into columns; disposing the result returns to a single column.
    /// </summary>
    /// <param name="count">The number of columns.</param>
    /// <param name="id">An optional identifier for the column set.</param>
    /// <param name="border">Whether to draw borders between columns.</param>
    /// <returns>A scope whose <see cref="DisposableImGui.Dispose"/> calls <c>Columns(1)</c>.</returns>
    public static DisposableImGui UColumns(int count, string? id = null, bool border = false)
    {
        Columns(count, id, border);
        return new DisposableImGui(true, DisposableTypes.Columns);
    }

    /// <summary>
    /// Begins a window; disposing the result ends it.
    /// </summary>
    /// <param name="name">The window title, which is also its identifier.</param>
    /// <param name="open">Bound to the window's close button; set to <see langword="false"/> when the user closes it.</param>
    /// <param name="collapsed">Set to <see langword="true"/> when the window is collapsed and its content should be skipped.</param>
    /// <param name="flags">Window behaviour flags.</param>
    /// <returns>A scope whose <see cref="DisposableImGui.Dispose"/> calls <c>End</c>.</returns>
    public static DisposableImGui Window(string name, ref bool open, out bool collapsed, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        collapsed = !Begin(name, ref open, flags);
        return new DisposableImGui(true, DisposableTypes.Window);
    }

    /// <summary>
    /// Begins a child region; disposing the result ends it.
    /// </summary>
    /// <param name="cln">Supplied by the compiler: the caller's line number, used as the child's identifier so each call site gets its own region.</param>
    /// <param name="size">The region size, or <see langword="default"/> to fill the available space.</param>
    /// <param name="childFlags">Child region behaviour flags.</param>
    /// <param name="flags">Window behaviour flags applied to the child.</param>
    /// <returns>A scope whose <see cref="DisposableImGui.Dispose"/> calls <c>EndChild</c>.</returns>
    public static unsafe DisposableImGui Child([CallerLineNumber] int cln = 0, Vector2 size = default,
        ImGuiChildFlags childFlags = ImGuiChildFlags.None, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        BeginChild(cln.ToString(), size, childFlags, flags);
        return new DisposableImGui(true, DisposableTypes.Child);
    }

    /// <summary>
    /// Shows a colour picker bound to a Stride <see cref="Stride.Core.Mathematics.Color3"/>.
    /// </summary>
    /// <param name="label">The widget label.</param>
    /// <param name="color">The colour to edit; updated in place when the user changes it.</param>
    /// <returns><see langword="true"/> when the colour changed this frame.</returns>
    public static bool ColorPicker3(string label, ref Stride.Core.Mathematics.Color3 color)
    {
        var lightColorVector = new Vector3(color.R, color.G, color.B);
        var changed = Hexa.NET.ImGui.ImGui.ColorPicker3(label, ref lightColorVector);
        if (changed)
        {
            color.R = lightColorVector.X;
            color.G = lightColorVector.Y;
            color.B = lightColorVector.Z;
        }
        return changed;
    }

    /// <summary>
    /// Adds a texture to the ImGui element with the Texture width and height
    /// </summary>
    /// <param name="texture"></param>
    public static void Image(Texture texture)
    {
        Hexa.NET.ImGui.ImGui.Image(GetTextureKey(texture), new Vector2(texture.Width, texture.Height));
    }

    /// <summary>
    /// Adds a texture to the ImGui element with a custom width and height
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public static void Image(Texture texture, int width, int height)
    {
        Hexa.NET.ImGui.ImGui.Image(GetTextureKey(texture), new Vector2(width, height));
    }

    /// <summary>
    /// Adds a texture to the ImGui element button with the Texture width and height
    /// </summary>
    /// <param name="text"></param>
    /// <param name="texture"></param>
    /// <returns></returns>
    public static bool ImageButton(string text, Texture texture)
    {
        return Hexa.NET.ImGui.ImGui.ImageButton(text, GetTextureKey(texture), new Vector2(texture.Width, texture.Height));
    }

    /// <summary>
    /// Adds a texture to the ImGui element button with a custom width and height
    /// </summary>
    /// <param name="strid"></param>
    /// <param name="texture"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static bool ImageButton(string strid, Texture texture, int width, int height)
    {
        return Hexa.NET.ImGui.ImGui.ImageButton(strid, GetTextureKey(texture), new Vector2(width, height));
    }

    /// <summary>
    /// Begins the window's menu bar; disposing the result ends it when it was opened.
    /// </summary>
    /// <param name="open">Set to <see langword="true"/> when the menu bar is shown and its menus should be emitted.</param>
    /// <returns>A scope whose <see cref="DisposableImGui.Dispose"/> calls <c>EndMenuBar</c> only if <paramref name="open"/> was <see langword="true"/>.</returns>
    public static DisposableImGui MenuBar(out bool open) => new DisposableImGui(open = BeginMenuBar(), DisposableTypes.MenuBar);

    /// <summary>
    /// Begins a menu; disposing the result ends it when it was opened.
    /// </summary>
    /// <param name="label">The menu label.</param>
    /// <param name="open">Set to <see langword="true"/> when the menu is open and its items should be emitted.</param>
    /// <param name="enabled">Whether the menu can be opened.</param>
    /// <returns>A scope whose <see cref="DisposableImGui.Dispose"/> calls <c>EndMenu</c> only if <paramref name="open"/> was <see langword="true"/>.</returns>
    public static DisposableImGui Menu(string label, out bool open, bool enabled = true) => new DisposableImGui(open = BeginMenu(label, enabled), DisposableTypes.Menu);

    /// <summary>
    /// Scope returned by <see cref="UIndent"/>: unindents on dispose.
    /// </summary>
    public struct DisposableImGuiIndent : IDisposable
    {
        float _size;

        internal DisposableImGuiIndent(float size = 0f)
        {
            _size = size;
            Indent(size);
        }

        /// <summary>
        /// Calls <c>Unindent</c> with the amount the scope was created with.
        /// </summary>
        public void Dispose()
        {
            Unindent(_size);
        }
    }

    /// <summary>
    /// Scope returned by the <c>Begin*</c> helpers of <see cref="ImGuiExtension"/>: calls the matching <c>End*</c> on dispose.
    /// </summary>
    public struct DisposableImGui : IDisposable
    {
        bool _dispose;
        DisposableTypes _type;

        internal DisposableImGui(bool dispose, DisposableTypes type)
        {
            _dispose = dispose;
            _type = type;
        }

        /// <summary>
        /// Calls the <c>End*</c> function matching the helper that created this scope, or nothing when that helper reported
        /// the element as not open (for example a closed <see cref="UCombo"/> or <see cref="Menu"/>).
        /// </summary>
        public void Dispose()
        {
            if (!_dispose)
                return;

            switch (_type)
            {
                case DisposableTypes.Menu: EndMenu(); return;
                case DisposableTypes.MenuBar: EndMenuBar(); return;
                case DisposableTypes.Child: EndChild(); return;
                case DisposableTypes.Window: End(); return;
                case DisposableTypes.Tooltip: EndTooltip(); return;
                case DisposableTypes.Columns: Columns(1); return;
                case DisposableTypes.Combo: EndCombo(); return;
                case DisposableTypes.ID: PopID(); return;
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal enum DisposableTypes
    {
        Menu,
        MenuBar,
        Child,
        Window,
        Tooltip,
        Columns,
        Combo,
        ID
    }

    /// <summary>
    /// Plots a line graph from a contiguous run of floats, with the parameters Hexa.NET.ImGui leaves optional.
    /// </summary>
    /// <param name="label">The widget label.</param>
    /// <param name="values">A reference to the first value; the plot reads <paramref name="count"/> values from here, <paramref name="stride"/> bytes apart.</param>
    /// <param name="count">The number of values to plot.</param>
    /// <param name="offset">The index of the first value to plot, for ring buffers.</param>
    /// <param name="overlay">Text drawn over the graph.</param>
    /// <param name="valueMin">The lower bound of the vertical axis, or <see cref="float.MaxValue"/> to fit the data.</param>
    /// <param name="valueMax">The upper bound of the vertical axis, or <see cref="float.MaxValue"/> to fit the data.</param>
    /// <param name="size">The graph size, or <see langword="default"/> for ImGui's default.</param>
    /// <param name="stride">The distance in bytes between consecutive values; <c>4</c> for a plain <see cref="float"/> array, larger when plotting one field of a struct array.</param>
    public static void PlotLines
    (
        string label,
        ref float values,
        int count,
        int offset = 0,
        string? overlay = null,
        float valueMin = float.MaxValue,
        float valueMax = float.MaxValue,
        Vector2 size = default,
        int stride = 4)
    {
        Hexa.NET.ImGui.ImGui.PlotLines(label, ref values, count, offset, overlay, valueMin, valueMax, size, stride);
    }
}

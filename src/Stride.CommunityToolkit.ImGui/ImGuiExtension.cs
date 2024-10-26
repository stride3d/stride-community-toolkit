using System.Numerics;
using IDisposable = System.IDisposable;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;

using Hexa.NET.ImGui;
using static Hexa.NET.ImGui.ImGui;
using System.Runtime.CompilerServices;
using Stride.Graphics;
using System.Collections.Generic;
using System;

namespace Stride.CommunityToolkit.ImGuiDebug;
public class ImGuiExtension
{
    // Dictionary to hold textures
    private static readonly List<Texture> _textureRegistry = [];

    /// <summary>
    /// Gets a pointer to the Texture and adds it to the <see cref="_textureRegistry"/> if it was not previously added.
    /// </summary>
    /// <param name="texture"></param>
    /// <returns></returns>
    internal static ulong GetTextureKey(Texture texture)
    {
        _textureRegistry.Add(texture);
        ulong id = (ulong)_textureRegistry.Count;

        return id;
    }

    /// <summary>
    /// Attempts to convert a pointer to a texture if its in the <see cref="_textureRegistry"/>
    /// </summary>
    /// <param name="key"></param>
    /// <param name="texture"></param>
    /// <returns></returns>
    internal static bool TryGetTexture(ulong key, out Texture texture)
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
    /// Clears the dictionaries that contain the mappings between textures and their reference ids:
    /// <see cref="_textureRegistry"/> <see cref="_pointerRegistry"/>
    /// </summary>
    internal static void ClearTextures()
    {
        _textureRegistry.Clear();
    }

    public static DisposableImGui ID(string id)
    {
        PushID(id);
        return new DisposableImGui(true, DisposableTypes.ID);
    }
    public static DisposableImGui ID(int id)
    {
        PushID(id);
        return new DisposableImGui(true, DisposableTypes.ID);
    }
    public static DisposableImGui UCombo(string label, string previewValue, out bool open, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        return new DisposableImGui(open = BeginCombo(label, previewValue, flags), DisposableTypes.Combo);
    }
    public static DisposableImGui Tooltip()
    {
        BeginTooltip();
        return new DisposableImGui(true, DisposableTypes.Tooltip);
    }
    public static DisposableImGuiIndent UIndent(float size = 0f) => new DisposableImGuiIndent(size);
    public static DisposableImGui UColumns(int count, string id = null, bool border = false)
    {
        Columns(count, id, border);
        return new DisposableImGui(true, DisposableTypes.Columns);
    }
    public static DisposableImGui Window(string name, ref bool open, out bool collapsed, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        collapsed = !Begin(name, ref open, flags);
        return new DisposableImGui(true, DisposableTypes.Window);
    }

    public static unsafe DisposableImGui Child([CallerLineNumber] int cln = 0, Vector2 size = default,
        ImGuiChildFlags childFlags = ImGuiChildFlags.None, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        BeginChild(cln.ToString(), size, childFlags, flags);
        return new DisposableImGui(true, DisposableTypes.Child);
    }

    public static bool ColorPicker3(string label, ref Stride.Core.Mathematics.Color3 color)
    {
        var lightColorVector = new Vector3(color.R, color.G, color.B);
        var changed = ImGui.ColorPicker3(label, ref lightColorVector);
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
        ImGui.Image(GetTextureKey(texture), new Vector2(texture.Width, texture.Height));
    }

    /// <summary>
    /// Adds a texture to the ImGui element with a custom width and height
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public static void Image(Texture texture, int width, int height)
    {
        ImGui.Image(GetTextureKey(texture), new Vector2(width, height));
    }

    /// <summary>
    /// Adds a texture to the ImGui element button with the Texture width and height
    /// </summary>
    /// <param name="text"></param>
    /// <param name="texture"></param>
    /// <returns></returns>
    public static bool ImageButton(string text, Texture texture)
    {
        return ImGui.ImageButton(text, GetTextureKey(texture), new Vector2(texture.Width, texture.Height));
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
        return ImGui.ImageButton(strid, GetTextureKey(texture), new Vector2(width, height));
    }

    public static DisposableImGui MenuBar(out bool open) => new DisposableImGui(open = BeginMenuBar(), DisposableTypes.MenuBar);
    public static DisposableImGui Menu(string label, out bool open, bool enabled = true) => new DisposableImGui(open = BeginMenu(label, enabled), DisposableTypes.Menu);


    public struct DisposableImGuiIndent : IDisposable
    {
        float _size;

        public DisposableImGuiIndent(float size = 0f)
        {
            _size = size;
            Indent(size);
        }

        public void Dispose()
        {
            Unindent(_size);
        }
    }

    public struct DisposableImGui : IDisposable
    {
        bool _dispose;
        DisposableTypes _type;

        public DisposableImGui(bool dispose, DisposableTypes type)
        {
            _dispose = dispose;
            _type = type;
        }

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

    public enum DisposableTypes
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

    public static void PlotLines
    (
        string label,
        ref float values,
        int count,
        int offset = 0,
        string overlay = null,
        float valueMin = float.MaxValue,
        float valueMax = float.MaxValue,
        Vector2 size = default,
        int stride = 4)
    {
        ImGui.PlotLines(label, ref values, count, offset, overlay, valueMin, valueMax, size, stride);
    }
}

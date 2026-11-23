# Stride.CommunityToolkit.ImGui

Dear ImGui for Stride through Hexa.NET.ImGui, and a few debug tools built on it: a scene hierarchy, an inspector and a performance monitor.

![](https://user-images.githubusercontent.com/5742236/55237373-563a1400-5232-11e9-8c24-beeaf127c0ac.png)

## How to:

* Add the package to your game's project:
```
dotnet add package Stride.CommunityToolkit.ImGui
```
* Start ImGui once the game is running, for example in the `start` callback of `game.Run`. The system registers itself with the game, so the instance need not be kept:
```cs
using Stride.CommunityToolkit.ImGui;

_ = new ImGuiSystem(game.Services);
```

Builtin Debug interfaces:

```cs
using Stride.CommunityToolkit.ImGui.DebugTools;

_ = new HierarchyView(game.Services);
_ = new PerfMonitor(game.Services);
Inspector.FindFreeInspector(game.Services).Target = objectToInspect;
```

Example interface implementation:

```cs
using Hexa.NET.ImGui;
using Stride.CommunityToolkit.ImGui;
using Stride.Core;

public sealed class YourInterface(IServiceRegistry services) : BaseWindow(services)
{
    private bool _showStats = true;
    private float _speed = 0.25f;

    protected override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.MenuBar;

    protected override void OnDraw(bool collapsed)
    {
        if (collapsed) return;

        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("Reset")) _speed = 0.25f;

                ImGui.EndMenu();
            }

            ImGui.EndMenuBar();
        }

        ImGui.SliderFloat("Speed", ref _speed, 0f, 2f);
        ImGui.Checkbox("Show stats", ref _showStats);

        if (_showStats) ImGui.Text($"Speed {_speed:0.00}");
    }

    protected override void OnDestroy()
    {
    }
}
```

A window is created the same way as the debug tools, and draws every frame from then on:

```cs
_ = new YourInterface(game.Services);
```

`ImGuiExtension` has scopes that end themselves when disposed, for use with `using static Stride.CommunityToolkit.ImGui.ImGuiExtension`: `Child()`, `MenuBar(out open)`, `Menu(label, out open)`, `UCombo`, `UColumns`, `Tooltip(out open)` and `ID`.

```cs
using (Child())
{
    for (var n = 0; n < 50; n++) Text($"{n}: Some text");
}
```

See `examples/code-only/E04_ImGui` for a complete working example.

## Add-ons

### ImNodes

Add the Hexa.NET.ImNodes package
`dotnet add package Hexa.NET.ImNodes`

Create the `ImNodes` context after the `ImGuiSystem` and hand it the ImGui context

```cs
var imGuiSystem = new ImGuiSystem(game.Services);

ImNodes.CreateContext();
ImNodes.SetImGuiContext(imGuiSystem.ImGuiContext);
```

Create ImNodes widgets as needed

```cs
using Hexa.NET.ImNodes;
using Stride.Core;
using Stride.CommunityToolkit.ImGui;
using static Hexa.NET.ImGui.ImGui;
using static Hexa.NET.ImNodes.ImNodes;

namespace StrideVisualScripting.ImGuiWindows;
public class HelloNodesWindow : BaseWindow
{
    private System.Numerics.Vector2 nodePos = new System.Numerics.Vector2(200, 100);
    private System.Numerics.Vector3 Color = System.Numerics.Vector3.Zero;

    private ImNodesEditorContextPtr _context;

    public HelloNodesWindow(IServiceRegistry services) : base(services)
    {
        _context = EditorContextCreate();
    }

    protected override void OnDestroy() { }

    protected override void OnDraw(bool collapsed)
    {
        Text("Hello World!");
        BeginNodeEditor();
        EditorContextSet(_context);
        
        BeginNode(1);
        BeginOutputAttribute(1);
        Text("Attribute Pin");
        EndOutputAttribute();
        EndNode();

        BeginNode(2);
        SetNodeGridSpacePos(2, nodePos);
        BeginInputAttribute(2);
        Text("Attribute Pin");
        EndInputAttribute();

        SetNextItemWidth(100f * Scale);
        ColorPicker3("Test", ref Color);

        EndNode();

        Link(0, 1, 2);

        EndNodeEditor();
        nodePos = GetNodeEditorSpacePos(2);
    }
}
```

## Credits

- [Profan's contribution](https://github.com/profan/dear-xenko)
- [jazzay's contribution](https://github.com/jazzay/Xenko.Extensions#xenkoimgui)
- [Eideren's contribution](https://github.com/Eideren/StrideCommunity.ImGuiDebug)
- [Hexa.NET.ImGui](https://github.com/HexaEngine/Hexa.NET.ImGui)
- [Dear ImGui](https://github.com/ocornut/imgui)
# Stride.CommunityToolkit.ImGuiDebug

Bare-bone implementation of ImGui and a couple of debug tools for Stride

![](https://user-images.githubusercontent.com/5742236/55237373-563a1400-5232-11e9-8c24-beeaf127c0ac.png)

## How to:

* Add this repo as a submodule of your game's repo.
* Add a project reference pointing to this project inside your game's .csproj.
* Reference Hexa.NET.ImGui's nuget package in your game's project, see below.
```xml
<ProjectReference Include="..\Stride.CommunityToolkit.ImGuiDebug.csproj" />
```
* Start ImGui within your game's BeginRun():
```cs
using Stride.CommunityToolkit.ImGuiDebug;
protected override void BeginRun()
{
    base.BeginRun();
    new ImGuiSystem( Services, GraphicsDeviceManager );
}
```

Builtin Debug interfaces:

```cs
new HierarchyView( Services );
new PerfMonitor( Services );
Inspector.FindFreeInspector( Services ).Target = objectToInspect;
```

Example interface implementation:

```cs
using System.Numerics;
using static Hexa.NET.ImGui.ImGui;
using static Stride.CommunityToolkit.ImGuiDebug.ImGuiExtension;

public class YourInterface : Stride.CommunityToolkit.ImGuiDebug.BaseWindow
{
    bool my_tool_active;
    Vector4 my_color;
    float[] my_values = { 0.2f, 0.1f, 1.0f, 0.5f, 0.9f, 2.2f };

    public YourInterface( Stride.Core.IServiceRegistry services ) : base( services )
    {
    }

    protected override void OnDestroy()
    {
    }

    protected override void OnDraw( bool collapsed )
    {
        if( collapsed )
            return;

        if( BeginMenuBar() )
        {
            if( BeginMenu( "File" ) )
            {
                if( MenuItem( "Open..", "Ctrl+O" ) ) { /* Do stuff */ }
                if( MenuItem( "Save", "Ctrl+S" ) ) { /* Do stuff */ }
                if( MenuItem( "Close", "Ctrl+W" ) ) { my_tool_active = false; }
                EndMenu();
            }
            EndMenuBar();
        }

        // Edit a color (stored as ~4 floats)
        ColorEdit4( "Color", ref my_color );

        // Plot some values
        PlotLines( "Frame Times", ref my_values[ 0 ], my_values.Length );

        // Display contents in a scrolling region
        TextColored( new Vector4( 1, 1, 0, 1 ), "Important Stuff" );
        using( Child() )
        {
            for( int n = 0; n < 50; n++ )
                Text( $"{n}: Some text" );
        }
    }
}

```

## Add-ons

### ImNodes

Add the Hexa.NET.ImNodes package
`dotnet add package Hexa.NET.ImNodes`

In your Game class add the `ImNodes` context after the `ImGuiSystem` and add the context to `ImNodes`

```cs
protected override void BeginRun()
{
    base.BeginRun();
    var imGuiSystem = new ImGuiSystem(Services, GraphicsDeviceManager);
    ImNodesContextPtr = ImNodes.CreateContext();
    ImNodes.SetImGuiContext(imGuiSystem.ImGuiContext);
}
```

Create ImNodes widgets as needed

```cs
using Hexa.NET.ImNodes;
using Stride.Core;
using Stride.CommunityToolkit.ImGuiDebug;
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
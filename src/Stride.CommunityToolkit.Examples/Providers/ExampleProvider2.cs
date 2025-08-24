using System.Diagnostics;

namespace Stride.CommunityToolkit.Examples.Providers;

public class ExampleProvider2
{
    private int _index;
    private readonly string _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

    public List<Example> GetExamples() =>
    [
        //CreateExample("Basic Example - Capsule with rigid body",
        //    nameof(Example01_Basic3DScene)),

        //CreateExample("Basic Example - Capsule with rigid body in F#",
        //    nameof(Example01_Basic3DScene_FSharp)),

        //CreateExample("Basic Example - Capsule with rigid body in Visual Basic",
        //    nameof(Example01_Basic3DScene_VBasic)),

        //CreateExample("Basic Example - Mesh Line",
        //    nameof(Example01_Basic3DScene_MeshLine)),

        //CreateExample("Basic Example - Material",
        //    nameof(Example01_Material)),

        //CreateExample("Basic Example - Give me a cube 700",
        //    nameof(Example02_GiveMeACube)),

        //CreateExample("Basic Example - Stride UI - Canvas - Capsule with rigid body and Window", 800
        //    nameof(Example03_StrideUI_CapsuleAndWindow)),

        //CreateExample("Basic Example - Stride UI - Grid - Save and load game state", 900
        //    nameof(Example07_CubeClicker)),

        //CreateExample("Basic Example - Procedural Geometry", 1100
        //    nameof(Example05_ProceduralGeometry)),

        //CreateExample("Basic Example - Cylinder Mesh", 1000
        //    nameof(Example05_CylinderMesh)),

        //CreateExample("Basic Example - Partial Torus",
        //    nameof(Example05_PartialTorus)),

        //CreateExample("Basic Example - Partial Torus in F#", 1200
        //    nameof(Example05_PartialTorus_FSharp)),

        //CreateExample("Basic Example - Particles", 1300
        //    nameof(Example12_Particles)),

        //CreateExample("Basic Example - Raycast", 1400
        //    nameof(Example14_Raycast)),

        //CreateExample("Basic Example - CollisionGroup", 1500
        //    nameof(Example16_CollisionGroup)),

        //CreateExample("Basic Example - CollisionLayer", 1600
        //    nameof(Example16_CollisionLayer)),



        //CreateExample("Advance Example - Simple Constraint",
        //    nameof(Example15_Constraint_Simple)),

        //CreateExample("Advance Example - Various Constraints", 10100
        //    nameof(Example15_Constraint)),

        //CreateExample("Advance Example - Myra UI - Draggable Window, GetService()", 10200
        //    nameof(Example04_MyraUI)),

        //CreateExample("Advance Example - Stride UI - Draggable Window", 13000
        //    nameof(Example10_StrideUI_DragAndDrop)),

        //CreateExample("Advance Example - Image Processing", 15000
        //    nameof(Example06_ImageProcessing)),

        //CreateExample("Advance Example - Root Renderer Shader", 16000
        //    nameof(Example13_RootRendererShader)),

        //CreateExample("Other - Debug Shapes",
        //    nameof(Example08_DebugShapes)),

        //CreateExample("Other - Renderer",
        //    nameof(Example09_Renderer)),

        new Example("Q", "Quit", () => Environment.Exit(0))
    ];

    private Example CreateExample(string title, string projectName)
        => new(GetIndex(), title, () => StartProcess(projectName));

    private string GetIndex() => Interlocked.Increment(ref _index).ToString();

    private void StartProcess(string projectName)
    {
        var exePath = Path.Combine(_baseDirectory, $"{projectName}.exe");
        var workingDirectory = Path.GetDirectoryName(exePath);

        Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        });
    }
}
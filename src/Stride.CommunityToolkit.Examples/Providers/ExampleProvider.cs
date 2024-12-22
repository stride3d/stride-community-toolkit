using System.Diagnostics;

namespace Stride.CommunityToolkit.Examples.Providers;

public class ExampleProvider
{
    private int _index;
    private readonly string _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

    public List<Example> GetExamples() =>
    [
        CreateExample("Basic Example - Capsule with rigid body",
            nameof(Example01_Basic3DScene)),
        CreateExample("Basic Example - Capsule with rigid body in F#",
            nameof(Example01_Basic3DScene_FSharp)),
        CreateExample("Basic Example - Capsule with rigid body in Visual Basic",
            nameof(Example01_Basic3DScene_VBasic)),
        CreateExample("Basic Example - Give me a cube",
            nameof(Example02_GiveMeACube)),
        CreateExample("Basic Example - Stride UI - Canvas - Capsule with rigid body and Window",
            nameof(Example03_StrideUI_CapsuleAndWindow)),
        CreateExample("Basic Example - Stride UI - Grid - Save and load game state",
            nameof(Example07_CubeClicker)),
        CreateExample("Basic Example - Procedural Geometry",
            nameof(Example05_ProceduralGeometry)),
        CreateExample("Advance Example - Myra UI - Draggable Window, GetService()",
            nameof(Example04_MyraUI)),
        CreateExample("Advance Example - Image Processing",
            nameof(Example06_ImageProcessing)),
        CreateExample("Other - CubeClicker",
            nameof(Example07_CubeClicker)),
        CreateExample("Other - Debug Shapes",
            nameof(Example08_DebugShapes)),
        CreateExample("Other - Renderer",
            nameof(Example09_Renderer)),
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
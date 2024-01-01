using System.Diagnostics;

namespace Stride.CommunityToolkit.Examples;

public static class ExampleProvider
{
    private static int _index = 0;

    public static List<Example> GetExamples() => new()
    {
        new Example(GetIndex(), "Basic Example - Capsule with rigid body",
            () => StartProcess(nameof(Example01_Basic3DScene))),

        new Example(GetIndex(), "Basic Example - Capsule with rigid body in F#",
            () => StartProcess(nameof(Example01_Basic3DScene_FSharp))),

        new Example(GetIndex(), "Basic Example - Capsule with rigid body in Visual Basic",
            () => StartProcess(nameof(Example01_Basic3DScene_VBasic))),

        new Example(GetIndex(), "Basic Example - Give me a cube",
            () => StartProcess(nameof(Example02_GiveMeACube))),

        new Example(GetIndex(), "Basic Example - Stride UI - Canvas - Capsule with rigid body and Window",
            () => StartProcess(nameof(Example03_StrideUI_CapsuleAndWindow))),

        new Example(GetIndex(), "Basic Example - Stride UI - Grid - Save and load game state",
            () => StartProcess(nameof(Example07_CubeClicker))),

        new Example(GetIndex(), "Basic Example - Procedural Geometry",
            () => StartProcess(nameof(Example05_ProceduralGeometry))),

        new Example(GetIndex(), "Advance Example - Myra UI - Draggable Window, GetService()",
            () => StartProcess(nameof(Example04_MyraUI))),

        new Example(GetIndex(), "Advance Example - Image Processing",
            () => StartProcess(nameof(Example06_ImageProcessing))),

        new Example("Q", "Quit", () => Environment.Exit(0))
    };

    private static string GetIndex() => Interlocked.Increment(ref _index).ToString();

    private static void StartProcess(string projectName)
    {
        var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{projectName}.exe");
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
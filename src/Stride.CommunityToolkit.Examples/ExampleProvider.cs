using System.Diagnostics;

namespace Stride.CommunityToolkit.Examples;

public static class ExampleProvider
{
    public static List<Example> GetExamples() => new()
    {
        new Example("1", "Basic Example - Capsule with rigid body", () => StartProcess(nameof(Example01_Basic3DScene))),
        new Example("2", "Basic Example - Give me a cube", () => StartProcess(nameof(Example02_GiveMeACube))),
        new Example("3", "Basic Example - Stride UI - Capsule with rigid body and Window", () => StartProcess(nameof(Example03_CapsuleAndWindow))),
        new Example("4", "Basic Example - Myra UI - Draggable Window, GetService()", () => StartProcess(nameof(Example04_MyraUI))),
        new Example("Q", "Quit", () => Environment.Exit(0))
    };

    private static void StartProcess(string projectName) => Process.Start(new ProcessStartInfo
    {
        FileName = $"{projectName}.exe",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true,
    });
}
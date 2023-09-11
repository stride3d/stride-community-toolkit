using Pastel;
using Stride.CommunityToolkit.Examples;
using System.Diagnostics;
using System.Drawing;

var _examples = GetExamples();

DisplayMenu();

while (true)
{
    Console.WriteLine($"Enter choice and press {"ENTER".Pastel(Color.FromArgb(165, 229, 250))} to continue");

    var choice = Console.ReadLine() ?? "";

    var example = _examples.FirstOrDefault(x => string.Equals(x.Id, choice, StringComparison.OrdinalIgnoreCase));

    if (example is null)
    {
        Console.WriteLine("Invalid choice. Try again.".Pastel(Color.Red));
    }
    else
    {
        example.Action();
    }
}

void DisplayMenu()
{
    Console.SetCursorPosition(0, 0);

    Console.WriteLine("Stride Community Toolkit Examples".Pastel(Color.LightBlue));
    Console.WriteLine();

    foreach (var example in _examples)
    {
        Console.WriteLine($"{Navigation($"[{example.Id}]")} {example.Title}");
    }

    Console.WriteLine();
}

static string Navigation(string text) => text.Pastel(Color.LightGreen);

static void StartProcess(string projectName) => Process.Start(new ProcessStartInfo
{
    FileName = $"{projectName}.exe",
    RedirectStandardOutput = true,
    UseShellExecute = false,
    CreateNoWindow = true,
});

static List<Example> GetExamples() => new() {
    new Example("1", "Basic Example - Capsule with rigid body", () => StartProcess(nameof(Example01_Basic3DScene))),
    new Example("2", "Basic Example - Give me a cube", () => StartProcess(nameof(Example02_GiveMeACube))),
    new Example("3", "Basic Example - Capsule with rigid body and window", () => StartProcess(nameof(Example03_CapsuleAndWindow))),
    //new Example("4", "Basic Example - Profiler",
    //                  () => StartProcess(nameof(Example04_Profiler))),
    new Example("Q", "Quit", () => Environment.Exit(0))
};
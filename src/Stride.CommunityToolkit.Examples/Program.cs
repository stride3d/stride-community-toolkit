using Pastel;
using Stride.CommunityToolkit.Examples.Providers;
using System.Drawing;

var examples = new ExampleProvider().GetExamples();

DisplayMenu();

while (true)
{
    HandleUserInput();
}

void DisplayMenu()
{
    Console.SetCursorPosition(0, 0);

    Console.WriteLine("Stride Community Toolkit Examples".Pastel(Color.LightBlue));
    Console.WriteLine();

    foreach (var example in examples)
    {
        Console.WriteLine($"{Navigation($"[{example.Id}]")} {example.Title}");
    }

    Console.WriteLine();
}

void HandleUserInput()
{
    Console.WriteLine($"Enter choice and press {"ENTER".Pastel(Color.FromArgb(165, 229, 250))} to continue");

    var choice = Console.ReadLine() ?? "";

    var example = examples.Find(x => string.Equals(x.Id, choice, StringComparison.OrdinalIgnoreCase));

    if (example is null)
    {
        Console.WriteLine("Invalid choice. Try again.".Pastel(Color.Red));
    }
    else
    {
        example.Action();
    }
}

static string Navigation(string text) => text.Pastel(Color.LightGreen);
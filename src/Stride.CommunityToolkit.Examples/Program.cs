using Pastel;
using Stride.CommunityToolkit.Examples.Core;
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
        Console.WriteLine($"{Navigation($"{(example.Id.PadLeft(2, ' ').Contains(' ') ? " " : "")}[{example.Id}]")} {example.Title}");
    }

    Console.WriteLine();
}

void HandleUserInput()
{
    Console.WriteLine($"Enter example id and press {"ENTER".Pastel(Color.FromArgb(165, 229, 250))} to run it.");
    Console.WriteLine("(Debug output may appear; you can ignore it and type another id at any time.)".Pastel(Color.GreenYellow));
    Console.Write("Choice: ");

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

    Console.WriteLine("It might take a few moments to start the example...");
    Console.WriteLine();

}

static string Navigation(string text) => text.Pastel(Color.LightGreen);
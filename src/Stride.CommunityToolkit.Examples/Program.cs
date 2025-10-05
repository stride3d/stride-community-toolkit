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
    Console.Clear();

    Console.WriteLine("Stride Community Toolkit Examples".Pastel(Color.LightBlue));
    Console.WriteLine();

    var maxIdWidth = examples.Max(e => e.Id.Length);

    foreach (var example in examples)
    {
        var idPadded = example.Id.PadLeft(maxIdWidth);
        var left = Navigation($"[{idPadded}]");

        var categoryLabel = example.Category is { Length: > 0 } category
            ? $" {RemoveFrontNumberAndDashAfter(category)}".Pastel(example.GetColor())
            : string.Empty;

        var right = example.ProjectName is { Length: > 0 } pn
            ? (example.Category is { Length: > 0 } cat2
                ? $" ({pn})".Pastel(ColorHelper.Lighten(example.GetColor(), 0.18f))
                : $" ({pn})".Pastel(Color.LightGray))
            : string.Empty;

        Console.WriteLine($"{left}{categoryLabel} {example.Title}{right}");
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

        if (example.Title == Constants.Clear)
        {
            DisplayMenu();
        }

        if (example.Title != Constants.Quit && example.Title != Constants.Clear)
        {
            Console.WriteLine("It might take a few moments to start the example...");
        }
    }

    Console.WriteLine();
}

static string Navigation(string text) => text.Pastel(Color.LightGreen);

static string RemoveFrontNumberAndDashAfter(string s) =>
    s.IndexOf('-') is int idx && idx > 0
        ? s[(idx + 1)..].Trim()
        : s.Trim();
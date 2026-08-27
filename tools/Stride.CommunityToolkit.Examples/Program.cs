using Pastel;
using Stride.CommunityToolkit.Examples.Core;
using System.Drawing;

List<Example> examples;

try
{
    examples = new ExampleProvider().GetExamples();
}
catch (InvalidOperationException ex)
{
    Console.WriteLine("Could not load the examples manifest.".Pastel(Color.Red));
    Console.WriteLine(ex.Message);

    return 1;
}

DisplayMenu();

while (true)
{
    HandleUserInput();
}

void DisplayMenu()
{
    Constants.SafeClear();

    Console.WriteLine("Stride Community Toolkit Examples".Pastel(Color.LightBlue));
    Console.WriteLine();

    var maxIdWidth = examples.Max(e => e.Id.Length);
    string? currentGroup = null;

    foreach (var example in examples)
    {
        // The manifest is ordered by (language, level), so a change of either starts a new group. The
        // language has to be part of the heading or the F# and VB sections repeat a level name that has
        // already been used, with no indication of why.
        if (example.Entry is { } entry && GroupHeading(entry) != currentGroup)
        {
            currentGroup = GroupHeading(entry);

            Console.WriteLine();
            Console.WriteLine($"  {currentGroup}".Pastel(entry.GetColor()));
        }

        var idPadded = example.Id.PadLeft(maxIdWidth);
        var left = Navigation($"[{idPadded}]");

        Console.WriteLine($"{left} {example.Title}{Suffix(example)}");
    }

    Console.WriteLine();
}

string GroupHeading(ExampleEntry entry) => entry.LanguageLabel.Length > 0
    ? $"{entry.Level.ToUpperInvariant()} - {entry.LanguageLabel}"
    : entry.Level.ToUpperInvariant();

string Suffix(Example example)
{
    if (example.Entry is not { } entry) return string.Empty;

    // The language is already in the group heading, so it is not repeated per line.
    var detail = entry.Category is { Length: > 0 } category ? category : string.Empty;

    var tail = detail.Length > 0 ? $" ({entry.ProjectName} · {detail})" : $" ({entry.ProjectName})";

    return tail.Pastel(ColorHelper.Lighten(entry.GetColor(), 0.18f));
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

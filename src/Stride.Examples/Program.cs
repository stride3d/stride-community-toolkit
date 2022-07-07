using Pastel;
using Stride.Examples.Models;
using System.Drawing;

Console.WriteLine($"{Navigation("[1]")} Basic Example - Capsule with Rigid Body");
Console.WriteLine($"{Navigation("[2]")} Basic Example - Capsule with Rigid Body and Window");
Console.WriteLine($"{Navigation("[3]")} Basic Example with Profiler");
Console.WriteLine();
Console.WriteLine($"{Navigation("[Q]")} Quit");
Console.WriteLine();

while (true)
{
    Console.WriteLine($"Enter choice and press {"ENTER".Pastel(Color.FromArgb(165, 229, 250))} to continue");

    var choice = Console.ReadLine();

    switch (choice)
    {
        case "1": CapsuleExample.Run(); break;
        case "2": CapsuleAndWindowExample.Run(); break;
        case "q": return;
        case "Q": return;
            //case 2: CapsuleExample.Run(); break;
    }
}

string Navigation(string text)
{
    return text.Pastel(Color.LightGreen);
}

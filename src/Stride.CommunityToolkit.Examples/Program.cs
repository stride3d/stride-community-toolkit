using Pastel;
using Stride.Examples.Models;
using System.Drawing;

Console.WriteLine($"{Navigation("[1]")} Basic Example - Give me a cube");
Console.WriteLine($"{Navigation("[2]")} Basic Example - Capsule with rigid body");
Console.WriteLine($"{Navigation("[3]")} Basic Example - Capsule with rigid body and window");
Console.WriteLine($"{Navigation("[4]")} Basic Example - Profiler");
Console.WriteLine();
Console.WriteLine($"{Navigation("[Q]")} Quit");
Console.WriteLine();

while (true)
{
    Console.WriteLine($"Enter choice and press {"ENTER".Pastel(Color.FromArgb(165, 229, 250))} to continue");

    var choice = Console.ReadLine();

    switch (choice)
    {
        case "1": GiveMeACubeExample.Run(); break;
        case "2": CapsuleExample.Run(); break;
        case "3": CapsuleAndWindowExample.Run(); break;
        case "q": return;
        case "Q": return;
    }
}

string Navigation(string text)
{
    return text.Pastel(Color.LightGreen);
}

Console.WriteLine("1 - Basic Example - Capsule with Rigid Body");
Console.WriteLine("2 - Basic Example with Profiler");
Console.WriteLine("Enter choice: ");

if (!int.TryParse(Console.ReadLine(), out var choice)) return;

switch (choice)
{
    case 1: CapsuleExample.Run(); break;
    //case 2: CapsuleExample.Run(); break;
}
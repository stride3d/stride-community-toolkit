namespace E03_2D_HUD;

/// <summary>
/// Names one thing on the HUD: a label, a shape the pointer can find, a value that animates.
/// </summary>
/// <param name="Name">The widget it belongs to.</param>
/// <param name="Part">Which piece of the widget, such as "title" or "hover". Empty for the widget itself.</param>
/// <param name="Index">Which item, where the widget has several. -1 where it has one.</param>
public readonly record struct HudId(string Name, string Part = "", int Index = -1)
{
    public static implicit operator HudId(string name) => new(name);
}
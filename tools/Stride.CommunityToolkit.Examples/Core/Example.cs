using System.Drawing;

namespace Stride.CommunityToolkit.Examples.Core;

/// <summary>
/// One line in the console menu: either an example, or a command such as Quit.
/// </summary>
/// <param name="Id">The number or letter the user types.</param>
/// <param name="Title">The text shown.</param>
/// <param name="Entry">The example behind this line, or <see langword="null"/> for a command.</param>
/// <param name="Action">What running it does.</param>
public record Example(string Id, string Title, ExampleEntry? Entry, Action Action)
{
    /// <summary>Gets the level colour, or grey for a command.</summary>
    public Color GetColor() => Entry?.GetColor() ?? Color.LightGray;

    /// <summary>Gets the project directory name, or <see langword="null"/> for a command.</summary>
    public string? ProjectName => Entry?.ProjectName;

    /// <summary>Gets the teaching level, or <see langword="null"/> for a command.</summary>
    public string? Level => Entry?.Level;
}

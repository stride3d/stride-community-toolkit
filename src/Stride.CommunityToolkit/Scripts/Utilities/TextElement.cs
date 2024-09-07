namespace Stride.CommunityToolkit.Scripts.Utilities;

/// <summary>
/// Represents a text element with optional color information.
/// </summary>
/// <param name="Text">The text content to be displayed.</param>
/// <param name="Color">The optional color for the text. If null, a default color will be used.</param>
public record TextElement(string Text, Color? Color = null);
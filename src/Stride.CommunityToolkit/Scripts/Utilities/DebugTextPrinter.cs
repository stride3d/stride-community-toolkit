namespace Stride.CommunityToolkit.Scripts.Utilities;

/// <summary>
/// A utility class for printing debug text to the screen at various positions.
/// Manages the layout of text, allowing it to be displayed in different areas of the screen and handling multi-line printing.
/// </summary>
public class DebugTextPrinter
{
    private const int LineIncrement = 20;
    private readonly Int2 _basePosition = new(5, 10);
    private Int2 _screenPosition;
    private DisplayPosition _currentPosition = DisplayPosition.TopRight;

    /// <summary>
    /// Gets or sets the screen size, which defines the boundaries for placing text on the screen.
    /// </summary>
    public Int2 ScreenSize { get; init; }

    /// <summary>
    /// Gets or sets the size of the text elements, typically defining the dimensions of each line of text.
    /// </summary>
    public Int2 TextSize { get; init; }

    /// <summary>
    /// Gets or sets the debug text system responsible for rendering text to the screen.
    /// </summary>
    public required Profiling.DebugTextSystem DebugTextSystem { get; init; }

    /// <summary>
    /// Gets or sets the list of text elements (instructions) to be printed on the screen.
    /// </summary>
    public List<TextElement> Instructions { get; init; } = [];

    /// <summary>
    /// Prints all text elements in the <see cref="Instructions"/> list, rendering them line by line on the screen.
    /// </summary>
    public void Print()
    {
        var currentYPosition = _screenPosition.Y;

        foreach (var instruction in Instructions)
        {
            PrintText(instruction.Text, instruction.Color);
        }

        void PrintText(string text, Color? color = null)
        {
            DebugTextSystem.Print(text, new Int2(_screenPosition.X, currentYPosition), color);

            currentYPosition += LineIncrement;
        }
    }

    /// <summary>
    /// Prints the specified list of text elements, rendering them line by line on the screen.
    /// </summary>
    /// <param name="textElements"></param>
    public void Print(List<TextElement> textElements)
    {
        Instructions.Clear();
        Instructions.AddRange(textElements);

        Print();
    }

    /// <summary>
    /// Changes the starting position for printing text, rotating through predefined screen positions (TopRight, BottomRight, BottomLeft, TopLeft).
    /// </summary>
    public void ChangeStartPosition()
    {
        _currentPosition = GetNextPosition(_currentPosition);

        SetStartPosition(_currentPosition);
    }

    /// <summary>
    /// Initializes the screen position by setting the starting position based on the current display position.
    /// </summary>
    public void Initialize() => SetStartPosition(_currentPosition);

    /// <summary>
    /// Initializes the screen position by setting the starting position based on the specified display position.
    /// </summary>
    /// <param name="startPosition"></param>
    public void Initialize(DisplayPosition startPosition) => SetStartPosition(startPosition);

    private static DisplayPosition GetNextPosition(DisplayPosition currentPosition) => currentPosition switch
    {
        DisplayPosition.TopLeft => DisplayPosition.TopRight,
        DisplayPosition.TopRight => DisplayPosition.BottomRight,
        DisplayPosition.BottomRight => DisplayPosition.BottomLeft,
        _ => DisplayPosition.TopLeft,
    };

    private void SetStartPosition(DisplayPosition position)
    {
        _screenPosition = position switch
        {
            DisplayPosition.TopLeft => _basePosition,
            DisplayPosition.BottomLeft => new(_basePosition.X, ScreenSize.Y - TextSize.Y),
            DisplayPosition.BottomRight => new(ScreenSize.X - TextSize.X, ScreenSize.Y - TextSize.Y),
            _ => new(ScreenSize.X - TextSize.X, _basePosition.Y),
        };
    }
}

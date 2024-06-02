namespace Stride.CommunityToolkit.Scripts.Utils;

public class DebugTextPrinter
{
    private const int LineIncrement = 20;
    private readonly Int2 _basePosition = new(5, 10);
    private Int2 _screenPosition;
    private DisplayPosition _currentPosition = DisplayPosition.TopRight;

    public Int2 ScreenSize { get; init; }
    public Int2 TextSize { get; init; }
    public required Profiling.DebugTextSystem DebugTextSystem { get; init; }
    public List<TextElement> Instructions { get; init; } = [];

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

    public void ChangeStartPosition()
    {
        _currentPosition = GetNextPosition(_currentPosition);

        SetStartPosition(_currentPosition);
    }

    private DisplayPosition GetNextPosition(DisplayPosition currentPosition) => currentPosition switch
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

    public void Initialize() => SetStartPosition(_currentPosition);
}

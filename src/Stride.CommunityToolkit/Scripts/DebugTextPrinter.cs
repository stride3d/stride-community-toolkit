namespace Stride.CommunityToolkit.Scripts;

public class DebugTextPrinter
{
    private const int LineIncrement = 20;

    private readonly Profiling.DebugTextSystem _debugTextSystem;
    private Int2 _textSize = new(205, 175);
    private Int2 _basePosition = new(5, 10);
    private Int2 _screenPosition;
    private Int2 _screenSize;
    private DisplayPosition _currentPosition = DisplayPosition.TopRight;

    public DebugTextPrinter(Profiling.DebugTextSystem debugTextSystem, Int2 screenSize)
    {
        _debugTextSystem = debugTextSystem;
        _screenSize = screenSize;

        SetStartPosition(_currentPosition);
    }

    public void Print()
    {
        var currentYPosition = _screenPosition.Y;

        PrintText("CONTROL INSTRUCTIONS");

        PrintText("F2: Toggle Help", Color.Red);
        PrintText("F3: Reposition Help", Color.Red);

        PrintText("WASD: Move");
        PrintText("Arrow Keys: Move");
        PrintText("Q/E: Ascend/Descend");
        PrintText("Hold Shift: Increase speed");
        PrintText("Numpad 2/4/6/8: Rotation");
        PrintText("Right Mouse Button: Rotate");

        void PrintText(string text, Color? color = null)
        {
            _debugTextSystem.Print(text, new Int2(_screenPosition.X, currentYPosition), color);

            currentYPosition += LineIncrement;
        }
    }

    public void ChangeStartPosition()
    {
        _currentPosition = _currentPosition switch
        {
            DisplayPosition.TopLeft => DisplayPosition.TopRight,
            DisplayPosition.TopRight => DisplayPosition.BottomRight,
            DisplayPosition.BottomRight => DisplayPosition.BottomLeft,
            _ => DisplayPosition.TopLeft,
        };

        SetStartPosition(_currentPosition);
    }

    private void SetStartPosition(DisplayPosition position)
    {
        _screenPosition = position switch
        {
            DisplayPosition.TopLeft => _basePosition,
            DisplayPosition.BottomLeft => new(_basePosition.X, _screenSize.Y - _textSize.Y),
            DisplayPosition.BottomRight => new(_screenSize.X - _textSize.X, _screenSize.Y - _textSize.Y),
            _ => new(_screenSize.X - _textSize.X, _basePosition.Y),
        };
    }
}

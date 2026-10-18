using Myra.Graphics2D.UI;

namespace E04_Myra_DraggableWindow;

/// <summary>
/// The main view: a health bar and a draggable test window.
/// </summary>
public class MainView : Panel
{
    /// <summary>The health bar.</summary>
    public HorizontalProgressBar HealthBar { get; private set; } = null!;

    /// <summary>The draggable test window, with a sample label.</summary>
    public Window ExampleWindow { get; private set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainView"/> class.
    /// </summary>
    public MainView()
    {
        InitializeHealthBar();
        InitializeTestWindow();
    }

    /// <summary>
    /// Initializes the health bar UI element.
    /// </summary>
    private void InitializeHealthBar() => Widgets.Add(UIUtils.CreateHealthBar(-20, "#4BD961FF"));

    /// <summary>
    /// Initializes the test window UI element.
    /// </summary>
    private void InitializeTestWindow()
    {
        var label = new Label
        {
            VerticalSpacing = 10,
            Text = "This is a Test! Hello from Myra! This is a draggable window and below two progress bars."
        };

        ExampleWindow = new Window
        {
            Title = "Hello From Myra",
            Left = 590,
            Top = 200,
            Content = label
        };

        Widgets.Add(ExampleWindow);
    }
}
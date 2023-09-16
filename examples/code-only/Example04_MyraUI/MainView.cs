using Myra.Graphics2D.UI;

namespace Example04_MyraUI;

/// <summary>
/// Represents the main user interface view for the application.
/// </summary>
/// <remarks>
/// The MainView class is responsible for creating and managing key UI elements such as a health bar and a test window.
/// </remarks>
public class MainView : Panel
{
    /// <summary>
    /// Gets the health bar UI element.
    /// </summary>
    /// <remarks>
    /// The health bar shows the current health status.
    /// </remarks>
    public HorizontalProgressBar HealthBar { get; private set; } = null!;

    /// <summary>
    /// Gets the test window UI element.
    /// </summary>
    /// <remarks>
    /// The example window is used for demo purposes and contains a sample label.
    /// </remarks>
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
            Text = "This is a Test! Hello from Myra! This is a window and below two progress bars."
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
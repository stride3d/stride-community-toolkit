using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace Example04_MyraUI;
public class MainView : Panel
{
    public HorizontalProgressBar HealthBar;
    public Window TestWindow;

    public MainView()
    {
        // This can be configured to show health as a progress bar
        // It isnt configured because Im lazy right now but Ill set this up later to show how to do it
        HealthBar = new()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Value = 100,
            Filler = new SolidBrush("#4BD961FF"),
            Left = 20,
            Top = -20,
            Width = 300,
            Height = 20,
            Background = new SolidBrush("#202020FF"),
        };

        // Just a simple readonly text field
        var label = new Label
        {
            VerticalSpacing = 10,
            Text = "This is a Test! Hello from Myra!"
        };

        // This is a window that can be moved around and closed and shows the label as Content
        TestWindow = new Window()
        {
            Title = "Hello World!",
            Left = 590,
            Top = 200,
            Content = label,
        };

        // Add the window and the health bar to the main view
        Widgets.Add(TestWindow);
        Widgets.Add(HealthBar);
    }
}

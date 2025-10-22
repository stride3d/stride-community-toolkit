using Avalonia;

namespace Stride.CommunityToolkit.Examples.Launcher;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args) =>
  BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
 AppBuilder.Configure<App>()
.UsePlatformDetect()
          .LogToTrace();
}

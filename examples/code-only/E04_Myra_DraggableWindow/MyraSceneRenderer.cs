using Myra;
using Myra.Graphics2D.UI;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace E04_Myra_DraggableWindow;

/// <summary>
/// Draws a Myra user interface as a scene renderer in Stride's rendering pipeline.
/// </summary>
public class MyraSceneRenderer : SceneRendererBase
{
    /// <summary>Myra's root container, which every element hangs off.</summary>
    private Desktop? _desktop;

    /// <summary>Sets up Myra, the main view and the desktop that holds it.</summary>
    protected override void InitializeCore()
    {
        base.InitializeCore();

        // Myra needs the concrete Game, and it has to exist by the time the renderer initializes:
        // a null here would surface much later as a NullReferenceException inside Myra itself.
        MyraEnvironment.Game = Services.GetService<IGame>() as Game
            ?? throw new InvalidOperationException("Myra needs the running Stride Game to be registered in Services.");

        // The view goes into the services, which is how Program.cs finds it to add a widget later
        var mainView = new MainView();

        Services.AddService(mainView);

        _desktop = new Desktop { Root = mainView };
    }

    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        drawContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);

        _desktop?.Render();
    }
}
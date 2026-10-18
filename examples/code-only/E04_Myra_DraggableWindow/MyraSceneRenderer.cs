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

    /// <summary>The view with the elements the user interacts with.</summary>
    private MainView? _mainView;

    /// <summary>Sets up Myra, the main view and the desktop that holds it.</summary>
    protected override void InitializeCore()
    {
        base.InitializeCore();

        // Myra needs the concrete Game, and it has to exist by the time the renderer initializes:
        // a null here would surface much later as a NullReferenceException inside Myra itself.
        MyraEnvironment.Game = Services.GetService<IGame>() as Game
            ?? throw new InvalidOperationException("Myra needs the running Stride Game to be registered in Services.");

        InitializeMainView();

        InitializeDesktop();
    }

    /// <summary>
    /// Initializes the main view and adds it to the Stride services.
    /// </summary>
    private void InitializeMainView()
    {
        _mainView = new MainView();

        Services.AddService(_mainView);
    }

    /// <summary>
    /// Initializes the desktop and sets the root view.
    /// </summary>
    private void InitializeDesktop()
    {
        _desktop = new Desktop
        {
            Root = _mainView
        };
    }

    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        drawContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);

        _desktop?.Render();
    }
}
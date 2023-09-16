using Myra;
using Myra.Graphics2D.UI;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Example04_MyraUI;

/// <summary>
/// Provides functionality for rendering Myra-based user interfaces in a Stride game.
/// </summary>
/// <remarks>
/// This renderer uses the Myra UI library to render the user interface and integrates it into the Stride rendering pipeline.
/// </remarks>
public class MyraSceneRenderer : SceneRendererBase
{
    /// <summary>
    /// Gets or sets the root of all UI elements to be rendered.
    /// </summary>
    /// <remarks>
    /// The desktop serves as the root container for all UI elements rendered by Myra.
    /// </remarks>
    private Desktop? _desktop;

    /// <summary>
    /// Gets or sets the main view of the application UI.
    /// </summary>
    /// <remarks>
    /// The main view contains the primary UI elements that the user will interact with.
    /// </remarks>
    private MainView? _mainView;

    /// <summary>
    /// Initializes the core rendering properties.
    /// </summary>
    /// <remarks>
    /// This method sets up the Myra environment, configures the main view, and associates it with the desktop.
    /// </remarks>
    protected override void InitializeCore()
    {
        base.InitializeCore();

        MyraEnvironment.Game = (Game)Services.GetService<IGame>();

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
        // Clear depth buffer
        drawContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);

        // Render UI
        _desktop?.Render();
    }
}
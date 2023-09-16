using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering.Compositing;
using Stride.Rendering;
using Myra;
using Myra.Graphics2D.UI;

namespace Example04_MyraUI;
public class MyraRenderer : SceneRendererBase
{
    // The default UI I am using but make your own or make this configurable
    private MainView _mainView;
    // The root of all rendered UI
    private Desktop _desktop;

    protected override void InitializeCore()
    {
        base.InitializeCore();

        MyraEnvironment.Game = (Game)Services.GetService<IGame>();

        _mainView = new MainView();

        // I add this to the Stride Service so that any Script can retrieve it from Strides Services
        // Game.Services.GetService<MainView>() is used to retrieve it
        Services.AddService(_mainView);

        // This is how you change the root rendered view and should be usable at runtime also!
        _desktop = new()
        {
            Root = _mainView,
        };
    }

    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        // Clear depth buffer
        drawContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);

        // Render UI
        _desktop.Render();
    }
}
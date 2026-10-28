using Myra;
using Myra.Graphics2D.UI;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace E04_Myra_ControlPanel;

/// <summary>
/// Draws a Myra user interface as a scene renderer in Stride's rendering pipeline, and hands the desktop to <see cref="Build"/> once Myra is ready for widgets.
/// </summary>
/// <remarks>
/// Myra widgets read the default stylesheet when they are made, which needs the graphics device, so nothing can be built before the renderer initialises. The callback is the one place to build.
/// </remarks>
public class MyraSceneRenderer : SceneRendererBase
{
    private Desktop? _desktop;

    /// <summary>Called once with the desktop, after Myra is set up: build the widgets here.</summary>
    public Action<Desktop>? Build { get; set; }

    protected override void InitializeCore()
    {
        base.InitializeCore();

        // Myra needs the concrete Game, and it has to exist by the time the renderer initializes:
        // a null here would surface much later as a NullReferenceException inside Myra itself.
        MyraEnvironment.Game = Services.GetService<IGame>() as Game
            ?? throw new InvalidOperationException("Myra needs the running Stride Game to be registered in Services.");

        _desktop = new Desktop();

        Build?.Invoke(_desktop);
    }

    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        drawContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
        _desktop?.Render();
    }
}
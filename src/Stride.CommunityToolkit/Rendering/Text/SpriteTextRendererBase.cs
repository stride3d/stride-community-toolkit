using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// The frame the sprite-text renderers share: a <see cref="Graphics.SpriteBatch"/>, the default font, the one-pixel
/// background texture and the display's scale, created once, and the checks every draw starts with.
/// </summary>
/// <remarks>
/// <see cref="EntityTextRenderer"/>, <see cref="WorldTextRenderer"/> and <see cref="Overlay.EntityInfoRenderer"/> derive
/// from it and keep only their own <see cref="SceneRendererBase.DrawCore"/>; the drawing itself is shared through
/// <see cref="ScreenTextDrawer"/>.
/// </remarks>
public abstract class SpriteTextRendererBase : SceneRendererBase
{
    private DisplayScale? _displayScale;

    /// <summary>
    /// What a frame is drawn with: the batch, the default font, the camera the compositor renders with and the
    /// viewport's size in pixels.
    /// </summary>
    /// <param name="Batch">The renderer's sprite batch, not yet begun.</param>
    /// <param name="DefaultFont">The font for text that names none.</param>
    /// <param name="Camera">The camera of the compositor's slot.</param>
    /// <param name="ScreenSize">The viewport's width and height in pixels.</param>
    protected readonly record struct Frame(SpriteBatch Batch, SpriteFont DefaultFont, CameraComponent Camera, Vector2 ScreenSize);

    /// <summary>The batch the text is drawn with; <see langword="null"/> until the renderer has initialised.</summary>
    protected SpriteBatch? SpriteBatch { get; private set; }

    /// <summary>
    /// The font for text that names none; <see langword="null"/> until the renderer has initialised, or when neither the
    /// engine's font nor a system font could be loaded.
    /// </summary>
    protected SpriteFont? DefaultFont { get; private set; }

    /// <summary>A one-pixel white texture, tinted when drawn, for the strip behind a text.</summary>
    protected Texture? BackgroundTexture { get; private set; }

    /// <summary>
    /// The size the default font is loaded at, in pixels, when it is a system font; every draw rasterises at its own size
    /// anyway. 32 unless a renderer says otherwise.
    /// </summary>
    protected virtual float DefaultFontSize => 32f;

    /// <inheritdoc />
    protected override void InitializeCore()
    {
        base.InitializeCore();

        SpriteBatch = new SpriteBatch(GraphicsDevice);
        DefaultFont = RendererDefaults.LoadDefaultFont(Content, Services, DefaultFontSize);
        BackgroundTexture = ScreenTextDrawer.CreateBackgroundTexture(GraphicsDevice);

        // The display's scale, for sizes that follow it. Absent outside a game, which leaves every size
        // at exactly the pixels asked for
        if (Services.GetService<IGame>() is { } game)
            _displayScale = DisplayScale.GetOrCreate(game);
    }

    /// <summary>
    /// The factor a pixel figure is multiplied by: the display's scale when <paramref name="autoScale"/> asks for it and
    /// there is a display, otherwise 1.
    /// </summary>
    /// <param name="autoScale">Whether the figure follows the display's scale.</param>
    /// <returns>The display's scale, or 1.</returns>
    protected float DisplayScaleFactor(bool autoScale) => autoScale && _displayScale is not null ? _displayScale.Value : 1f;

    /// <summary>
    /// The checks every draw starts with: the batch and the font exist, the compositor has a camera and the viewport has
    /// a size. Resolved per frame rather than cached, so a change of scene or camera is picked up.
    /// </summary>
    /// <param name="context">The render context.</param>
    /// <param name="drawContext">The draw context, whose command list has the viewport.</param>
    /// <param name="frame">What to draw with, when the method returns <see langword="true"/>.</param>
    /// <returns><see langword="false"/> when there is nothing to draw with or into.</returns>
    protected bool TryBeginFrame(RenderContext context, RenderDrawContext drawContext, out Frame frame)
    {
        frame = default;

        if (SpriteBatch is not { } batch || DefaultFont is not { } font) return false;

        if (CompositorCameras.Find(context) is not { } camera) return false;

        var viewport = drawContext.CommandList.Viewport;
        var screenSize = new Vector2(viewport.Width, viewport.Height);

        if (screenSize.X <= 0 || screenSize.Y <= 0) return false;

        frame = new Frame(batch, font, camera, screenSize);

        return true;
    }

    /// <inheritdoc />
    protected override void Destroy()
    {
        base.Destroy();

        SpriteBatch?.Dispose();
        BackgroundTexture?.Dispose();
    }
}
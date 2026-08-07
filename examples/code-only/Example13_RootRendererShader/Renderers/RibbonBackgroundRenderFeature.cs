using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;

namespace Example13_RootRendererShader.Renderers;

public class RibbonBackgroundRenderFeature : RootRenderFeature
{
    private SpriteBatch? _spriteBatch;
    private DynamicEffectInstance? _background2DEffect;
    private Texture? _texture;

    /// <summary>
    /// Size of the placeholder texture handed to <see cref="SpriteBatch"/>. Only the 16:9 ratio is
    /// meaningful - see <see cref="InitializeCore"/>.
    /// </summary>
    private const int PlaceholderWidth = 16;
    private const int PlaceholderHeight = 9;

    public override Type SupportedRenderObjectType => typeof(RibbonRenderBackground);

    public RibbonBackgroundRenderFeature()
    {
        // Background should render after most objects (to take advantage of early z depth test)
        SortKey = 192;
    }

    public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
    {
        if (_texture is null || _spriteBatch is null || _background2DEffect is null) return;

        for (int index = startIndex; index < endIndex; index++)
        {
            var renderNodeReference = renderViewStage.SortedRenderNodes[index].RenderNode;
            var renderNode = GetRenderNode(renderNodeReference);
            var renderBackground = (RibbonRenderBackground)renderNode.RenderObject;

            Draw2D(context, renderBackground, _texture, _spriteBatch, _background2DEffect);
        }
    }

    protected override void InitializeCore()
    {
        // SpriteBatch needs a texture to size the quad and to derive texture coordinates from, but
        // RibbonBackgroundShader overrides Shading() and never samples it - the ribbons are generated
        // procedurally from the UVs. Only the texture's aspect ratio matters, because Draw2D uses it
        // to crop the source rectangle so the pattern is not stretched. A tiny 16:9 placeholder
        // produces identical UVs to a full 1920x1080 surface, without the memory or the mip chain.
        var textureDescription = TextureDescription.New2D(
            PlaceholderWidth,
            PlaceholderHeight,
            PixelFormat.R8G8B8A8_UNorm_SRgb,
            TextureFlags.ShaderResource);

        _texture = Texture.New(Context.GraphicsDevice, textureDescription);

        // load shader
        _background2DEffect = new DynamicEffectInstance("RibbonBackgroundShader");
        _background2DEffect.Initialize(Context.Services);

        _spriteBatch = new SpriteBatch(RenderSystem.GraphicsDevice) { VirtualResolution = new Vector3(1) };
    }

    /// <inheritdoc/>
    protected override void Destroy()
    {
        _spriteBatch?.Dispose();
        _spriteBatch = null;

        _background2DEffect?.Dispose();
        _background2DEffect = null;

        _texture?.Dispose();
        _texture = null;

        base.Destroy();
    }

    private static void Draw2D([NotNull] RenderDrawContext context, [NotNull] RibbonRenderBackground renderBackground,
        Texture texture, SpriteBatch spriteBatch, DynamicEffectInstance effect)
    {
        var target = context.CommandList.RenderTarget;
        var graphicsDevice = context.GraphicsDevice;

        // The quad always covers the whole screen (VirtualResolution is 1x1)
        var destination = new RectangleF(0, 0, 1, 1);

        // Crop the source rectangle to the render target's aspect ratio, so the generated pattern
        // keeps its proportions instead of being stretched to the window. This is the only reason
        // the placeholder texture's dimensions matter - its pixels are never sampled.
        var textureSize = new Size3(texture.ViewWidth, texture.ViewHeight, texture.ViewDepth);
        var imageBufferMinRatio = Math.Min(textureSize.Width / (float)target.ViewWidth, textureSize.Height / (float)target.ViewHeight);
        var sourceSize = new Vector2(target.ViewWidth * imageBufferMinRatio, target.ViewHeight * imageBufferMinRatio);
        var source = new RectangleF((textureSize.Width - sourceSize.X) / 2, (textureSize.Height - sourceSize.Y) / 2, sourceSize.X, sourceSize.Y);

        effect.UpdateEffect(graphicsDevice);

        spriteBatch.Begin(context.GraphicsContext, SpriteSortMode.FrontToBack, BlendStates.Opaque,
            graphicsDevice.SamplerStates.LinearClamp, DepthStencilStates.DepthRead, null, effect);

        spriteBatch.Parameters.Set(RibbonBackgroundShaderKeys.DeltaTime, (float)context.RenderContext.Time.Total.TotalSeconds);
        spriteBatch.Parameters.Set(RibbonBackgroundShaderKeys.Intensity, renderBackground.Intensity);
        spriteBatch.Parameters.Set(RibbonBackgroundShaderKeys.Frequency, renderBackground.Frequency);
        spriteBatch.Parameters.Set(RibbonBackgroundShaderKeys.Amplitude, renderBackground.Amplitude);
        spriteBatch.Parameters.Set(RibbonBackgroundShaderKeys.Speed, renderBackground.Speed);
        spriteBatch.Parameters.Set(RibbonBackgroundShaderKeys.Top, renderBackground.Top);
        spriteBatch.Parameters.Set(RibbonBackgroundShaderKeys.Bottom, renderBackground.Bottom);
        spriteBatch.Parameters.Set(RibbonBackgroundShaderKeys.WidthFactor, renderBackground.WidthFactor);
        spriteBatch.Draw(texture, destination, source, Color.White, 0, Vector2.Zero, layerDepth: -0.5f);
        spriteBatch.End();
    }
}
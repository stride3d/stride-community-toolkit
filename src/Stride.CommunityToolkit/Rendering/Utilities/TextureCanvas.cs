using Stride.CommunityToolkit.Collections;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Images;

namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// Provides functionality for drawing and manipulating textures on a canvas.
/// This class supports various operations such as drawing rectangles, drawing textures,
/// applying filters, transforming colors, and resampling textures. It is designed to work
/// with the Stride graphics framework and provides methods for loading and storing texture data.
/// </summary>
public class TextureCanvas : IDisposable
{
    private PixelFormat _pixelFormat;
    private Texture? _primaryBuffer;
    private Texture? _secondaryBuffer;
    private Size2 _size;

    private RenderContext RenderContext { get; }
    private RenderDrawContext RenderDrawContext => RenderContext.GetThreadContext();
    private GraphicsContext GraphicsContext => RenderDrawContext.GraphicsContext;
    private GraphicsDevice GraphicsDevice => RenderContext.GraphicsDevice;
    private CommandList CommandList => GraphicsContext.CommandList;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureCanvas"/> class.
    /// </summary>
    public TextureCanvas(RenderContext renderContext, Size2? size, PixelFormat pixelFormat = PixelFormat.R8G8B8A8_UNorm)
    {
        RenderContext = renderContext;
        _size = size ?? new Size2(1024, 1024);
        _pixelFormat = pixelFormat;
    }

    /// <summary>
    ///     Gets or sets the current pixel format
    /// </summary>
    /// <remarks>
    ///     Setting a different pixel format resamples what has been drawn so far; before the first draw it only
    ///     changes what the first draw creates.<br />
    ///     You may consider using <see cref="Resample" /> if you also want to change the size in a single operation.
    /// </remarks>
    public PixelFormat PixelFormat
    {
        get => _pixelFormat;
        set => Resample(_size, value);
    }

    /// <summary>
    ///     Gets or sets the current texture size
    /// </summary>
    /// <remarks>
    ///     Setting a different size resamples what has been drawn so far; before the first draw it only changes
    ///     what the first draw creates.<br />
    ///     You may consider using <see cref="Resample" /> if you also want to change the pixel format in a single operation.
    /// </remarks>
    public Size2 Size
    {
        get => _size;
        set => Resample(value, _pixelFormat);
    }

    /// <summary>
    ///     Calculates the texture size in bytes
    /// </summary>
    public int ByteSize => EnsurePrimary().CalculatePixelDataCount<byte>();

    /// <summary>
    /// Releases all resources used by the <see cref="TextureCanvas"/> object.
    /// </summary>
    public void Dispose()
    {
        Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Draws a texture to the drawing context
    /// </summary>
    /// <param name="sourceTexture">The source texture to draw</param>
    /// <param name="colorMultiplier">The color multiplier. Default is <see cref="Stride.Core.Mathematics.Color.White"/></param>
    /// <param name="stretch">The stretch mode</param>
    /// <param name="anchor">The anchor mode</param>
    /// <param name="samplingPattern">The sampling pattern</param>
    public void DrawTexture(
        Texture sourceTexture,
        Color4? colorMultiplier = null,
        Stretch stretch = Stretch.Stretch,
        Anchor anchor = Anchor.TopLeft,
        SamplingPattern samplingPattern = SamplingPattern.Linear
    )
    {
        var sourceRect = new Rectangle(0, 0, sourceTexture.Width, sourceTexture.Height);
        var destinationRect = new Rectangle(0, 0, _size.Width, _size.Height);
        DrawTexture(sourceTexture, sourceRect, destinationRect, colorMultiplier, stretch, anchor, samplingPattern);
    }

    /// <summary>
    ///     Draws a texture to the <see cref="TextureCanvas" />
    /// </summary>
    /// <param name="sourceTexture">The source texture to draw</param>
    /// <param name="destinationRect">The sub rectangle of the target texture, in pixels</param>
    /// <param name="colorMultiplier">The color multiplier. Default is <see cref="Stride.Core.Mathematics.Color.White"/></param>
    /// <param name="stretch">The stretch mode</param>
    /// <param name="anchor">The anchor mode</param>
    /// <param name="samplingPattern">The sampling pattern</param>
    public void DrawTexture(
        Texture sourceTexture,
        Rectangle destinationRect,
        Color4? colorMultiplier = null,
        Stretch stretch = Stretch.Stretch,
        Anchor anchor = Anchor.TopLeft,
        SamplingPattern samplingPattern = SamplingPattern.Linear
    )
    {
        var sourceRect = new Rectangle(0, 0, sourceTexture.Width, sourceTexture.Height);
        DrawTexture(sourceTexture, sourceRect, destinationRect, colorMultiplier, stretch, anchor, samplingPattern);
    }

    /// <summary>
    ///     Draws a texture to the <see cref="TextureCanvas" /> using a rectangle with percentage values
    /// </summary>
    /// <param name="sourceTexture">The source texture to draw</param>
    /// <param name="relativeDestinationRect">The sub rectangle of the target texture in percentages</param>
    /// <param name="colorMultiplier">The color multiplier. Default is <see cref="Stride.Core.Mathematics.Color.White"/></param>
    /// <param name="stretch">The stretch mode</param>
    /// <param name="anchor">The anchor mode</param>
    /// <param name="samplingPattern">The sampling pattern</param>
    public void DrawTexture(
        Texture sourceTexture,
        RectangleF relativeDestinationRect,
        Color4? colorMultiplier = null,
        Stretch stretch = Stretch.Stretch,
        Anchor anchor = Anchor.TopLeft,
        SamplingPattern samplingPattern = SamplingPattern.Linear
    )
    {
        var sourceRect = new Rectangle(0, 0, sourceTexture.Width, sourceTexture.Height);
        DrawTexture(sourceTexture, sourceRect, ToAbsoluteSize(_size, relativeDestinationRect), colorMultiplier, stretch, anchor, samplingPattern);
    }

    /// <summary>
    ///     Draws a texture to the <see cref="TextureCanvas" />
    /// </summary>
    /// <param name="sourceTexture">The source texture to draw</param>
    /// <param name="sourceRect">The sub rectangle of the source texture to draw</param>
    /// <param name="destinationRect">The sub rectangle of the target texture</param>
    /// <param name="colorMultiplier">The color multiplier. Default is <see cref="Stride.Core.Mathematics.Color.White"/></param>
    /// <param name="stretch">The stretch mode</param>
    /// <param name="anchor">The anchor mode</param>
    /// <param name="samplingPattern">The sampling pattern</param>
    public void DrawTexture(Texture sourceTexture, Rectangle sourceRect, Rectangle destinationRect, Color4? colorMultiplier = null,
        Stretch stretch = Stretch.Stretch, Anchor anchor = Anchor.TopLeft,
        SamplingPattern samplingPattern = SamplingPattern.Linear)
    {
        var bufferTexture = EnsurePrimary();
        var (viewport, scissor) = GetViewportAndScissor(sourceTexture, sourceRect, destinationRect, stretch, anchor);
        using var scaler = new ImageScaler(samplingPattern);

        scaler.RasterizerState = new RasterizerStateDescription(CullMode.Back) { ScissorTestEnable = true };
        scaler.Color = colorMultiplier.GetValueOrDefault(Color4.White);
        scaler.SetViewport(viewport);
        scaler.SetScissorRectangle(scissor);
        scaler.SetInput(sourceTexture);
        scaler.SetOutput(bufferTexture);

        scaler.Draw(RenderDrawContext);
    }

    /// <summary>
    ///     Draws a texture to the <see cref="TextureCanvas" /> using rectangles with percentage values
    /// </summary>
    /// <param name="sourceTexture">The source texture to draw</param>
    /// <param name="relativeSourceRect">The sub rectangle of the source texture to draw in percentages</param>
    /// <param name="relativeDestinationRect">The sub rectangle of the target texture in percentages</param>
    /// <param name="colorMultiplier">The color multiplier. Default is <see cref="Stride.Core.Mathematics.Color.White"/></param>
    /// <param name="stretch">The stretch mode</param>
    /// <param name="anchor">The anchor mode</param>
    /// <param name="samplingPattern">The sampling pattern</param>
    public void DrawTexture(
        Texture sourceTexture,
        RectangleF relativeSourceRect,
        RectangleF relativeDestinationRect,
        Color4? colorMultiplier = null,
        Stretch stretch = Stretch.Stretch,
        Anchor anchor = Anchor.TopLeft,
        SamplingPattern samplingPattern = SamplingPattern.Linear
    )
    {
        DrawTexture(sourceTexture, ToAbsoluteSize(sourceTexture.Size, relativeSourceRect), ToAbsoluteSize(_size, relativeDestinationRect), colorMultiplier, stretch, anchor, samplingPattern);
    }

    /// <summary>
    ///     Replaces the current <see cref="TextureCanvas" /> with a texture loaded from the local file system
    /// </summary>
    /// <param name="path">The file path</param>
    /// <remarks>
    ///     This method also replaces the current size and pixel format. Use <see cref="Resample" /> to change it
    ///     afterwards.
    /// </remarks>
    public void Load(string path)
    {
        using var fileStream = File.OpenRead(path);
        Load(fileStream);
    }

    /// <summary>
    ///     Replaces the current <see cref="TextureCanvas" /> with a texture loaded from a <see cref="T:byte[]" />
    /// </summary>
    /// <param name="data">The <see cref="T:byte[]" /> containing file data</param>
    /// <remarks>
    ///     This method also replaces the current size and pixel format. Use <see cref="Resample" /> to change it
    ///     afterwards.
    /// </remarks>
    public void Load(byte[] data)
    {
        using var stream = new MemoryStream(data);

        Load(stream);
    }

    /// <summary>
    ///     Replaces the current <see cref="TextureCanvas" /> with a texture loaded from a <see cref="Stream" />
    /// </summary>
    /// <param name="stream">The <see cref="Stream" /> containing file data</param>
    /// <remarks>
    ///     This method also replaces the current size and pixel format. Use <see cref="Resample" /> to change it
    ///     afterwards.
    /// </remarks>
    public void Load(Stream stream)
    {
        _primaryBuffer?.Dispose();
        _primaryBuffer = Texture.Load(GraphicsDevice, stream, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
        _size = new Size2(_primaryBuffer.Size.Width, _primaryBuffer.Size.Height);
        _pixelFormat = _primaryBuffer.Format;
    }

    /// <summary>
    ///     Stores the current texture to a file in the local file system.
    /// </summary>
    /// <param name="path">The target file path</param>
    /// <param name="fileType">The file type to write or null for automatic inference based on the file extension</param>
    public void Store(string path, ImageFileType? fileType = null)
    {
        using var stream = File.Open(path, FileMode.Create);
        fileType ??= Path.GetExtension(path) switch
        {
            ".stride" => ImageFileType.Stride,
            ".dds" => ImageFileType.Dds,
            ".png" => ImageFileType.Png,
            ".gif" => ImageFileType.Gif,
            ".jpg" or ".jpeg" => ImageFileType.Jpg,
            ".bmp" => ImageFileType.Bmp,
            ".tiff" => ImageFileType.Tiff,
            ".tga" => ImageFileType.Tga,
            _ => throw new NotSupportedException($"'{Path.GetExtension(path)}' is not an image format the canvas can write. Use .png, .jpg, .bmp, .gif, .tiff, .tga, .dds or .stride, or pass the fileType.")
        };
        Store(stream, fileType.Value);
    }

    /// <summary>
    ///     Stores the current texture to a <see cref="Stream" /> in the selected file format
    /// </summary>
    /// <param name="stream">The target stream</param>
    /// <param name="fileType">The file type to write</param>
    public void Store(Stream stream, ImageFileType fileType) => EnsurePrimary().Save(CommandList, stream, fileType);

    /// <summary>
    ///     Replaces the current <see cref="TextureCanvas" /> with a texture loaded from a <see cref="T:byte[]" /> containing
    ///     pixel data
    /// </summary>
    /// <param name="data">The <see cref="T:byte[]" /> containing pixel data</param>
    /// <param name="width">The width of the new texture</param>
    /// <param name="height">The height of the new texture</param>
    /// <param name="pixelFormat">The pixel format of the new texture</param>
    /// <remarks>
    ///     This method also replaces the current size and pixel format. Use <see cref="Resample" /> to change it
    ///     afterwards.
    /// </remarks>
    public void SetData(byte[] data, int width, int height, PixelFormat pixelFormat)
    {
        _primaryBuffer?.Dispose();
        _primaryBuffer = Texture.New2D(GraphicsDevice, width, height, pixelFormat, data,
            TextureFlags.ShaderResource | TextureFlags.RenderTarget);
        _size = new Size2(_primaryBuffer.Size.Width, _primaryBuffer.Size.Height);
        _pixelFormat = _primaryBuffer.Format;
    }

    /// <summary>
    ///     Copies the current texture data to a byte array
    /// </summary>
    /// <param name="data">The data array to store the data</param>
    public void GetData(byte[] data) => EnsurePrimary().GetData(CommandList, data);

    /// <summary>
    ///     Copies the current texture data to a byte array
    /// </summary>
    public byte[] GetData() => EnsurePrimary().GetData<byte>(CommandList);

    /// <summary>
    ///     Copies the current texture data to a new texture
    /// </summary>
    public Texture ToTexture(TextureFlags flags = TextureFlags.ShaderResource)
        => Texture.New2D(GraphicsDevice, _size.Width, _size.Height, PixelFormat, GetData(), flags);

    /// <summary>
    ///     Applies a <see cref="Stride.Rendering.Images.BrightFilter" /> effect.
    /// </summary>
    /// <param name="threshold">The threshold relative to the white point</param>
    /// <param name="steepness">the smooth-step steepness for bright pass filtering.</param>
    /// <param name="colorMultiplier">
    ///     Modulates bright areas with the provided color. It affects the color of sub-sequent bloom,
    ///     light-streak effects
    /// </param>
    public void BrightFilter(float threshold = 0.2f, float steepness = 1.0f, Color3? colorMultiplier = null)
    {
        using var effect = new BrightFilter();
        effect.Steepness = steepness;
        effect.Threshold = threshold;
        effect.Color = colorMultiplier.GetValueOrDefault(new Color3(1f));

        Apply(effect);
    }

    /// <summary>
    ///     Applies a <see cref="Stride.Rendering.Images.GaussianBlur" /> effect.
    /// </summary>
    /// <param name="radius">The radius.</param>
    /// <param name="sigmaRatio">
    ///     The sigma ratio. The sigma ratio is used to calculate the sigma based on the radius: The
    ///     actual formula is sigma = radius / SigmaRatio. The default value is 2.0f.
    /// </param>
    public void GaussianBlur(int radius = 4, float sigmaRatio = 2.0f)
    {
        using var effect = new GaussianBlur();
        effect.Radius = radius;
        effect.SigmaRatio = sigmaRatio;

        Apply(effect);
    }

    /// <summary>
    ///     Applies a <see cref="Stride.Rendering.Images.CoCMapBlur" /> effect.
    /// </summary>
    /// <param name="radius">The radius.</param>
    public void CoCMapBlur(int radius = 4)
    {
        using var effect = new CoCMapBlur();
        effect.Radius = radius;

        Apply(effect);
    }

    /// <summary>
    ///     Applies a <see cref="Stride.Rendering.Images.ColorTransformGroup" /> effect.
    /// </summary>
    /// <param name="transforms">The color transforms to apply</param>
    /// <param name="preTransforms">The color pre-transforms to apply</param>
    /// <param name="postTransforms">The color post-transforms to apply</param>
    public void Transform(
        IEnumerable<ColorTransform>? transforms = null,
        IEnumerable<ColorTransform>? preTransforms = null,
        IEnumerable<ColorTransform>? postTransforms = null
    )
    {
        if (transforms is null && preTransforms is null && postTransforms is null) return;

        using var effect = new ColorTransformGroup();

        if (transforms is not null) effect.Transforms.AddRange(transforms);
        if (preTransforms is not null) effect.PreTransforms.AddRange(preTransforms);
        if (postTransforms is not null) effect.PostTransforms.AddRange(postTransforms);

        Apply(effect);
    }

    /// <summary>
    ///     Applies a <see cref="Stride.Rendering.Images.ColorCombiner" /> effect.
    /// </summary>
    /// <param name="textures">The textures combine (use null to use the current drawing buffer as a texture input)</param>
    /// <param name="factors">The factors used to multiply the colors.</param>
    /// <param name="colorMultipliers">The color multiplier of each texture. Default is <see cref="Stride.Core.Mathematics.Color.White"/></param>
    /// <remarks>Null, Empty arrays or array entries with null as a value will be replaced by the current drawing buffer</remarks>
    public void Combine(Texture?[]? textures = null, float[]? factors = null, Color3[]? colorMultipliers = null)
    {
        using var effect = new ColorCombiner();
        if (factors is { Length: > 0 }) Array.Copy(factors, 0, effect.Factors, 0, factors.Length);
        if (colorMultipliers is { Length: > 0 }) Array.Copy(colorMultipliers, 0, effect.ModulateRGB, 0, colorMultipliers.Length);

        Apply(effect, textures);
    }

    /// <summary>
    ///     Applies a color multiply effect.
    /// </summary>
    /// <param name="colorMultiplier">The color multiplier</param>
    public void Colorize(Color4 colorMultiplier)
    {
        using var effect = new ImageScaler();
        effect.Color = colorMultiplier;
        Apply(effect);
    }

    /// <summary>
    ///     Applies a grey scale effect by copying the red channel to blue and green and optionally also recolors the resulting image.
    /// </summary>
    /// <param name="colorMultiplier">The color multiplier. Default is <see cref="Stride.Core.Mathematics.Color.White"/></param>
    public void Recolorize(Color4? colorMultiplier = null)
    {
        using var effect = new ImageScaler();
        effect.IsOnlyChannelRed = true;
        effect.Color = colorMultiplier.GetValueOrDefault(Color4.White);
        Apply(effect);
    }

    /// <summary>
    ///     Applies an image effect to the buffer texture
    /// </summary>
    /// <param name="effect">The image effect to apply</param>
    /// <param name="inputs">The input textures</param>
    /// <remarks>
    ///     Null, Empty arrays or array entries with null as a value will be replaced by the current drawing buffer<br />
    ///     The output will always be replaced by the <see cref="TextureCanvas" /> as a single output.
    /// </remarks>
    public void Apply(ImageEffect effect, params Texture?[]? inputs)
    {
        // Get or create buffers
        var primaryBuffer = EnsurePrimary();
        var secondaryBuffer = EnsureSecondary();

        // Apply the effect
        if (inputs is not { Length: > 0 })
        {
            effect.SetInput(primaryBuffer);
        }
        else
        {
            for (var i = 0; i < inputs.Length; i++)
                effect.SetInput(i, inputs[i] ?? primaryBuffer);
        }

        effect.SetOutput(secondaryBuffer);
        effect.Draw(RenderDrawContext);
        effect.Reset();

        // Switch primary and secondary buffer
        _primaryBuffer = secondaryBuffer;
        _secondaryBuffer = primaryBuffer;
    }

    /// <summary>
    ///     Resamples the texture in a different size or pixel format
    /// </summary>
    /// <param name="size">The new size</param>
    /// <param name="pixelFormat">The new pixel format</param>
    /// <param name="samplingPattern">The filter the pixels are resampled with</param>
    public void Resample(Size2 size, PixelFormat pixelFormat, SamplingPattern samplingPattern = SamplingPattern.Linear)
    {
        // Nothing changed so we can safely skip this step
        if (size == _size && pixelFormat == _pixelFormat) return;

        // Nothing drawn yet means nothing to resample: the first draw creates the buffer with the new settings
        if (_primaryBuffer is not null)
        {
            // Create a new render target with the new settings and dispose the previous buffer at the end
            _secondaryBuffer?.Dispose();
            _secondaryBuffer = CreateRenderTarget(RenderContext, size, pixelFormat);

            try
            {
                // We use the image scaler effect to resample the image to the new size and format
                using var scaler = new ImageScaler(samplingPattern);
                scaler.SetInput(_primaryBuffer);
                scaler.SetOutput(_secondaryBuffer);

                // Finally draw the current buffer to the new render texture
                scaler.Draw(RenderDrawContext);
            }
            finally
            {
                _primaryBuffer.Dispose();
                _primaryBuffer = _secondaryBuffer;
                _secondaryBuffer = null;
            }
        }

        _size = size;
        _pixelFormat = pixelFormat;
    }

    /// <summary>
    ///     Clears the current drawing context and releases internal texture buffers
    /// </summary>
    public void Clear()
    {
        _primaryBuffer?.Dispose();
        _primaryBuffer = null;
        _secondaryBuffer?.Dispose();
        _secondaryBuffer = null;
    }

    /// <summary>
    ///     Ensures the primary buffer is allocated
    /// </summary>
    private Texture EnsurePrimary() => _primaryBuffer ??= CreateRenderTarget(RenderContext, _size, _pixelFormat);

    /// <summary>
    ///     Ensures the secondary buffer is allocated (for effects)
    /// </summary>
    private Texture EnsureSecondary() => _secondaryBuffer ??= CreateRenderTarget(RenderContext, _size, _pixelFormat);

    /// <summary>
    ///     Creates a new render texture based on size and pixel format
    /// </summary>
    private static Texture CreateRenderTarget(RenderContext renderContext, Size2 size, PixelFormat pixelFormat) =>
        Texture.New2D(renderContext.GraphicsDevice, size.Width, size.Height, pixelFormat,
            TextureFlags.ShaderResource | TextureFlags.RenderTarget);

    /// <summary>
    ///     Calculates the target viewport and scissor for drawing textures
    /// </summary>
    private static (Viewport viewport, Rectangle scissor) GetViewportAndScissor(Texture sourceTexture,
        Rectangle sourceRect, Rectangle destinationRect, Stretch stretch, Anchor anchor)
    {
        // How much the source rectangle is scaled on each axis. Contain fits the whole source inside
        // the destination and leaves letterboxing; Cover fills the destination and crops the overflow,
        // which the scissor below is what actually enforces.
        var fitX = destinationRect.Width / (float)sourceRect.Width;
        var fitY = destinationRect.Height / (float)sourceRect.Height;

        var (widthScale, heightScale) = stretch switch
        {
            Stretch.None => (1f, 1f),
            Stretch.Stretch => (fitX, fitY),
            Stretch.Contain => (Math.Min(fitX, fitY), Math.Min(fitX, fitY)),
            Stretch.Cover => (Math.Max(fitX, fitY), Math.Max(fitX, fitY)),
            _ => throw new ArgumentOutOfRangeException(nameof(stretch), stretch, null),
        };

        var finalWidth = (int)(sourceRect.Width * widthScale);
        var finalHeight = (int)(sourceRect.Height * heightScale);

        // Where the scaled image sits inside the destination on each axis: flush at one edge, or
        // centred on the leftover space. The two switches are separate because Anchor combines both.
        var alignmentX = anchor switch
        {
            Anchor.MiddleLeft or Anchor.TopLeft or Anchor.BottomLeft => 0,
            Anchor.TopCenter or Anchor.MiddleCenter or Anchor.BottomCenter => (destinationRect.Width - finalWidth) / 2,
            Anchor.TopRight or Anchor.MiddleRight or Anchor.BottomRight => destinationRect.Width - finalWidth,
            _ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null),
        };

        var alignmentY = anchor switch
        {
            Anchor.TopLeft or Anchor.TopCenter or Anchor.TopRight => 0,
            Anchor.MiddleLeft or Anchor.MiddleCenter or Anchor.MiddleRight => (destinationRect.Height - finalHeight) / 2,
            Anchor.BottomLeft or Anchor.BottomCenter or Anchor.BottomRight => destinationRect.Height - finalHeight,
            _ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null),
        };

        var finalX = destinationRect.X + alignmentX;
        var finalY = destinationRect.Y + alignmentY;

        // The scissor is the destination clipped to itself, so a Cover image cannot paint outside it.
        var scissorX = Math.Max(finalX, destinationRect.X);
        var scissorY = Math.Max(finalY, destinationRect.Y);
        var scissorWidth = Math.Min(finalWidth, destinationRect.Width);
        var scissorHeight = Math.Min(finalHeight, destinationRect.Height);

        // The viewport covers the whole source texture, shifted so that the requested source
        // rectangle - not the texture origin - lands at the aligned destination position.
        var viewportX = finalX - sourceRect.X * widthScale;
        var viewportY = finalY - sourceRect.Y * heightScale;
        var viewportWidth = (int)(sourceTexture.Width * widthScale);
        var viewportHeight = (int)(sourceTexture.Height * heightScale);

        var viewport = new Viewport(viewportX, viewportY, viewportWidth, viewportHeight);
        var scissor = new Rectangle(scissorX, scissorY, scissorWidth, scissorHeight);
        return (viewport, scissor);
    }

    private static Rectangle ToAbsoluteSize(Size3 size, RectangleF relative) => ToAbsoluteSize(new Size2(size.Width, size.Height), relative);

    /// <summary>A rectangle given as fractions of <paramref name="size"/>, in pixels.</summary>
    private static Rectangle ToAbsoluteSize(Size2 size, RectangleF relative)
        => new((int)(size.Width * relative.X), (int)(size.Height * relative.Y), (int)(size.Width * relative.Width), (int)(size.Height * relative.Height));
}
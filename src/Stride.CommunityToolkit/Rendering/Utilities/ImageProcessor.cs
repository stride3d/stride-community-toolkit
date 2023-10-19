using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Images;

namespace Stride.CommunityToolkit.Rendering.Utilities;

/// <summary>
/// A helper class for image processing
/// </summary>
public class ImageProcessor : IDisposable
{
    private Texture? _sourceTexture;
    private Texture? _bufferTexture;
    private Texture? _outputTexture;

    public RenderContext RenderContext { get; }
    public RenderDrawContext RenderDrawContext => RenderContext.GetThreadContext();
    public GraphicsContext GraphicsContext => RenderDrawContext.GraphicsContext;
    public GraphicsDevice GraphicsDevice => RenderContext.GraphicsDevice;
    public CommandList CommandList => GraphicsContext.CommandList;

    public ImageProcessor(IGame game) => RenderContext = RenderContext.GetShared(game.Services);
    public ImageProcessor(IServiceRegistry services) => RenderContext = RenderContext.GetShared(services);
    public ImageProcessor(RenderContext renderContext) => RenderContext = renderContext;

    public void Load(Texture texture) {
        _sourceTexture?.Dispose();
        _sourceTexture = texture.Clone();
    }
    public void Load(string path) {
        using var fileStream = File.OpenRead(path);
        Load(fileStream);
    }
    public void Load(byte[] data) {
        using var stream = new MemoryStream(data);
        stream.Seek(0, SeekOrigin.Begin);
        Load(stream);
    }
    public void Load(Stream stream) {
        _sourceTexture?.Dispose();
        _sourceTexture = Texture.Load(GraphicsDevice, stream);
    }
    public void LoadData(byte[] data, int width, int height, PixelFormat pixelFormat) {
        _sourceTexture?.Dispose();
        _sourceTexture = Texture.New2D(GraphicsDevice, width, height, pixelFormat, data);
    }

    public void Store(string fileName) {
        using var fileStream = File.Open(fileName, FileMode.Create);

        var fileType = Path.GetExtension(fileName) switch
        {
            ".stride" => ImageFileType.Stride,
            ".dds" => ImageFileType.Dds,
            ".png" => ImageFileType.Png,
            ".gif" => ImageFileType.Gif,
            ".jpg" or ".jpeg" => ImageFileType.Jpg,
            ".bmp" => ImageFileType.Bmp,
            ".tiff" => ImageFileType.Tiff,
            ".wmp" => ImageFileType.Wmp,
            ".tga" => ImageFileType.Tga,
            _ => throw new NotSupportedException()
        };
        Store(fileStream, fileType);
    }
    public void Store(byte[] data, ImageFileType fileType)
    {
        using var stream = new MemoryStream(data, true);
        Store(stream, fileType);
    }
    public void Store(Stream stream, ImageFileType fileType)
    {
        var outputTexture = _outputTexture ?? _sourceTexture ?? throw new InvalidOperationException("No texture was loaded so saving is not allowed at this point");
        outputTexture.Save(CommandList, stream, fileType);
    }
    public Texture Store() {
        var outputTexture = _outputTexture ?? _sourceTexture ?? throw new InvalidOperationException("No texture was loaded so saving is not allowed at this point");
        return outputTexture.Clone();
    }
    public void StoreData(byte[] data) {
        var outputTexture = _outputTexture ?? _sourceTexture ?? throw new InvalidOperationException("No texture was loaded so saving is not allowed at this point");
        outputTexture.GetData(CommandList, data);
    }

    /// <summary>
    /// Scales an input texture to an output texture (down or up)
    /// </summary>
    /// <param name="width">The target width</param>
    /// <param name="height">The target height</param>
    /// <param name="samplingPattern">How samples are fetched from the source texture when scaling</param>
    public void Scale(int width, int height, SamplingPattern samplingPattern = SamplingPattern.Expanded) => Scale(new Size2(width, height));

    /// <summary>
    /// Scales an input texture to an output texture (down or up)
    /// </summary>
    /// <param name="targetSize">The target size</param>
    /// <param name="samplingPattern">How samples are fetched from the source texture when scaling</param>
    public void Scale(Size2 targetSize, SamplingPattern samplingPattern = SamplingPattern.Expanded) {
        using var imageScaler = new ImageScaler();
        imageScaler.SamplingPattern = samplingPattern;

        Apply(imageScaler, targetSize);
    }

    /// <summary>
    /// A bright pass filter.
    /// </summary>
    /// <param name="threshold">The threshold relative to the WhitePoint</param>
    /// <param name="steepness">the smooth-step steepness for bright pass filtering.</param>
    /// <param name="color">Modulates bright areas with the provided color. It affects the color of sub-sequent bloom, light-streak effects</param>
    public void BrightFilter(float threshold = 0.2f, float steepness = 1.0f, Color3? color = null)
    {
        using var effect = new BrightFilter();
        effect.Steepness = steepness;
        effect.Threshold = threshold;
        effect.Color = color ?? new Color3(1.0f);
        Apply(effect);
    }

    /// <summary>
    /// Provides a gaussian blur effect.
    /// </summary>
    /// <param name="radius">The radius.</param>
    /// <param name="sigmaRatio">The sigma ratio. The sigma ratio is used to calculate the sigma based on the radius: The actual formula is sigma = radius / SigmaRatio. The default value is 2.0f.</param>
    public void GaussianBlur(int radius = 4, float sigmaRatio = 2.0f)
    {
        using var effect = new GaussianBlur();
        effect.Radius = radius;
        effect.SigmaRatio = sigmaRatio;

        Apply(effect);
    }

    /// <summary>
    /// Blurs a Circle of Confusion map.
    /// </summary>
    /// <param name="radius">The radius.</param>
    public void CoCMapBlur(int radius = 4) {
        using var effect = new CoCMapBlur();
        effect.Radius = radius;

        Apply(effect);
    }

    /// <summary>
    /// Applies an image effect to the source texture and
    /// </summary>
    public void Apply(IImageEffect effect, Size2? targetSize = null) {
        // Get or create buffers
        var input = _outputTexture ?? _sourceTexture ?? throw new InvalidOperationException("No texture was loaded so applying effects is not allowed at this point");
        var output = _bufferTexture;

        var inputSize = new Size2(input.Width, input.Height);
        var outputSize = targetSize ?? inputSize;

        // If the sizes are not compatible we need to reallocate our as we can no longer use it
        var forceDispose = outputSize != inputSize;
        if (output == null || forceDispose) {
            output?.Dispose();
            output = Texture.New2D(GraphicsDevice, outputSize.Width, outputSize.Height, input.Format, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
        }

        // Apply the effect
        effect.SetInput(input);
        effect.SetOutput(output);
        effect.Draw(RenderDrawContext);

        // Switch buffer and output
        // It might be that the buffer size is now incompatible again but we would check this the next time anyway
        _bufferTexture = _outputTexture;
        _outputTexture = output;
    }

    /// <summary>
    /// Clears the current processor instance
    /// </summary>
    public void Clear() {
        _sourceTexture?.Dispose();
        _sourceTexture = null;
        _bufferTexture?.Dispose();
        _bufferTexture = null;
        _outputTexture?.Dispose();
        _outputTexture = null;
    }

    public void Dispose()
    {
        Clear();
        GC.SuppressFinalize(this);
    }
}
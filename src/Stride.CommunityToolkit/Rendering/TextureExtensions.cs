using Stride.Graphics;
using System.Reflection;

namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// Provides extension methods for working with <see cref="Texture"/> objects.
/// This includes methods for finding asset source directories, resizing textures,
/// and reformatting textures.
/// </summary>
public static class TextureExtensions
{
    private const string SourceFileIdentifier = "Source: !file";

    /// <summary>
    /// Finds the asset file named <paramref name="urlName"/> and returns the path on its <c>Source: !file</c> line.
    /// </summary>
    /// <param name="urlName">The asset file name to search for, such as <c>Brick.sdtex</c>.</param>
    /// <param name="workDir">The folder to search, appended to the folder three levels above the executing assembly.</param>
    /// <returns>The source path, or an empty string if the asset or its source line is not found.</returns>
    public static string FindAssetSourceDir(string urlName, string workDir)
    {
        // Three folders above the executing assembly is where the solution usually sits
        var startupPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location)?.Parent?.Parent?.Parent?.FullName;
        var asset = new DirectoryInfo(startupPath + workDir).GetFiles(urlName, SearchOption.AllDirectories).FirstOrDefault();

        if (asset is null) return string.Empty;

        // The *.sdtex asset names its source file on one line
        foreach (var line in File.ReadLines(asset.FullName))
        {
            if (line.StartsWith(SourceFileIdentifier, StringComparison.Ordinal)) return line[SourceFileIdentifier.Length..];
        }

        return string.Empty;
    }

    /// <summary>
    /// Renders a texture into a new texture with the given size and pixel format.
    /// Also clears the back buffer and draws the result onto it. Load a compressed texture from its source file first.
    /// </summary>
    /// <param name="texture">The original texture to resize.</param>
    /// <param name="width">The desired width of the resized texture.</param>
    /// <param name="height">The desired height of the resized texture.</param>
    /// <param name="graphicsContext">The graphics context to use for rendering.</param>
    /// <param name="pixelFormat">The pixel format for the resized texture (default is PixelFormat.R8G8B8A8_UNorm).</param>
    /// <returns>A new texture with the specified width, height, and pixel format, or null if the operation fails or the original texture has invalid dimensions.</returns>
    public static Texture? Resize(this Texture texture, int width, int height, GraphicsContext graphicsContext, PixelFormat pixelFormat = PixelFormat.R8G8B8A8_UNorm)
    {
        try
        {
            if (texture.Width == 0 || texture.Height == 0) return null;

            var device = texture.GraphicsDevice;
            var commandList = graphicsContext.CommandList;
            var target = Texture.New2D(device, width, height, pixelFormat, TextureFlags.ShaderResource | TextureFlags.RenderTarget);

            using var depthBuffer = Texture.New2D(device, width, height, PixelFormat.D24_UNorm_S8_UInt, TextureFlags.DepthStencil);
            using var spriteBatch = new SpriteBatch(device);

            // Render into the new texture
            commandList.Clear(target, new Color4(0, 0, 0, 0));
            commandList.Clear(depthBuffer, DepthStencilClearOptions.DepthBuffer);
            commandList.SetRenderTargetAndViewport(depthBuffer, target);

            spriteBatch.Begin(graphicsContext);
            spriteBatch.Draw(texture, new RectangleF(0, 0, width, height), null, Color.White, 0, Vector2.Zero);
            spriteBatch.End();

            // Then copy it to the screen
            commandList.Clear(device.Presenter.BackBuffer, Color.Black);
            commandList.Clear(device.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
            commandList.SetRenderTargetAndViewport(device.Presenter.DepthStencilBuffer, device.Presenter.BackBuffer);

            spriteBatch.Begin(graphicsContext);
            spriteBatch.Draw(target, new RectangleF(0, 0, width, height), null, Color.White, 0, Vector2.Zero);
            spriteBatch.End();

            return target;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Reformats the pixels of a given texture via a rendering to texture approach.
    /// </summary>
    /// <param name="texture">The texture to reformat.</param>
    /// <param name="graphicsContext">The graphics context to use for rendering.</param>
    /// <param name="pixelFormat">The pixel format of the new texture (default is PixelFormat.R8G8B8A8_UNorm).</param>
    /// <returns>A new texture of the same size in the requested format, or null if the operation fails.</returns>
    public static Texture? ReFormat(this Texture texture, GraphicsContext graphicsContext, PixelFormat pixelFormat = PixelFormat.R8G8B8A8_UNorm)
        => texture.Resize(texture.Width, texture.Height, graphicsContext, pixelFormat);
}
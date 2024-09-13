using Stride.Graphics;
using System.Reflection;

namespace Stride.CommunityToolkit.Extensions;

/// <summary>
/// Provides extension methods for working with <see cref="Texture"/> objects.
/// This includes methods for finding asset source directories, resizing textures,
/// and reformatting textures.
/// </summary>
public static class TextureExtensions
{
    private const string SourceFileIdentifier = "Source: !file";

    /// <summary>
    /// Retrieves the full filename of the Source of an asset.
    /// workDir should be your Assets directory.
    /// </summary>
    /// <param name="urlName"></param>
    /// <param name="workDir"></param>
    /// <returns></returns>
    public static string FindAssetSourceDir(string urlName, string workDir)
    {
        //start in the same directory as the .sln project
        string? startupPath = Directory.GetParent(Assembly.
            GetExecutingAssembly().Location)?.Parent?.Parent?.Parent?.
            FullName;
        DirectoryInfo dir = new DirectoryInfo(startupPath + workDir);
        FileInfo[] Files = dir.GetFiles(urlName, SearchOption.AllDirectories);

        if (Files.Length == 0)
        {
            return "";
        }

        string filename = Files[0].FullName;

        //open the *.sdtex file and read the Source
        var lines = File.ReadAllLines(filename);
        string outFilename = "";

        for (var i = 0; i < lines.Length; i += 1)
        {
            string line = lines[i];

            // Process line
            if (line.StartsWith(SourceFileIdentifier))
            {
                outFilename = line.Substring(SourceFileIdentifier.Length);
                break;
            }
        }

        return outFilename;
    }

    /// <summary>
    /// Resizes and reformats a given texture by rendering it to a new texture with the specified dimensions and pixel format.
    /// This is useful for processing textures loaded through `Content.Load&lt;Texture&gt;()`, which may be compressed and default to a smaller size like 32x32 pixels.
    /// To properly resize a compressed texture, ensure it's decompressed by loading the source file from disk before applying this method.
    /// </summary>
    /// <param name="texture">The original texture to resize.</param>
    /// <param name="width">The desired width of the resized texture.</param>
    /// <param name="height">The desired height of the resized texture.</param>
    /// <param name="graphicsContext">The graphics context to use for rendering.</param>
    /// <param name="pixelFormat">The pixel format for the resized texture (default is PixelFormat.R8G8B8A8_UNorm).</param>
    /// <returns>A new texture with the specified width, height, and pixel format, or null if the operation fails or the original texture has invalid dimensions.</returns>
    public static Texture? Resize(this Texture texture, int width, int height,
        GraphicsContext graphicsContext, PixelFormat pixelFormat = PixelFormat.R8G8B8A8_UNorm)
    {
        try
        {
            if (texture.Width == 0 || texture.Height == 0) return null;

            //if(texture.Width==width && texture.Height == height) return texture;
            GraphicsDevice GraphicsDevice = texture.GraphicsDevice;
            Texture offlineTarget = Texture.New2D(GraphicsDevice, width, height,
                pixelFormat, TextureFlags.ShaderResource |
                TextureFlags.RenderTarget);
            Texture depthBuffer = Texture.New2D(GraphicsDevice, width, height,
                PixelFormat.D24_UNorm_S8_UInt, TextureFlags.DepthStencil);
            SpriteBatch spriteBatch = new SpriteBatch(GraphicsDevice);

            // render into texture
            graphicsContext.CommandList.Clear(offlineTarget, new Color4(0, 0, 0, 0));
            graphicsContext.CommandList.Clear(depthBuffer, DepthStencilClearOptions.DepthBuffer);
            graphicsContext.CommandList.SetRenderTargetAndViewport(depthBuffer, offlineTarget);

            spriteBatch.Begin(graphicsContext);
            spriteBatch.Draw(texture, new RectangleF(0, 0, width, height), null, Color.White, 0, Vector2.Zero);
            spriteBatch.End();

            // copy texture on screen
            graphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.Black);
            graphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
            graphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);

            spriteBatch.Begin(graphicsContext);
            spriteBatch.Draw(offlineTarget, new RectangleF(0, 0, width, height), null, Color.White, 0, Vector2.Zero);
            spriteBatch.End();

            // offlineTarget.ToStaging();
            return offlineTarget;
        }

        catch { return null; }
    }

    /// <summary>
    /// Reformats the pixels of a given texture via a rendering to texture approach.
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="graphicsContext"></param>
    /// <param name="pixelFormat"></param>
    /// <returns></returns>
    public static Texture? ReFormat(
        this Texture texture, GraphicsContext graphicsContext,
        PixelFormat pixelFormat = PixelFormat.R8G8B8A8_UNorm)
    {
        return texture.Resize(texture.Width, texture.Height, graphicsContext, pixelFormat);
    }
}
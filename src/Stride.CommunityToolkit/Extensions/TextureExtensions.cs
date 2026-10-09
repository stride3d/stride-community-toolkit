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
    /// Finds the asset file named <paramref name="urlName"/> and returns the path on its <c>Source: !file</c> line.
    /// </summary>
    /// <param name="urlName">The asset file name to search for, such as <c>Brick.sdtex</c>.</param>
    /// <param name="workDir">The folder to search, appended to the folder three levels above the executing assembly.</param>
    /// <returns>The source path, or an empty string if the asset or its source line is not found.</returns>
    public static string FindAssetSourceDir(string urlName, string workDir)
    {
        //start in the same directory as the .sln project
        string? startupPath = Directory.GetParent(Assembly.
            GetExecutingAssembly().Location)?.Parent?.Parent?.Parent?.
            FullName;
        DirectoryInfo dir = new(startupPath + workDir);
        FileInfo[] Files = dir.GetFiles(urlName, SearchOption.AllDirectories);

        if (Files.Length == 0)
        {
            return "";
        }

        string filename = Files[0].FullName;

        //open the *.sdtex file and read the Source
        var lines = File.ReadAllLines(filename);
        string outFilename = "";

        for (var i = 0; i < lines.Length; ++i)
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
    /// Renders a texture into a new texture with the given size and pixel format.
    /// Also clears the back buffer and draws the result onto it. Load a compressed texture from its source file first.
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
            using Texture depthBuffer = Texture.New2D(GraphicsDevice, width, height,
                PixelFormat.D24_UNorm_S8_UInt, TextureFlags.DepthStencil);
            using SpriteBatch spriteBatch = new(GraphicsDevice);

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
using Stride.Graphics;
using System.Reflection;

namespace Stride.CommunityToolkit.Extensions;

public static class TextureExtensions
{
    /// <summary>
    /// Retrieves the full filename of the Source of an asset.
    /// workdir should be your Assets directory.
    /// </summary>
    /// <param name="urlname"></param>
    /// <param name="workdir"></param>
    /// <returns></returns>
    public static string FindAssetSourceDir(string urlname, string workdir)
    {
        //start in the same directory as the .sln project
        string? startupPath = Directory.GetParent(Assembly.
            GetExecutingAssembly().Location)?.Parent?.Parent?.Parent?.
            FullName;
        DirectoryInfo dir = new DirectoryInfo(startupPath + workdir);
        FileInfo[] Files = dir.GetFiles(urlname, SearchOption.AllDirectories);

        if (Files.Length == 0)
        {
            return "";
        }

        string filename = Files[0].FullName;
        //open the *.sdtex file and read the Source
        var lines = File.ReadAllLines(filename);
        string outfilename = "";

        for (var i = 0; i < lines.Length; i += 1)
        {
            string line = lines[i];
            // Process line
            if (line.IndexOf("Source: !file") >= 0)
            {
                outfilename = line.Substring(13);
                break;
            }
        }

        return outfilename;
    }

    /// <summary>
    /// Performs render to texture in a single function in order to resize and reformat a
    /// given texture. if the texture was loaded using Content.Load<Texture>(...)
    /// the texture is usually compressed and it will be by default 32x32.
    /// So make sure you decompress it first by loading the Source file from the disc that this
    /// asset refers to.
    /// </summary>
    /// <param name="texin"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="GraphicsContext"></param>
    /// <param name="pixelformat"></param>
    /// <returns></returns>
    public static Texture? Resize(this Texture texin, int width, int height,
        GraphicsContext GraphicsContext, PixelFormat pixelformat = PixelFormat.R8G8B8A8_UNorm)
    {
        try
        {
            if (texin.Width == 0 || texin.Height == 0) return null;

            //if(texin.Width==width && texin.Height == height) return texin;
            GraphicsDevice GraphicsDevice = texin.GraphicsDevice;
            Texture offlineTarget = Texture.New2D(GraphicsDevice, width, height,
                pixelformat, TextureFlags.ShaderResource |
                TextureFlags.RenderTarget);
            Texture depthBuffer = Texture.New2D(GraphicsDevice, width, height,
                PixelFormat.D24_UNorm_S8_UInt, TextureFlags.DepthStencil);
            SpriteBatch spriteBatch = new SpriteBatch(GraphicsDevice);

            // render into texture
            GraphicsContext.CommandList.Clear(offlineTarget, new Color4(0, 0, 0, 0));
            GraphicsContext.CommandList.Clear(depthBuffer, DepthStencilClearOptions.DepthBuffer);
            GraphicsContext.CommandList.SetRenderTargetAndViewport(depthBuffer, offlineTarget);

            spriteBatch.Begin(GraphicsContext);
            spriteBatch.Draw(texin, new RectangleF(0, 0, width, height), null, Color.White, 0, Vector2.Zero);
            spriteBatch.End();

            // copy texture on screen
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.Black);
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
            GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);

            spriteBatch.Begin(GraphicsContext);
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
    /// <param name="texin"></param>
    /// <param name="GraphicsContext"></param>
    /// <param name="pixelformat"></param>
    /// <returns></returns>
    public static Texture? ReFormat(
        this Texture texin, GraphicsContext GraphicsContext,
        PixelFormat pixelformat = PixelFormat.R8G8B8A8_UNorm)
    {
        return texin.Resize(texin.Width, texin.Height, GraphicsContext, pixelformat);
    }
}
using Stride.Graphics;
using System.Reflection;

namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// Extension methods for <see cref="Texture"/>: a copy at another size or in another pixel format, drawn through a
/// <see cref="TextureCanvas"/>, and the source file of a texture asset in a source checkout.
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
    /// <remarks>
    /// For tools, tests and drafting in a source checkout: the search starts three folders above the executing
    /// assembly, where the solution usually sits, which a shipped game does not have.
    /// </remarks>
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
    /// A copy of the texture at another size, and optionally in another pixel format, drawn through a
    /// <see cref="TextureCanvas"/> with linear sampling. Nothing is drawn on screen.
    /// </summary>
    /// <param name="texture">The texture to copy.</param>
    /// <param name="width">The width of the copy, in pixels.</param>
    /// <param name="height">The height of the copy, in pixels.</param>
    /// <param name="services">The game's services, which hold the render context the canvas draws with: <c>game.Services</c>, or a script's <c>Services</c>.</param>
    /// <param name="pixelFormat">The pixel format of the copy; one a render target can have.</param>
    /// <returns>A new texture the caller owns, to dispose when it is no longer needed.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="texture"/> or <paramref name="services"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="width"/> or <paramref name="height"/> is not positive.</exception>
    public static Texture Resize(this Texture texture, int width, int height, IServiceRegistry services, PixelFormat pixelFormat = PixelFormat.R8G8B8A8_UNorm)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        using var canvas = services.CreateTextureCanvas(new Size2(width, height), pixelFormat);

        canvas.DrawTexture(texture);

        return canvas.ToTexture();
    }

    /// <summary>
    /// A copy of the texture in another pixel format, at the same size: <see cref="Resize"/> without the resize.
    /// </summary>
    /// <param name="texture">The texture to copy.</param>
    /// <param name="services">The game's services, which hold the render context the canvas draws with: <c>game.Services</c>, or a script's <c>Services</c>.</param>
    /// <param name="pixelFormat">The pixel format of the copy; one a render target can have.</param>
    /// <returns>A new texture the caller owns, to dispose when it is no longer needed.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="texture"/> or <paramref name="services"/> is <see langword="null"/>.</exception>
    public static Texture ReFormat(this Texture texture, IServiceRegistry services, PixelFormat pixelFormat = PixelFormat.R8G8B8A8_UNorm)
    {
        ArgumentNullException.ThrowIfNull(texture);

        return texture.Resize(texture.Width, texture.Height, services, pixelFormat);
    }
}
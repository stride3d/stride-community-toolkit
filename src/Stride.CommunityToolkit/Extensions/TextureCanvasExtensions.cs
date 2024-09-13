using Stride.CommunityToolkit.Rendering.Utilities;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.CommunityToolkit.Extensions;

/// <summary>
/// Provides extension methods for creating instances of <see cref="TextureCanvas"/>.
/// </summary>
public static class TextureCanvasExtensions
{
    /// <summary>
    /// Creates a new instance of <see cref="TextureCanvas"/> using the specified <see cref="RenderContext"/>.
    /// </summary>
    /// <param name="renderContext">The render context to use for creating the texture canvas.</param>
    /// <param name="size">The size of the texture canvas. If null, a default size will be used.</param>
    /// <param name="pixelFormat">The pixel format of the texture canvas. Default is <see cref="PixelFormat.R8G8B8A8_UNorm"/>.</param>
    /// <returns>A new instance of <see cref="TextureCanvas"/>.</returns>
    public static TextureCanvas CreateTextureCanvas(this RenderContext renderContext, Size2? size, PixelFormat pixelFormat = PixelFormat.R8G8B8A8_UNorm)
    {
        return new TextureCanvas(renderContext, size, pixelFormat);
    }

    /// <summary>
    /// Creates a new instance of <see cref="TextureCanvas"/> using the specified <see cref="IServiceRegistry"/>.
    /// </summary>
    /// <param name="services">The service registry to use for creating the texture canvas.</param>
    /// <param name="size">The size of the texture canvas. If null, a default size will be used.</param>
    /// <param name="pixelFormat">The pixel format of the texture canvas. Default is <see cref="PixelFormat.R8G8B8A8_UNorm"/>.</param>
    /// <returns>A new instance of <see cref="TextureCanvas"/>.</returns>
    public static TextureCanvas CreateTextureCanvas(this IServiceRegistry services, Size2? size, PixelFormat pixelFormat = PixelFormat.R8G8B8A8_UNorm)
    {
        return new TextureCanvas(RenderContext.GetShared(services), size, pixelFormat);
    }

    /// <summary>
    /// Creates a new instance of <see cref="TextureCanvas"/> using the specified <see cref="IGame"/>.
    /// </summary>
    /// <param name="game">The game instance to use for creating the texture canvas.</param>
    /// <param name="size">The size of the texture canvas. If null, a default size will be used.</param>
    /// <param name="pixelFormat">The pixel format of the texture canvas. Default is <see cref="PixelFormat.R8G8B8A8_UNorm"/>.</param>
    /// <returns>A new instance of <see cref="TextureCanvas"/>.</returns>
    public static TextureCanvas CreateTextureCanvas(this IGame game, Size2? size, PixelFormat pixelFormat = PixelFormat.R8G8B8A8_UNorm)
    {
        return new TextureCanvas(RenderContext.GetShared(game.Services), size, pixelFormat);
    }
}
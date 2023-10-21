using Stride.CommunityToolkit.Rendering.Utilities;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.CommunityToolkit.Extensions;

public static class TextureCanvasExtensions{
    public static TextureCanvas CreateTextureCanvas(this RenderContext renderContext, Size2? size, PixelFormat pixelFormat = PixelFormat.R8G8B8A8_UNorm)
    {
        return new TextureCanvas(renderContext, size, pixelFormat);
    }

    public static TextureCanvas CreateTextureCanvas(this IServiceRegistry services, Size2? size, PixelFormat pixelFormat = PixelFormat.R8G8B8A8_UNorm)
    {
        return new TextureCanvas(RenderContext.GetShared(services), size, pixelFormat);
    }

    public static TextureCanvas CreateTextureCanvas(this IGame game, Size2? size, PixelFormat pixelFormat = PixelFormat.R8G8B8A8_UNorm)
    {
        return new TextureCanvas(RenderContext.GetShared(game.Services), size, pixelFormat);
    }
}
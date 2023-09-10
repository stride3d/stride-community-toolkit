using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.CommunityToolkit.Extensions;

public static class GraphicsDeviceExtensions
{
    /// <summary>
    /// Retrieves the current window size of the GraphicsDevice as a <see cref="Int2"/>.
    /// </summary>
    /// <param name="graphics">The GraphicsDevice for which the window size should be retrieved.</param>
    /// <returns>An <see cref="Int2"/> representing the current window size, where <see cref="Int2.X"/> is the width and <see cref="Int2.Y"/> is the height.</returns>
    public static Int2 GetWindowSize(this GraphicsDevice graphics)
    {
        int width = graphics.Presenter.BackBuffer.Width;
        int height = graphics.Presenter.BackBuffer.Height;
        return new Int2(width, height);
    }
}
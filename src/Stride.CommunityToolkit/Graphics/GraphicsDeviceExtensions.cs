using Stride.Graphics;

namespace Stride.CommunityToolkit.Graphics;

/// <summary>
/// Provides extension methods for the <see cref="GraphicsDevice"/> to simplify common operations related to graphics rendering.
/// </summary>
public static class GraphicsDeviceExtensions
{
    /// <summary>
    /// Retrieves the current window size of the <see cref="GraphicsDevice"/> as an <see cref="Int2"/> structure.
    /// </summary>
    /// <param name="graphics">The <see cref="GraphicsDevice"/> for which the window size should be retrieved.</param>
    /// <returns>
    /// An <see cref="Int2"/> representing the current window size. The <see cref="Int2.X"/> value corresponds to the window's width,
    /// and the <see cref="Int2.Y"/> value corresponds to the window's height.
    /// </returns>
    /// <remarks>
    /// This method provides a convenient way to access the dimensions of the back buffer, which corresponds to the size of the window
    /// in which the game or application is running.
    /// </remarks>
    public static Int2 GetWindowSize(this GraphicsDevice graphics)
    {
        int width = graphics.Presenter.BackBuffer.Width;
        int height = graphics.Presenter.BackBuffer.Height;

        return new Int2(width, height);
    }
}
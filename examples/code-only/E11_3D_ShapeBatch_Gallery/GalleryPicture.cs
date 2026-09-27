using Stride.CommunityToolkit.Rendering;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace E11_3D_ShapeBatch_Gallery;

/// <summary>The picture the textured stations are filled with.</summary>
public static class GalleryPicture
{
    /// <summary>
    /// A 128 by 128 picture with an unmistakable orientation: a warm-to-cool diagonal, a grid of
    /// dark lines, a white square in the top-left corner and a black one at the bottom right.
    /// </summary>
    /// <param name="device">The device to make the texture on.</param>
    /// <returns>The texture, sRGB with its mipmaps, owned by the caller.</returns>
    public static Texture Create(GraphicsDevice device)
    {
        const int size = 128;
        var pixels = new Color[size * size];

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var t = 0.5f * (x + y) / (size - 1);
                var color = Color.Lerp(new Color(255, 120, 40), new Color(40, 110, 255), t);

                if (x % 16 == 0 || y % 16 == 0)
                {
                    color = new Color(20, 20, 30);
                }

                if (x < 24 && y < 24)
                {
                    color = Color.White;
                }
                else if (x >= size - 24 && y >= size - 24)
                {
                    color = Color.Black;
                }

                pixels[y * size + x] = color;
            }
        }

        // Through the toolkit's loader rather than Texture.New2D, for the mipmap chain: tiled and seen at
        // a grazing angle, a picture with one level shimmers
        return TextureLoader.FromPixels(device, pixels, size, size, new TextureLoadOptions(TextureRole.Color));
    }
}
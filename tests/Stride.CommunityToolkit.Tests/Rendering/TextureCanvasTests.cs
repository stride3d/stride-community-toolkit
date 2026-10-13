using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Tests.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// Pins that the <c>Size</c> and <c>PixelFormat</c> setters of a <c>TextureCanvas</c> take effect: the
/// setters used to assign the field first, so the resample that compared against it always found
/// nothing to do. The buffer is created on first use, so the setters are checked before it, where no
/// shader is needed; the resample of a drawn buffer goes through the image scaler shader, which the
/// test host does not have.
/// </summary>
[Collection(GameExtensionsRunTests.Name)]
public class TextureCanvasTests
{
    private const int FrameBudget = 60;

    [Fact]
    public void SizeSetter_SizesTheBuffer()
    {
        using var game = new Game();

        Size2 size = default;
        var byteSize = 0;

        game.Run(SceneSetup.None, start: scene =>
        {
            using var canvas = game.CreateTextureCanvas(new Size2(64, 64));

            canvas.Size = new Size2(32, 16);

            size = canvas.Size;
            byteSize = canvas.ByteSize;
        }, update: (_, _) =>
        {
            if (game.UpdateTime.FrameCount > FrameBudget || byteSize > 0)
                game.Exit();
        }, context: new GameContextHeadless());

        Assert.Equal(new Size2(32, 16), size);
        Assert.Equal(32 * 16 * 4, byteSize);
    }

    [Fact]
    public void PixelFormatSetter_SizesTheBuffer()
    {
        using var game = new Game();

        PixelFormat format = default;
        var byteSize = 0;

        game.Run(SceneSetup.None, start: scene =>
        {
            using var canvas = game.CreateTextureCanvas(new Size2(16, 16));

            canvas.PixelFormat = PixelFormat.R16G16B16A16_Float;

            format = canvas.PixelFormat;
            byteSize = canvas.ByteSize;
        }, update: (_, _) =>
        {
            if (game.UpdateTime.FrameCount > FrameBudget || byteSize > 0)
                game.Exit();
        }, context: new GameContextHeadless());

        Assert.Equal(PixelFormat.R16G16B16A16_Float, format);
        Assert.Equal(16 * 16 * 8, byteSize);
    }
}
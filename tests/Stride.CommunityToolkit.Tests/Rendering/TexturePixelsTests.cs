using Stride.CommunityToolkit.Rendering;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// The pixel work of the texture loader, on plain arrays with no device: the sRGB transfer function,
/// premultiplication in linear light, the normal map's green flip, and mipmaps averaged in the right
/// space for each role - the three places a hand-written loader goes wrong without anything failing.
/// </summary>
public class TexturePixelsTests
{
    [Fact]
    public void EverySrgbByteSurvivesARoundTripThroughLinear()
    {
        for (var i = 0; i < 256; i++)
        {
            Assert.Equal((byte)i, TexturePixels.LinearToSrgb(TexturePixels.SrgbToLinear((byte)i)));
        }
    }

    [Fact]
    public void MidGreyInSrgbIsAboutAFifthInLinearLight()
    {
        // Why a colour map read as data looks pale: the byte 128 means 0.22, not 0.5
        Assert.Equal(0.216f, TexturePixels.SrgbToLinear(128), 3);
        Assert.Equal(188, TexturePixels.LinearToSrgb(0.5f));
    }

    [Fact]
    public void AColourMipOfBlackAndWhiteIsHalfTheLightNotHalfTheByte()
    {
        Color[] checker = [Color.Black, Color.White, Color.White, Color.Black];

        var levels = TexturePixels.BuildMipChain(checker, 2, 2, TextureRole.Color);

        Assert.Equal(2, levels.Count);
        Assert.Equal(new Color(188, 188, 188, 255), levels[1][0]);
    }

    [Fact]
    public void ADataMipAveragesTheStoredValues()
    {
        Color[] checker = [Color.Black, Color.White, Color.White, Color.Black];

        var levels = TexturePixels.BuildMipChain(checker, 2, 2, TextureRole.Data);

        Assert.Equal(new Color(128, 128, 128, 255), levels[1][0]);
    }

    [Fact]
    public void NormalMapMipsKeepUnitLength()
    {
        // Two normals tilted forty-five degrees apart average to a shorter vector; the mip renormalises it
        var up = new Color(128, 128, 255, 255);
        var tilted = new Color(218, 128, 218, 255);
        Color[] pixels = [up, tilted, up, tilted];

        var mip = TexturePixels.BuildMipChain(pixels, 2, 2, TextureRole.NormalMap)[1][0];
        var normal = new Vector3(mip.R / 127.5f - 1f, mip.G / 127.5f - 1f, mip.B / 127.5f - 1f);

        Assert.Equal(1f, normal.Length(), 1);
    }

    [Fact]
    public void PremultiplyingLeavesOpaquePixelsAloneAndClearsTransparentOnes()
    {
        Color[] pixels = [new Color(10, 200, 30, 255), new Color(255, 255, 255, 0), new Color(255, 0, 0, 128)];

        TexturePixels.PremultiplyAlpha(pixels);

        Assert.Equal(new Color(10, 200, 30, 255), pixels[0]);
        Assert.Equal(new Color(0, 0, 0, 0), pixels[1]);

        // Full red at half coverage is half the light, which in sRGB is 188, not the 128 of scaling the byte
        Assert.Equal(new Color(188, 0, 0, 128), pixels[2]);
    }

    [Fact]
    public void InvertingGreenFlipsOnlyGreen()
    {
        Color[] pixels = [new Color(10, 0, 30, 40)];

        TexturePixels.InvertGreen(pixels);

        Assert.Equal(new Color(10, 255, 30, 40), pixels[0]);
    }

    [Fact]
    public void SwappingRedAndBlueTurnsBgraIntoRgba()
    {
        Color[] pixels = [new Color(1, 2, 3, 4)];

        TexturePixels.SwapRedBlue(pixels);

        Assert.Equal(new Color(3, 2, 1, 4), pixels[0]);
    }

    [Theory]
    [InlineData(512, 512, 10)]
    [InlineData(256, 128, 9)]
    [InlineData(5, 3, 3)]
    [InlineData(1, 1, 1)]
    public void TheChainRunsDownToOneTexel(int width, int height, int count)
    {
        Assert.Equal(count, TexturePixels.MipCount(width, height));

        var levels = TexturePixels.BuildMipChain(new Color[width * height], width, height, TextureRole.Data);

        Assert.Equal(count, levels.Count);
        Assert.Single(levels[^1]);
    }
}
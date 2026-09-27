using Stride.CommunityToolkit.Rendering;
using Stride.Core.Mathematics;
using Stride.Rendering.Materials;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// The pure halves of the runtime-parameter helper: the colour conversion that must match what the
/// generator compiled in, and the key naming that must match how the generator suffixes a key's
/// later uses. Setting on a material needs a device, so that half is exercised by the gallery.
/// </summary>
public class MaterialParametersTests
{
    [Fact]
    public void AMaterialValueIsLinearAndPremultiplied()
    {
        // sRGB 0.5 is about 0.214 linear; at alpha 0.5, premultiplied, about 0.107
        var value = MaterialParameters.ToMaterialValue(new Color4(0.5f, 0.5f, 0.5f, 0.5f));

        Assert.Equal(0.107f, value.R, 3);
        Assert.Equal(0.107f, value.G, 3);
        Assert.Equal(0.107f, value.B, 3);
        Assert.Equal(0.5f, value.A, 3);
    }

    [Fact]
    public void PremultiplicationCanBeLeftOut()
    {
        var value = MaterialParameters.ToMaterialValue(new Color4(0.5f, 0.5f, 0.5f, 0.5f), premultiply: false);

        Assert.Equal(0.214f, value.R, 3);
        Assert.Equal(0.5f, value.A, 3);
    }

    [Fact]
    public void BlackAndWhiteAreFixedPointsOfTheConversion()
    {
        Assert.Equal(Color4.Black, MaterialParameters.ToMaterialValue(Color4.Black));
        Assert.Equal(Color4.White, MaterialParameters.ToMaterialValue(Color4.White));
    }

    [Fact]
    public void KeysCarryTheNameTheyWereGiven()
    {
        Assert.Equal("MyGame.Tint", MaterialParameters.ColorKey("MyGame.Tint").Name);
        Assert.Equal("MyGame.Wear", MaterialParameters.FloatKey("MyGame.Wear").Name);
        Assert.Equal("MyGame.Screen", MaterialParameters.TextureKey("MyGame.Screen").Name);
    }

    [Fact]
    public void TheFirstTextureUsesTheBaseKeyAndLaterOnesAreSuffixed()
    {
        Assert.Same(MaterialKeys.TextureScale, MaterialParameters.TextureKeyAt(MaterialKeys.TextureScale, 0));
        Assert.EndsWith(".i1", MaterialParameters.TextureKeyAt(MaterialKeys.TextureScale, 1).Name);
        Assert.EndsWith(".i2", MaterialParameters.TextureKeyAt(MaterialKeys.TextureOffset, 2).Name);
    }
}
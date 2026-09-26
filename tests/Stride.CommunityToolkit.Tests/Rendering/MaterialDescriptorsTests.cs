using Stride.CommunityToolkit.Rendering;
using Stride.Core.Mathematics;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// The descriptors are plain objects, so what a helper puts in the bag is checked without a device:
/// which slots are filled, which are left empty, and that every lit one carries the environment term
/// a code-only game can resolve.
/// </summary>
public class MaterialDescriptorsTests
{
    [Fact]
    public void PbrFillsTheFourNumbersUnderLambertAndMicrofacet()
    {
        var descriptor = MaterialDescriptors.Pbr(Color.Green, metalness: 0.25f, glossiness: 0.8f);
        var a = descriptor.Attributes;

        Assert.Equal(Color.Green, Assert.IsType<ComputeColor>(Assert.IsType<MaterialDiffuseMapFeature>(a.Diffuse).DiffuseMap).Value);
        Assert.IsType<MaterialDiffuseLambertModelFeature>(a.DiffuseModel);
        Assert.Equal(0.8f, Assert.IsType<ComputeFloat>(Assert.IsType<MaterialGlossinessMapFeature>(a.MicroSurface).GlossinessMap).Value);
        Assert.Equal(0.25f, Assert.IsType<ComputeFloat>(Assert.IsType<MaterialMetalnessMapFeature>(a.Specular).MetalnessMap).Value);
        Assert.IsType<MaterialSpecularMicrofacetEnvironmentGGXPolynomial>(Assert.IsType<MaterialSpecularMicrofacetModelFeature>(a.SpecularModel).Environment);
    }

    [Fact]
    public void PbrDefaultsToAMatteDielectric()
    {
        var a = MaterialDescriptors.Pbr(Color.Red).Attributes;

        // A colour alone is a matte cube of that colour: no metalness, the middling glossiness
        Assert.Equal(0f, Assert.IsType<ComputeFloat>(Assert.IsType<MaterialMetalnessMapFeature>(a.Specular).MetalnessMap).Value);
        Assert.Equal(MaterialDescriptors.DefaultGlossiness, Assert.IsType<ComputeFloat>(Assert.IsType<MaterialGlossinessMapFeature>(a.MicroSurface).GlossinessMap).Value);
    }

    [Fact]
    public void TexturedPutsTheTextureInTheDiffuseSlotScaledByTheTiling()
    {
        // A null texture is fine for the descriptor: the node holds a reference, the generator reads it
        var a = MaterialDescriptors.Textured(null!, tiling: 4f).Attributes;
        var node = Assert.IsType<ComputeTextureColor>(Assert.IsType<MaterialDiffuseMapFeature>(a.Diffuse).DiffuseMap);

        Assert.Equal(new Vector2(4f), node.Scale);
        Assert.Equal(Vector2.Zero, node.Offset);
        Assert.IsType<MaterialSpecularMicrofacetEnvironmentGGXPolynomial>(Assert.IsType<MaterialSpecularMicrofacetModelFeature>(a.SpecularModel).Environment);
    }

    [Fact]
    public void EmissiveLightsTheColourAndEmitsItAtTheIntensityWithNoSpecular()
    {
        var a = MaterialDescriptors.Emissive(Color.Orange, intensity: 6f).Attributes;
        var emissive = Assert.IsType<MaterialEmissiveMapFeature>(a.Emissive);

        Assert.Equal(Color.Orange, Assert.IsType<ComputeColor>(emissive.EmissiveMap).Value);
        Assert.Equal(6f, Assert.IsType<ComputeFloat>(emissive.Intensity).Value);
        Assert.IsType<MaterialDiffuseLambertModelFeature>(a.DiffuseModel);
        Assert.Null(a.Specular);
        Assert.Null(a.SpecularModel);
    }

    [Fact]
    public void ScreenIsAnUnlitClampedTexture()
    {
        var a = MaterialDescriptors.Screen(null!).Attributes;
        var node = Assert.IsType<ComputeTextureColor>(Assert.IsType<MaterialEmissiveMapFeature>(a.Emissive).EmissiveMap);

        Assert.Equal(Stride.Graphics.TextureAddressMode.Clamp, node.AddressModeU);
        Assert.Equal(Stride.Graphics.TextureAddressMode.Clamp, node.AddressModeV);
        Assert.Null(a.Diffuse);
        Assert.Null(a.DiffuseModel);
        Assert.Null(a.SpecularModel);
    }

    [Fact]
    public void FlatHasNoSpecularAtAll()
    {
        var a = MaterialDescriptors.Flat(Color.White).Attributes;

        Assert.NotNull(a.Emissive);
        Assert.NotNull(a.Diffuse);
        Assert.Null(a.Specular);
        Assert.Null(a.SpecularModel);
    }
}
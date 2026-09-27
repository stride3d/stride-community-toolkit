using Stride.CommunityToolkit.Rendering;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// The shader source each stream fills the forward effect's filter hook with, checked without a
/// device: the engine's stream-shading shader takes the stream's name, the channels and the signed
/// remap as generics, and the world normal has a shader of its own. The feature itself needs a
/// render system, so that half is exercised by the material gallery's view list.
/// </summary>
public class MaterialStreamViewTests
{
    [Theory]
    [InlineData(MaterialStream.Diffuse, "matDiffuse", "rgb", false)]
    [InlineData(MaterialStream.ColorBase, "matColorBase", "rgb", false)]
    [InlineData(MaterialStream.Specular, "matSpecular", "rgb", false)]
    [InlineData(MaterialStream.Glossiness, "matGlossiness", "rrr", false)]
    [InlineData(MaterialStream.NormalTangent, "matNormal", "rgb", true)]
    [InlineData(MaterialStream.Occlusion, "matAmbientOcclusion", "rrr", false)]
    [InlineData(MaterialStream.Cavity, "matCavity", "rrr", false)]
    [InlineData(MaterialStream.Emissive, "matEmissive", "rgb", false)]
    public void AStreamFillsTheHookWithTheStreamShadingShader(MaterialStream stream, string name, string channels, bool remapSigned)
    {
        var filter = MaterialStreamView.FilterFor(stream);

        // A shader source keeps its generic arguments as the text the mixer will paste in; a bool is lower-case
        Assert.Equal("MaterialSurfaceStreamShading", filter.ClassName);
        Assert.Equal([name, channels, remapSigned ? "true" : "false"], filter.GenericArguments);
    }

    [Fact]
    public void TheWorldNormalHasAShaderOfItsOwn()
    {
        var filter = MaterialStreamView.FilterFor(MaterialStream.NormalWorld);

        Assert.Equal("MaterialSurfaceNormalStreamShading", filter.ClassName);
        Assert.True(filter.GenericArguments is null or []);
    }

    [Fact]
    public void EveryStreamHasANameAndAFilter()
    {
        foreach (var stream in MaterialStreamView.All)
        {
            Assert.NotEmpty(MaterialStreamView.DisplayName(stream));
            Assert.NotNull(MaterialStreamView.FilterFor(stream));
        }
    }
}
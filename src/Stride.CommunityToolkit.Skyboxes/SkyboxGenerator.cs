using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Data;
using Stride.Rendering.ComputeEffect.GGXPrefiltering;
using Stride.Rendering.ComputeEffect.LambertianPrefiltering;
using Stride.Rendering.Skyboxes;
using Stride.Shaders;

namespace Stride.CommunityToolkit.Skyboxes;

/// <summary>
/// Provides functionality to generate a skybox with diffuse and specular lighting parameters from a given texture.
/// </summary>
/// <remarks>
/// This class handles the conversion of a texture to a cubemap, performs spherical harmonics filtering for diffuse lighting,
/// and prefilters the texture for specular lighting using GGX reflection. The original logic is from Stride.Assets.Skyboxes
/// </remarks>
public static class SkyboxGenerator
{
    /// <summary>
    /// Generates a skybox using the provided texture and context, applying both diffuse and specular lighting.
    /// </summary>
    /// <param name="skybox">The skybox instance to apply the generated parameters to.</param>
    /// <param name="context">The context required for rendering, which includes services and draw context.</param>
    /// <param name="skyboxTexture">The texture used to generate the skybox cubemap.</param>
    /// <returns>The modified <see cref="Skybox"/> with diffuse and specular lighting applied.</returns>
    /// <remarks>
    /// The method performs the following:
    /// <list type="number">
    ///   <item>Converts the provided texture into a cubemap with a computed resolution based on the texture width.</item>
    ///   <item>Applies Lambertian spherical harmonics filtering for diffuse lighting.</item>
    ///   <item>Performs GGX prefiltering for specular lighting and generates a cubemap for reflection purposes.</item>
    /// </list>
    /// </remarks>
    public static Skybox Generate(Skybox skybox, SkyboxGeneratorContext context, Texture skyboxTexture)
    {
        var cubemapSize = (int)Math.Pow(2, Math.Ceiling(Math.Log(skyboxTexture.Width / 4) / Math.Log(2))); // maximum resolution is around horizontal middle line which composes 4 images.

        if (skyboxTexture.Dimension != TextureDimension.TextureCube)
        {
            skyboxTexture = CubemapFromTextureRenderer.GenerateCubemap(context.Services, context.RenderDrawContext, skyboxTexture, cubemapSize);
        }

        using var lamberFiltering = new LambertianPrefilteringSHNoCompute(context.RenderContext)
        {
            HarmonicOrder = 3,
            RadianceMap = skyboxTexture
        };
        lamberFiltering.Draw(context.RenderDrawContext);

        var coefficients = lamberFiltering.PrefilteredLambertianSH.Coefficients;
        for (int i = 0; i < coefficients.Length; i++)
            coefficients[i] *= SphericalHarmonics.BaseCoefficients[i];

        skybox.DiffuseLightingParameters.Set(SkyboxKeys.Shader, new ShaderClassSource("SphericalHarmonicsEnvironmentColor", lamberFiltering.HarmonicOrder));
        skybox.DiffuseLightingParameters.Set(SphericalHarmonicsEnvironmentColorKeys.SphericalColors, coefficients);

        using var specularRadiancePrefilterGGX = new RadiancePrefilteringGGXNoCompute(context.RenderContext);

        //var textureSize = asset.SpecularCubeMapSize <= 0 ? 64 : asset.SpecularCubeMapSize;
        // Not sure what should be here
        var textureSize = 64;

        textureSize = (int)Math.Pow(2, Math.Round(Math.Log(textureSize, 2)));
        if (textureSize < 64) textureSize = 64;

        var filteringTextureFormat = skyboxTexture.Format.IsHDR() ? skyboxTexture.Format : PixelFormat.R8G8B8A8_UNorm;

        using (var outputTexture = Texture.New2D(context.GraphicsDevice, textureSize, textureSize, true, filteringTextureFormat, TextureFlags.ShaderResource | TextureFlags.RenderTarget, 6))
        {
            specularRadiancePrefilterGGX.RadianceMap = skyboxTexture;
            specularRadiancePrefilterGGX.PrefilteredRadiance = outputTexture;
            //specularRadiancePrefilterGGX.Draw(context.RenderDrawContext);

            var cubeTexture = Texture.NewCube(context.GraphicsDevice, textureSize, true, filteringTextureFormat);
            context.RenderDrawContext.CommandList.Copy(outputTexture, cubeTexture);

            cubeTexture.SetSerializationData(cubeTexture.GetDataAsImage(context.RenderDrawContext.CommandList));

            skybox.SpecularLightingParameters.Set(SkyboxKeys.Shader, new ShaderClassSource("RoughnessCubeMapEnvironmentColor"));
            skybox.SpecularLightingParameters.Set(SkyboxKeys.CubeMap, cubeTexture);
        }

        return skybox;
    }
}
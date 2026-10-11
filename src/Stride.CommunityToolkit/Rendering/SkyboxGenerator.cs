using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.ComputeEffect.GGXPrefiltering;
using Stride.Rendering.ComputeEffect.LambertianPrefiltering;
using Stride.Rendering.Skyboxes;
using Stride.Shaders;

namespace Stride.CommunityToolkit.Rendering;

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
    /// Smallest specular cubemap Stride's own skybox pipeline will produce.
    /// </summary>
    private const int MinimumSpecularCubeMapSize = 64;

    /// <summary>
    /// Generates a skybox using the provided texture and context, applying both diffuse and specular lighting.
    /// </summary>
    /// <param name="skybox">The skybox instance to apply the generated parameters to.</param>
    /// <param name="context">The context required for rendering, which includes services and draw context.</param>
    /// <param name="skyboxTexture">The texture used to generate the skybox cubemap. Must be a cubemap or a 2D texture.</param>
    /// <param name="specularCubeMapSize">
    /// Size of the prefiltered specular cubemap, in pixels. Leave <see langword="null"/> to match the
    /// source cubemap, which is the default and is usually what you want - see the remarks. Any value
    /// given is rounded to the nearest power of two and floored at 64.
    /// </param>
    /// <returns>The modified <see cref="Skybox"/> with diffuse and specular lighting applied.</returns>
    /// <exception cref="ArgumentNullException">If any argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">If <paramref name="skyboxTexture"/> is neither a cubemap nor a 2D texture.</exception>
    /// <remarks>
    /// <para>
    /// The method performs the following:
    /// <list type="number">
    ///   <item>Converts the provided texture into a cubemap with a computed resolution based on the texture width.</item>
    ///   <item>Applies Lambertian spherical harmonics filtering for diffuse lighting.</item>
    ///   <item>Performs GGX prefiltering for specular lighting and generates a cubemap for reflection purposes.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Why the specular cubemap defaults to the source size.</b> Stride's own asset pipeline defaults
    /// this to 64, but that is not a free choice here.
    /// <c>RadiancePrefilteringGGXNoCompute</c> only copies the sharpest mip straight across when
    /// <c>Log2(input.Width / output.Width) &lt; input.MipLevelCount</c>; otherwise it rescales through
    /// an image scaler instead. A source cubemap with no mip chain has a level count of one, so that
    /// condition holds only when the two widths are equal. Matching the source keeps the roughness-zero
    /// mirror level pixel-exact; shrinking it resamples that level and visibly softens reflections.
    /// Pass a smaller size deliberately when a large source would otherwise cost more memory than the
    /// reflection is worth.
    /// </para>
    /// <para>
    /// <b>Ownership.</b> The returned skybox holds a GPU cubemap that is not released automatically and
    /// does not belong to any content manager. Generating skyboxes repeatedly - once per scene load,
    /// say - leaks one each time. To release it:
    /// <code>
    /// var cubeMap = skybox.SpecularLightingParameters.Get(SkyboxKeys.CubeMap);
    /// cubeMap?.Dispose();
    /// </code>
    /// </para>
    /// </remarks>
    public static Skybox Generate(Skybox skybox, SkyboxGeneratorContext context, Texture skyboxTexture, int? specularCubeMapSize = null)
    {
        ArgumentNullException.ThrowIfNull(skybox);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(skyboxTexture);

        // Tracked separately so the intermediate can be released: it is only an input to the two
        // filters below, and once they have drawn, nothing refers to it again
        Texture? generatedCubemap = null;

        if (skyboxTexture.ViewDimension == TextureDimension.Texture2D)
        {
            // Maximum resolution is around the horizontal middle line, which composes 4 images.
            // Clamped at 1 so a very small source cannot reach Math.Log(0).
            var cubemapSize = (int)Math.Pow(2, Math.Ceiling(Math.Log2(Math.Max(1, skyboxTexture.Width / 4))));

            generatedCubemap = CubemapFromTextureRenderer.GenerateCubemap(context.Services, context.RenderDrawContext, skyboxTexture, cubemapSize);
            skyboxTexture = generatedCubemap;
        }
        else if (skyboxTexture.ViewDimension != TextureDimension.TextureCube)
        {
            // Rejected here rather than passed on, because the cubemap renderer fails much further
            // down with an error that says nothing about the texture that caused it
            throw new ArgumentException(
                $"A skybox texture must be a cubemap or a 2D texture, but this one is {skyboxTexture.ViewDimension}.",
                nameof(skyboxTexture));
        }

        try
        {
            ApplyDiffuseLighting(skybox, context, skyboxTexture);
            ApplySpecularLighting(skybox, context, skyboxTexture, ResolveSpecularSize(specularCubeMapSize, skyboxTexture.Width));
        }
        finally
        {
            generatedCubemap?.Dispose();
        }

        return skybox;
    }

    /// <summary>
    /// Computes the spherical harmonics that stand in for diffuse environment lighting.
    /// </summary>
    private static void ApplyDiffuseLighting(Skybox skybox, SkyboxGeneratorContext context, Texture skyboxTexture)
    {
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
    }

    /// <summary>
    /// Prefilters the radiance into a roughness mip chain used for specular reflections.
    /// </summary>
    private static void ApplySpecularLighting(Skybox skybox, SkyboxGeneratorContext context, Texture skyboxTexture, int size)
    {
        var filteringTextureFormat = skyboxTexture.Format.IsHDR ? skyboxTexture.Format : PixelFormat.R8G8B8A8_UNorm;

        using (var specularRadiancePrefilterGGX = new RadiancePrefilteringGGXNoCompute(context.RenderContext))
        using (var outputTexture = Texture.New2D(context.GraphicsDevice, size, size, true, filteringTextureFormat, TextureFlags.ShaderResource | TextureFlags.RenderTarget, 6))
        {
            specularRadiancePrefilterGGX.RadianceMap = skyboxTexture;
            specularRadiancePrefilterGGX.PrefilteredRadiance = outputTexture;
            specularRadiancePrefilterGGX.Draw(context.RenderDrawContext);

            // Deliberately not disposed - it is handed to the skybox below and has to outlive this
            // method. See the ownership note on Generate.
            var filteredCubeMap = Texture.NewCube(context.GraphicsDevice, size, MipMapCount.Auto, filteringTextureFormat, TextureFlags.ShaderResource);

            context.RenderDrawContext.CommandList.Copy(outputTexture, filteredCubeMap);

            skybox.SpecularLightingParameters.Set(SkyboxKeys.Shader, new ShaderClassSource("RoughnessCubeMapEnvironmentColor"));
            skybox.SpecularLightingParameters.Set(SkyboxKeys.CubeMap, filteredCubeMap);
        }
    }

    /// <summary>
    /// Resolves the requested specular size, defaulting to the source width.
    /// </summary>
    private static int ResolveSpecularSize(int? requested, int sourceWidth)
    {
        if (requested is not { } size) return sourceWidth;

        // Rounded to a power of two and floored at 64, matching Stride's own skybox pipeline
        size = (int)Math.Pow(2, Math.Round(Math.Log2(Math.Max(1, size))));

        return Math.Max(MinimumSpecularCubeMapSize, size);
    }
}
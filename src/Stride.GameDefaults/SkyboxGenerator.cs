namespace Stride.GameDefaults;

// Taken from Stride.Assets.Skyboxes
public static class SkyboxGenerator
{
    public static Skybox Generate(Skybox skybox, SkyboxGeneratorContext context, Texture skyboxTexture)
    {
        var cubemapSize = (int)Math.Pow(2, Math.Ceiling(Math.Log(skyboxTexture.Width / 4) / Math.Log(2))); // maximum resolution is around horizontal middle line which composes 4 images.

        skyboxTexture = CubemapFromTextureRenderer.GenerateCubemap(context.Services, context.RenderDrawContext, skyboxTexture, cubemapSize);

        var lamberFiltering = new LambertianPrefilteringSHNoCompute(context.RenderContext)
        {
            HarmonicOrder = 3,
            RadianceMap = skyboxTexture
        };
        lamberFiltering.Draw(context.RenderDrawContext);

        var coefficients = lamberFiltering.PrefilteredLambertianSH.Coefficients;
        for (int i = 0; i < coefficients.Length; i++)
        {
            coefficients[i] *= SphericalHarmonics.BaseCoefficients[i];
        }

        skybox.DiffuseLightingParameters.Set(SkyboxKeys.Shader, new ShaderClassSource("SphericalHarmonicsEnvironmentColor", lamberFiltering.HarmonicOrder));
        skybox.DiffuseLightingParameters.Set(SphericalHarmonicsEnvironmentColorKeys.SphericalColors, coefficients);

        var specularRadiancePrefilterGGX = new RadiancePrefilteringGGXNoCompute(context.RenderContext);

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
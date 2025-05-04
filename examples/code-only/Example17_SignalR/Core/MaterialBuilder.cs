using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Example17_SignalR.Core;

public class MaterialBuilder(GraphicsDevice graphicsDevice)
{
    private readonly GraphicsDevice _graphicsDevice = graphicsDevice;

    public Material CreateMaterial(Color? color = null, float specular = 1.0f, float microSurface = 0.65f)
    {
        var materialDescription2 = new MaterialDescriptor
        {
            Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(color ?? GameDefaults.DefaultMaterialColor)),
                    //DiffuseModel = new MaterialLightmapModelFeature(),
                    Specular =  new MaterialMetalnessMapFeature(new ComputeFloat(specular)),
                    SpecularModel = new MaterialSpecularMicrofacetModelFeature(),
                    MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(microSurface))
                }
        };

        var materialDescription = new MaterialDescriptor
        {
            Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(color ?? GameDefaults.DefaultMaterialColor)),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Specular =  new MaterialMetalnessMapFeature(new ComputeFloat(0)),
                    SpecularModel = new MaterialSpecularMicrofacetModelFeature()
                    {
                        Fresnel = new MaterialSpecularMicrofacetFresnelSchlick(),
                        Visibility = new MaterialSpecularMicrofacetVisibilitySmithSchlickGGX(),
                        NormalDistribution = new MaterialSpecularMicrofacetNormalDistributionGGX(),
                        Environment = new MaterialSpecularMicrofacetEnvironmentGGXLUT(),
                    },
                    MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0)),
                }
        };

        return Material.New(_graphicsDevice, materialDescription);
    }
}

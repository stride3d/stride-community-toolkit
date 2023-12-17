using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.CommunityToolkit.Rendering.Gizmos;

public static class GizmoEmissiveColorMaterial
{
    public static Material Create(GraphicsDevice device, Color color, float intensity = 1f)
    {
        var material = Material.New(device, new MaterialDescriptor
        {
            Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor()),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Emissive = new MaterialEmissiveMapFeature(new ComputeColor())
                }
        });

        // set the color to the material
        UpdateColor(device, material, color, intensity);

        // set the transparency property to the material if necessary
        if (color.A < byte.MaxValue)
        {
            material.Passes[0].HasTransparency = true;
        }

        return material;
    }

    public static void UpdateColor(GraphicsDevice device, Material material, Color color, float intensity = 1f)
    {
        // set the color to the material
        material.Passes[0].Parameters.Set(MaterialKeys.DiffuseValue, new Color4(color).ToColorSpace(device.ColorSpace));
        material.Passes[0].Parameters.Set(MaterialKeys.EmissiveIntensity, intensity);
        material.Passes[0].Parameters.Set(MaterialKeys.EmissiveValue, new Color4(color).ToColorSpace(device.ColorSpace));
    }
}

public static class GizmoUniformColorMaterial
{
    public static Material Create(GraphicsDevice device, Color color)
    {
        var descriptor = new MaterialDescriptor();
        descriptor.Attributes.Diffuse = new MaterialDiffuseMapFeature(new ComputeColor());
        descriptor.Attributes.DiffuseModel = new MaterialDiffuseLambertModelFeature();

        var material = Material.New(device, descriptor);

        // set the color to the material
        UpdateColor(device, material, color);

        // set the transparency property to the material if necessary
        if (color.A < Byte.MaxValue)
        {

            material.Passes[0].HasTransparency = true;
            // TODO GRAPHICS REFACTOR
            //material.Parameters.SetResourceSlow(Graphics.Effect.BlendStateKey, device.BlendStates.NonPremultiplied);
        }

        return material;
    }

    public static void UpdateColor(GraphicsDevice device, Material material, Color color)
    {
        // set the color to the material
        material.Passes[0].Parameters.Set(MaterialKeys.DiffuseValue, new Color4(color).ToColorSpace(device.ColorSpace));
    }
}
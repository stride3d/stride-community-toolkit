using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.CommunityToolkit.Rendering.Gizmos;

/// <summary>
/// A utility class for creating and updating materials with emissive color properties for gizmos.
/// </summary>
public static class GizmoEmissiveColorMaterial
{
    /// <summary>
    /// Creates a new material with emissive color and diffuse properties based on the specified color and intensity.
    /// </summary>
    /// <param name="device">The <see cref="GraphicsDevice"/> used to create the material.</param>
    /// <param name="color">The <see cref="Color"/> to apply to the material.</param>
    /// <param name="intensity">The intensity of the emissive color. Defaults to 1f.</param>
    /// <returns>A new <see cref="Material"/> with the specified emissive color and intensity.</returns>
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

        // Set the color to the material
        UpdateColor(device, material, color, intensity);

        // Set the transparency property to the material if necessary
        if (color.A < byte.MaxValue)
        {
            material.Passes[0].HasTransparency = true;
        }

        return material;
    }

    /// <summary>
    /// Updates the color and emissive properties of an existing material.
    /// </summary>
    /// <param name="device">The <see cref="GraphicsDevice"/> used to update the color.</param>
    /// <param name="material">The <see cref="Material"/> to be updated.</param>
    /// <param name="color">The <see cref="Color"/> to apply to the material.</param>
    /// <param name="intensity">The intensity of the emissive color. Defaults to 1f.</param>
    private static void UpdateColor(GraphicsDevice device, Material material, Color color, float intensity = 1f)
    {
        material.Passes[0].Parameters.Set(MaterialKeys.DiffuseValue, new Color4(color).ToColorSpace(device.ColorSpace));
        material.Passes[0].Parameters.Set(MaterialKeys.EmissiveIntensity, intensity);
        material.Passes[0].Parameters.Set(MaterialKeys.EmissiveValue, new Color4(color).ToColorSpace(device.ColorSpace));
    }
}
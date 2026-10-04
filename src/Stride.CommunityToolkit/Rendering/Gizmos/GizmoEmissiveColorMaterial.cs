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
    /// <remarks>
    /// A color with an alpha below 255 produces a translucent material: the descriptor gains a
    /// <see cref="MaterialTransparencyBlendFeature"/>, the emissive feature is told to take the material alpha from
    /// its own alpha channel, and the color values are premultiplied. All three are needed - Stride blends
    /// transparent materials with premultiplied alpha, and without the blend feature the alpha is simply ignored,
    /// so the mesh renders fully opaque or, for a low alpha, appears to vanish. Opaque colors stay in the
    /// opaque render stage.
    /// </remarks>
    /// <param name="device">The <see cref="GraphicsDevice"/> used to create the material.</param>
    /// <param name="color">The <see cref="Color"/> to apply to the material.</param>
    /// <param name="intensity">The intensity of the emissive color. Defaults to 1f.</param>
    /// <returns>A new <see cref="Material"/> with the specified emissive color and intensity.</returns>
    public static Material Create(GraphicsDevice device, Color color, float intensity = 1f)
    {
        var isTransparent = color.A < byte.MaxValue;

        var descriptor = new MaterialDescriptor
        {
            Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor()),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Emissive = new MaterialEmissiveMapFeature(new ComputeColor()) { UseAlpha = isTransparent }
                }
        };

        // The blend feature is what makes the alpha mean anything; it also sets HasTransparency on the pass
        if (isTransparent)
        {
            descriptor.Attributes.Transparency = new MaterialTransparencyBlendFeature();
        }

        var material = Material.New(device, descriptor);

        // Set the color to the material
        UpdateColor(device, material, color, intensity);

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
        var value = new Color4(color).ToColorSpace(device.ColorSpace);

        // Premultiplied alpha, in the space the shader works in: the emissive and lit contributions are added to
        // the shading color without being scaled by alpha, and the blend state expects them already scaled
        if (color.A < byte.MaxValue)
        {
            value = Color4.PremultiplyAlpha(value);
        }

        material.Passes[0].Parameters.Set(MaterialKeys.DiffuseValue, value);
        material.Passes[0].Parameters.Set(MaterialKeys.EmissiveIntensity, intensity);
        material.Passes[0].Parameters.Set(MaterialKeys.EmissiveValue, value);
    }
}
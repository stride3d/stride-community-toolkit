using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.CommunityToolkit.Rendering.Gizmos;

/// <summary>
/// A utility class for creating and updating materials with uniform color properties for gizmos.
/// </summary>
public static class GizmoUniformColorMaterial
{
    /// <summary>
    /// Creates a new material with uniform diffuse color properties based on the specified color.
    /// </summary>
    /// <param name="device">The <see cref="GraphicsDevice"/> used to create the material.</param>
    /// <param name="color">The <see cref="Color"/> to apply to the material.</param>
    /// <returns>A new <see cref="Material"/> with the specified uniform diffuse color.</returns>
    /// <remarks>
    /// If the color contains transparency (alpha &lt; 255), transparency is enabled for the material.
    /// </remarks>
    public static Material Create(GraphicsDevice device, Color color)
    {
        var descriptor = new MaterialDescriptor();
        descriptor.Attributes.Diffuse = new MaterialDiffuseMapFeature(new ComputeColor());
        descriptor.Attributes.DiffuseModel = new MaterialDiffuseLambertModelFeature();

        var material = Material.New(device, descriptor);

        // Set the color to the material
        UpdateColor(device, material, color);

        // Set the transparency property to the material if necessary
        if (color.A < byte.MaxValue)
        {

            material.Passes[0].HasTransparency = true;
            // TODO GRAPHICS REFACTOR
            //material.Parameters.SetResourceSlow(Graphics.Effect.BlendStateKey, device.BlendStates.NonPremultiplied);
        }

        return material;
    }

    /// <summary>
    /// Updates the color of an existing material.
    /// </summary>
    /// <param name="device">The <see cref="GraphicsDevice"/> used to update the material.</param>
    /// <param name="material">The <see cref="Material"/> to be updated.</param>
    /// <param name="color">The <see cref="Color"/> to apply to the material.</param>
    private static void UpdateColor(GraphicsDevice device, Material material, Color color)
    {
        material.Passes[0].Parameters.Set(MaterialKeys.DiffuseValue, new Color4(color).ToColorSpace(device.ColorSpace));
    }
}
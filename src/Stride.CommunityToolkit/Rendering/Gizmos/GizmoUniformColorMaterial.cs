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
        var descriptor = new MaterialDescriptor
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor()),
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            }
        };

        var material = Material.New(device, descriptor);

        material.Passes[0].Parameters.Set(MaterialKeys.DiffuseValue, new Color4(color).ToColorSpace(device.ColorSpace));
        material.Passes[0].HasTransparency = color.A < byte.MaxValue;

        return material;
    }
}
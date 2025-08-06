using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example09_Renderer;

/// <summary>
/// Represents a component that enables an outline effect on a mesh entity.
/// Attach this component to an entity with a model to render an outline using the MeshOutlineRenderFeature.
/// </summary>
/// <remarks>
/// The outline effect is controlled by the <see cref="Enabled"/>, <see cref="Color"/>, and <see cref="Intensity"/> properties.
/// Ensure that MeshOutlineRenderFeature is enabled in the Graphics Compositor for this effect to be visible.
/// </remarks>
[DataContract("MeshOutline")]
[Display("Outline", null, Expand = ExpandRule.Once)]
[ComponentCategory("Model")]
public class MeshOutlineComponent : EntityComponent
{
    /// <summary>
    /// Gets or sets a value indicating whether the mesh outline effect is enabled for this entity.
    /// </summary>
    /// <remarks>
    /// Set to <c>true</c> to render the outline; <c>false</c> to disable it.
    /// </remarks>
    [DataMember(10)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the color of the outline effect.
    /// </summary>
    /// <remarks>
    /// The color is applied to the outline rendered around the mesh. Use <see cref="Color4"/> for RGBA values.
    /// </remarks>
    [DataMember(30)]
    public Color4 Color { get; set; } = new Color4();

    /// <summary>
    /// Gets or sets the intensity of the outline color.
    /// </summary>
    /// <remarks>
    /// Expected range is from 0.0 (transparent) to 1.0 (fully opaque). Values outside this range may result in unintended visual effects.
    /// </remarks>
    [DataMember(40)]
    public float Intensity { get; set; } = 1.0f;
}
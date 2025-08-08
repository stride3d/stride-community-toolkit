using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example18_Box2DPhysics;

/// <summary>
/// Represents a component that enables an outline effect on a mesh entity.
/// Attach this component to an entity with a model to render an outline using the MeshOutlineRenderFeature.
/// </summary>
/// <remarks>
/// The outline effect is controlled by the <see cref="Enabled"/>, <see cref="Color"/>, and <see cref="Intensity"/> properties.
/// Ensure that MeshOutlineRenderFeature is enabled in the Graphics Compositor for this effect to be visible.
/// </remarks>
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
    /// Values greater than 1.0 will increase brightness. Typical range is 0.0 (transparent) and above.
    /// </remarks>
    [DataMember(40)]
    public float Intensity { get; set; } = 1.0f;

    public Primitive2DModelType ShapeType { get; set; }

    public float OutlineThickness { get; set; } = 1.0f;

    public float Radius { get; set; }
}
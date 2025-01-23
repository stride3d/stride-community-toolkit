using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Rendering;

namespace Example13_RootRendererShader.Renderers;

[DataContract("RibbonBackgroundComponent")]
[ComponentCategory("Xmb")]
[Display("Ribbon Background", Expand = ExpandRule.Once)]
[DefaultEntityComponentRenderer(typeof(RibbonBackgroundRenderProcessor))]
public class RibbonBackgroundComponent : ActivableEntityComponent
{
    /// <summary>
    /// Create an empty Background component.
    /// </summary>
    public RibbonBackgroundComponent()
    {
        Intensity = 1f;
    }

    /// <summary>
    /// Gets or sets the intensity.
    /// </summary>
    /// <value>The intensity.</value>
    /// <userdoc>The intensity of the background color</userdoc>
    [DataMemberRange(0.0, 100.0, 0.01f, 1.0f, 2)]
    public float Intensity { get; set; } = 1f;

    [DataMemberRange(0.0, 5.0, 0.01f, 1.0f, 2)]
    public float Frequency { get; set; } = 1f;

    [DataMemberRange(0.0, 2.0, 0.01f, 1.0f, 2)]
    public float Amplitude { get; set; } = 1f;

    [DataMemberRange(0.0, 5.0, 0.01f, 1.0f, 2)]
    public float Speed { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the top.
    /// </summary>
    public Color3 Top { get; set; } = Color.Blue.ToColor3();

    /// <summary>
    /// Gets or sets the bottom.
    /// </summary>
    public Color3 Bottom { get; set; } = Color.BlueViolet.ToColor3();

    /// <summary>
    /// Gets or sets the width factor.
    /// </summary>
    public float WidthFactor { get; set; } = 0.5f;

    /// <summary>
    /// The render group for this component.
    /// </summary>
    [Display("Render group")]
    public RenderGroup RenderGroup { get; set; } = RenderGroup.Group0;
}
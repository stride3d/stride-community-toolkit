using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Rendering;

namespace E09_3D_RootRendererShader.Renderers;

[DataContract(nameof(RibbonBackgroundComponent))]
[ComponentCategory("Xmb")]
[Display("Ribbon Background", Expand = ExpandRule.Once)]
[DefaultEntityComponentRenderer(typeof(RibbonBackgroundRenderProcessor))]
public class RibbonBackgroundComponent : ActivableEntityComponent
{
    /// <summary>
    /// Gets or sets the intensity.
    /// </summary>
    /// <userdoc>The intensity of the background colour</userdoc>
    [DataMemberRange(0.0, 100.0, 0.01f, 1.0f, 2)]
    public float Intensity { get; set; } = 1f;

    [DataMemberRange(0.0, 5.0, 0.01f, 1.0f, 2)]
    public float Frequency { get; set; } = 1f;

    [DataMemberRange(0.0, 2.0, 0.01f, 1.0f, 2)]
    public float Amplitude { get; set; } = 1f;

    [DataMemberRange(0.0, 5.0, 0.01f, 1.0f, 2)]
    public float Speed { get; set; } = 1f;

    public Color3 Top { get; set; } = Color.Blue.ToColor3();

    public Color3 Bottom { get; set; } = Color.BlueViolet.ToColor3();

    public float WidthFactor { get; set; } = 0.5f;

    [Display("Render group")]
    public RenderGroup RenderGroup { get; set; } = RenderGroup.Group0;
}
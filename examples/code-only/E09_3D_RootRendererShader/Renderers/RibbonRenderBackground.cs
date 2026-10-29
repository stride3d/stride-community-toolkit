using Stride.Core.Mathematics;
using Stride.Rendering;

namespace E09_3D_RootRendererShader.Renderers;

/// <summary>
/// The per-frame snapshot the render feature draws from: <see cref="RibbonBackgroundRenderProcessor"/>
/// copies the component's values onto it each frame, and
/// <see cref="RibbonBackgroundRenderFeature"/> passes them to the shader.
/// </summary>
/// <remarks>Properties rather than public fields, so the access can be breakpointed.</remarks>
public class RibbonRenderBackground : RenderObject
{
    public float Intensity { get; set; }
    public float Frequency { get; set; }
    public float Amplitude { get; set; }
    public float Speed { get; set; }

    public Vector3 Top { get; set; }
    public Vector3 Bottom { get; set; }
    public float WidthFactor { get; set; }
}
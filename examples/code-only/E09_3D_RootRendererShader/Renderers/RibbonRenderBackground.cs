using Stride.Core.Mathematics;
using Stride.Rendering;

namespace E09_3D_RootRendererShader.Renderers;

/// <summary>
/// The per-frame snapshot the render feature draws from: <see cref="RibbonBackgroundRenderProcessor"/>
/// copies the component's values onto it each frame, and
/// <see cref="RibbonBackgroundRenderFeature"/> reads them back out into the shader's parameters.
/// </summary>
/// <remarks>
/// These are properties rather than public fields because the two types that fill and read them live
/// outside this class, so the values have to be reachable from outside - and a property keeps that
/// access behind something you can breakpoint.
/// </remarks>
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
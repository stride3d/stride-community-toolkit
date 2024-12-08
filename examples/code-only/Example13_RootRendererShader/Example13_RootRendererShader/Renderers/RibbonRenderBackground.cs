using Stride.Core.Mathematics;
using Stride.Rendering;

namespace Example13_RootRendererShader.Renderers;
public class RibbonRenderBackground : RenderObject
{
    public float Intensity;
    public float Frequency;
    public float Amplitude;
    public float Speed;

    public Vector3 Top;
    public Vector3 Bottom;
    public float WidthFactor;
}

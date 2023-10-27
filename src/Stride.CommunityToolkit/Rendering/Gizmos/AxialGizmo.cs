using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.CommunityToolkit.Rendering.Gizmos;

public abstract class AxialGizmo
{
    /// <summary>
    /// Gets or sets the entity group of the Gizmo
    /// </summary>
    public RenderGroup RenderGroup { get; protected set; } = RenderGroup.Group0;

    /// <summary>
    /// Gets the graphics device.
    /// </summary>
    /// <value>The graphics device.</value>
    protected GraphicsDevice GraphicsDevice { get; private set; }

    protected const int GizmoTessellation = 64;
    protected const float GizmoExtremitySize = 0.15f; // the size of the object placed at the extremity of the gizmo axes
    protected const float GizmoOriginScale = 1.33f; // the scale of the object placed at the origin in comparison to the extremity object
    protected const float GizmoPlaneLength = 0.25f; // the size of the gizmo small planes defining transformation along planes.
    protected const float GizmoDefaultSize = 133f; // the default size of the gizmo on the screen in pixels.

    private static readonly Color RedUniformColor = new(0xFC, 0x37, 0x37);
    private static readonly Color GreenUniformColor = new(0x32, 0xE3, 0x35);
    private static readonly Color BlueUniformColor = new(0x2F, 0x6A, 0xE1);

    protected Material? DefaultOriginMaterial { get; private set; }

    /// <summary>
    /// A uniform red material
    /// </summary>
    protected Material? RedUniformMaterial { get; private set; }

    /// <summary>
    /// A uniform green material
    /// </summary>
    protected Material? GreenUniformMaterial { get; private set; }

    /// <summary>
    /// A uniform blue material
    /// </summary>
    protected Material? BlueUniformMaterial { get; private set; }

    protected virtual Entity? Create()
    {
        if (GraphicsDevice == null)
        {
            throw new InvalidOperationException("GraphicsDevice has not been initialized");
        }

        DefaultOriginMaterial = CreateEmissiveColorMaterial(Color.White);
        RedUniformMaterial = CreateEmissiveColorMaterial(RedUniformColor);
        GreenUniformMaterial = CreateEmissiveColorMaterial(GreenUniformColor);
        BlueUniformMaterial = CreateEmissiveColorMaterial(BlueUniformColor);

        return null;
    }

    protected AxialGizmo(GraphicsDevice graphicsDevice) => GraphicsDevice = graphicsDevice;

    /// <summary>
    /// Creates an emissive color material.
    /// </summary>
    /// <param name="color">The color of the material</param>
    /// <returns>the material</returns>
    protected Material CreateEmissiveColorMaterial(Color color)
    {
        return GizmoEmissiveColorMaterial.Create(GraphicsDevice, color, 0.75f);
    }
}

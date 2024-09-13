using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.CommunityToolkit.Rendering.Gizmos;

/// <summary>
/// Represents a base class for creating an Axial Gizmo, which visually represents 3D axes in the scene.
/// The Gizmo consists of three axes (X, Y, Z), each represented by a different color (Red, Green, Blue).
/// This class provides the foundational functionality for rendering and configuring the visual aspects of the Gizmo.
/// </summary>
public abstract class AxialGizmo
{
    /// <summary>
    /// Gets or sets the render group of the Gizmo. This determines which render group the Gizmo will belong to.
    /// </summary>
    public RenderGroup RenderGroup { get; protected set; } = RenderGroup.Group0;

    /// <summary>
    /// Gets the graphics device used for rendering the Gizmo.
    /// </summary>
    protected GraphicsDevice GraphicsDevice { get; private set; }

    /// <summary>
    /// Defines the tessellation level of the Gizmo, which affects how smoothly the object is rendered.
    /// A higher value results in smoother rendering at the cost of performance.
    /// </summary>
    protected const int GizmoTessellation = 64;

    /// <summary>
    /// The size of the object placed at the extremities of the Gizmo's axes (e.g., arrowheads or markers).
    /// </summary>
    protected const float GizmoExtremitySize = 0.15f;

    /// <summary>
    /// The scale factor of the object placed at the origin of the Gizmo, compared to the objects at the extremities.
    /// Typically, the object at the origin is larger to make it more noticeable.
    /// </summary>
    protected const float GizmoOriginScale = 1.33f;

    /// <summary>
    /// The size of the Gizmo's small planes, which are used for transformations along specific axes or planes.
    /// </summary>
    protected const float GizmoPlaneLength = 0.25f;

    /// <summary>
    /// The default size of the Gizmo when rendered on the screen, measured in pixels. This controls how large the Gizmo appears relative to the screen.
    /// </summary>
    protected const float GizmoDefaultSize = 133f;

    private static readonly Color _redUniformColor = new(0xFC, 0x37, 0x37);
    private static readonly Color _greenUniformColor = new(0x32, 0xE3, 0x35);
    private static readonly Color _blueUniformColor = new(0x2F, 0x6A, 0xE1);

    /// <summary>
    /// Represents the color for the X-axis (Red).
    /// </summary>
    private readonly Color? _redColor;

    /// <summary>
    /// Represents the color for the Y-axis (Green).
    /// </summary>
    private readonly Color? _greenColor;

    /// <summary>
    /// Represents the color for the Z-axis (Blue).
    /// </summary>
    private readonly Color? _blueColor;

    /// <summary>
    /// Gets the default material for the Gizmo's origin, typically represented in white.
    /// </summary>
    protected Material? DefaultOriginMaterial { get; private set; }

    /// <summary>
    /// Gets the material for the X-axis (Red) of the Gizmo.
    /// </summary>
    protected Material? RedUniformMaterial { get; private set; }

    /// <summary>
    /// Gets the material for the Y-axis (Green) of the Gizmo.
    /// </summary>
    protected Material? GreenUniformMaterial { get; private set; }

    /// <summary>
    /// Gets the material for the Z-axis (Blue) of the Gizmo.
    /// </summary>
    protected Material? BlueUniformMaterial { get; private set; }

    /// <summary>
    /// Creates the base components of the Gizmo, such as the default origin and axis materials.
    /// This method should be called in derived classes to initialize the Gizmo's materials.
    /// </summary>
    /// <returns>The <see cref="Entity"/> representing the Gizmo, or <c>null</c> if no entity is created.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="GraphicsDevice"/> is not initialized.</exception>
    protected virtual Entity? Create()
    {
        if (GraphicsDevice == null)
        {
            throw new InvalidOperationException("GraphicsDevice has not been initialized");
        }

        DefaultOriginMaterial = CreateEmissiveColorMaterial(Color.White);
        RedUniformMaterial = CreateEmissiveColorMaterial(GetRedColor());
        GreenUniformMaterial = CreateEmissiveColorMaterial(GetGreenColor());
        BlueUniformMaterial = CreateEmissiveColorMaterial(GetBlueColor());

        return null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AxialGizmo"/> class with optional custom axis colors.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to render the Gizmo.</param>
    /// <param name="redColor">Optional custom color for the X-axis (Red). If null, a default red color is used.</param>
    /// <param name="greenColor">Optional custom color for the Y-axis (Green). If null, a default green color is used.</param>
    /// <param name="blueColor">Optional custom color for the Z-axis (Blue). If null, a default blue color is used.</param>
    protected AxialGizmo(GraphicsDevice graphicsDevice, Color? redColor = null, Color? greenColor = null, Color? blueColor = null)
    {
        GraphicsDevice = graphicsDevice;
        _redColor = redColor;
        _greenColor = greenColor;
        _blueColor = blueColor;
    }

    /// <summary>
    /// Creates a material with an emissive color to visually represent the Gizmo's axes.
    /// </summary>
    /// <param name="color">The color of the material.</param>
    /// <returns>A <see cref="Material"/> instance with the specified emissive color.</returns>
    protected Material CreateEmissiveColorMaterial(Color color)
        => GizmoEmissiveColorMaterial.Create(GraphicsDevice, color, 0.75f);

    /// <summary>
    /// Retrieves the color for the X-axis (Red). Returns the default red color if none is specified.
    /// </summary>
    /// <returns>The color for the X-axis.</returns>
    protected Color GetRedColor() => _redColor ?? _redUniformColor;

    /// <summary>
    /// Retrieves the color for the Y-axis (Green). Returns the default green color if none is specified.
    /// </summary>
    /// <returns>The color for the Y-axis.</returns>
    protected Color GetGreenColor() => _greenColor ?? _greenUniformColor;

    /// <summary>
    /// Retrieves the color for the Z-axis (Blue). Returns the default blue color if none is specified.
    /// </summary>
    /// <returns>The color for the Z-axis.</returns>
    protected Color GetBlueColor() => _blueColor ?? _blueUniformColor;
}
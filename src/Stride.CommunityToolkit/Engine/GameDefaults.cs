namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides shared default values used by game setup helpers.
/// </summary>
public static class GameDefaults
{
    /// <summary>
    /// The default name assigned to generated ground entities.
    /// </summary>
    public const string DefaultGroundName = "Ground";

    /// <summary>
    /// The error message used when a graphics compositor is required but has not been configured.
    /// </summary>
    public const string GraphicsCompositorNotSet = "GraphicsCompositor is not set.";

    /// <summary>
    /// The default scale applied to generated 3D ground entities.
    /// </summary>
    /// <remarks>
    /// The default value is <c>(20, 1, 20)</c>, representing width, height, and depth.
    /// </remarks>
    public static readonly Vector3 Default3DGroundSize = new(20f, 1f, 20f);

    /// <summary>
    /// The default scale applied to generated 2D ground entities.
    /// </summary>
    /// <remarks>
    /// The default value is <c>(20, 0.5, 1)</c>, representing width, height, and depth.
    /// </remarks>
    public static readonly Vector3 Default2DGroundSize = new(20, 0.5f, 1);

    /// <summary>
    /// The default world position applied to generated 2D ground entities.
    /// </summary>
    /// <remarks>
    /// The default value is <c>(0, -3, 0)</c>.
    /// </remarks>
    public static readonly Vector3 Default2DGroundPosition = new(0, -3f, 0);

    /// <summary>
    /// The default material color applied to generated primitive entities.
    /// </summary>
    public static readonly Color DefaultMaterialColor = Color.FromBgra(0xFF8C8C8C);

    /// <summary>
    /// The default material color applied to generated ground entities.
    /// </summary>
    public static readonly Color DefaultGroundMaterialColor = Color.FromBgra(0xFF242424);
}
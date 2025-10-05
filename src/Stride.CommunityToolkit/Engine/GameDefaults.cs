namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides default values for the game project.
/// </summary>
public static class GameDefaults
{
    /// <summary>
    /// The default name for the ground entity.
    /// </summary>
    public const string DefaultGroundName = "Ground";

    /// <summary>
    /// The message to display when the GraphicsCompositor is not set.
    /// </summary>
    public const string GraphicsCompositorNotSet = "GraphicsCompositor is not set.";

    /// <summary>
    /// The default size of the 3D ground entity.
    /// </summary>
    public static readonly Vector2 Default3DGroundSize = new(20f);

    /// <summary>
    /// The default size of the 2D ground entity.
    /// </summary>
    public static readonly Vector3 Default2DGroundSize = new(20, 0.1f, 0);

    /// <summary>
    /// The default material color for 3D entities.
    /// </summary>
    public static readonly Color DefaultMaterialColor = Color.FromBgra(0xFF8C8C8C);

    /// <summary>
    /// The default material color for the ground entity.
    /// </summary>
    public static readonly Color DefaultGroundMaterialColor = Color.FromBgra(0xFF242424);
}
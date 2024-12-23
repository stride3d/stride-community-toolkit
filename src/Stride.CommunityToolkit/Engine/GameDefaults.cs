namespace Stride.CommunityToolkit.Engine;

public static class GameDefaults
{
    public const string DefaultGroundName = "Ground";
    public const string GraphicsCompositorNotSet = "GraphicsCompositor is not set.";

    public static readonly Vector2 _default3DGroundSize = new(15f);
    public static readonly Vector3 _default2DGroundSize = new(15, 0.1f, 0);
    public static readonly Color _defaultMaterialColor = Color.FromBgra(0xFF8C8C8C);
    public static readonly Color _defaultGroundMaterialColor = Color.FromBgra(0xFF242424);
}
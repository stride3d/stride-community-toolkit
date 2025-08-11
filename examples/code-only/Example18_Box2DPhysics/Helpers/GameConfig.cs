using Stride.Core.Mathematics;

namespace Example18_Box2DPhysics.Helpers;

/// <summary>
/// Central configuration for the Box2D physics example
/// </summary>
public static class GameConfig
{
    // Entity naming
    public const string ShapeName = "Box2DShape";
    public const string WallName = "Wall";

    // Display and UI settings
    public const int DefaultSpacing = 20;
    public const int DefaultDebugX = 5;
    public const int DefaultDebugY = 30;
    public const int HeaderSpacing = 30;

    // Physics settings
    public const float Gravity = -10.0f;
    public const float DefaultDensity = 2.0f;
    public const float DefaultFriction = 0.3f;
    public const float DefaultRestitution = 0.1f;

    // Shape dimensions
    public static readonly Vector2 BoxSize = new(0.4f, 0.4f);
    public static readonly Vector2 RectangleSize = new(0.3f, 0.6f);
    public static readonly Vector2 CircleSize = new(0.3f, 0.3f);
    public static readonly Vector2 TriangleSize = new(0.5f, 0.5f);
    public static readonly Vector2 CapsuleSize = new(0.3f, 0.8f);

    // Spawn settings
    public const int DefaultSpawnCount = 10;
    public const int MassSpawnCount = 50;
    public const float SpawnAreaMinX = -5f;
    public const float SpawnAreaMaxX = 5f;
    public const float SpawnAreaMinY = 10f;
    public const float SpawnAreaMaxY = 30f;

    // Joint settings
    public const float DefaultJointLength = 1.0f;
    public const float JointHertz = 2.0f;
    public const float JointDampingRatio = 0.5f;

    // Visual settings
    public static readonly Color BackgroundColor = Color.CornflowerBlue;
    public static readonly Color DefaultShapeColor = Color.White;
    public static readonly Color SelectedShapeColor = Color.Yellow;
    public static readonly Color ConstraintColor = Color.LightBlue;
    public static readonly Color ShapeColor = Color.Pink; // b2_colorPink = 0xFFC0CB;
    public static readonly Color ShapeSleepColor = Color.Gray; // b2_colorGray = 0x808080;
    public static readonly Color GroundColor = Color.PaleGreen; // b2_colorPaleGreen = 0x98FB98

    // Input settings
    public const float ImpulseStrength = 5.0f;
    public const float MouseQuerySize = 0.1f;
}
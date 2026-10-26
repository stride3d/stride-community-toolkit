using Stride.Core.Mathematics;

namespace E06_Box2D.Helpers;

/// <summary>
/// Central configuration for the Box2D physics example
/// </summary>
public static class GameConfig
{
    // Entity naming
    public const string ShapeName = "Box2DShape";
    public const string WallName = "Wall";

    // Physics settings
    public const float Gravity = -10.0f;
    public const float DefaultDensity = 2.0f;
    public const float DefaultFriction = 0.3f;
    public const float DefaultRestitution = 0.1f;

    // Shape dimensions
    public static readonly Vector2 BoxSize = new(0.4f, 0.4f);
    public static readonly Vector2 RectangleSize = new(0.3f, 0.6f);

    /// <summary>
    /// The X value represents the radius, and the Y value represents the total height of the capsule.
    /// </summary>
    public static readonly Vector2 CapsuleSize = new(0.1f, 0.4f);

    // Spawn settings
    public const int DefaultSpawnCount = 10;
    public const int MassSpawnCount = 50;

    // Joint settings
    public const float DefaultJointLength = 1.0f;
    public const float JointHertz = 2.0f;
    public const float JointDampingRatio = 0.5f;

    // Visual settings
    public static readonly Color SelectedShapeColor = Color.Yellow;
    public static readonly Color ConstraintColor = Color.LightBlue;
    public static readonly Color ShapeColor = Color.Pink; // b2_colorPink = 0xFFC0CB;
    public static readonly Color GroundColor = Color.PaleGreen; // b2_colorPaleGreen = 0x98FB98

    // Input settings
    public const float MouseQuerySize = 0.1f;
}
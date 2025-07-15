using Stride.Core.Mathematics;

namespace Example18_Box2DPhysics.Helpers;

public static class GameConfig
{
    public const string ShapeName = "Box2DShape";
    public const int DefaultSpacing = 20;
    public const int DefaultDebugX = 5;
    public const int DefaultDebugY = 30;
    public const int HeaderSpacing = 30;
    public static readonly Vector2 BoxSize = new(0.2f, 0.2f);
    public static readonly Vector2 RectangleSize = new(0.2f, 0.3f);
}
using Example.Common;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example18_Box2DPhysics.Helpers;

/// <summary>
/// Factory class to create 2D shapes for the Box2D simulation with Box2D.NET
/// </summary>
public class ShapeFactory
{
    private readonly Game _game;
    private readonly Scene _scene;
    private readonly List<Shape2DModel> _shapes =
    [
        new() { Type = Primitive2DModelType.Square2D, Color = Color.Green, Size = GameConfig.BoxSize },
        new() { Type = Primitive2DModelType.Rectangle2D, Color = Color.Orange, Size = GameConfig.RectangleSize },
        new() { Type = Primitive2DModelType.Circle2D, Color = Color.Red, Size = GameConfig.BoxSize / 2 },
        new() { Type = Primitive2DModelType.Circle2D, Color = Color.Gold, Size = GameConfig.BoxSize },
        new() { Type = Primitive2DModelType.Triangle2D, Color = Color.Purple, Size = GameConfig.BoxSize },
        new()
        {
            Type = Primitive2DModelType.Capsule, Color = Color.Blue,
            Size = new Vector2(GameConfig.BoxSize.X, GameConfig.BoxSize.Y * 2)
        }
    ];

    public ShapeFactory(Game game, Scene scene)
    {
        _game = game;
        _scene = scene;
    }

    public Shape2DModel? GetShapeModel(Primitive2DModelType type)
        => _shapes.Find(x => x.Type == type);

    public Shape2DModel GetRandomShapeModel()
        => _shapes[Random.Shared.Next(_shapes.Count)];

    public Entity CreateEntity(Shape2DModel shape, Color? overrideColor = null, Vector2? position = null, string? name = null)
    {
        var actualColor = overrideColor ?? shape.Color;

        var darkerColor = new Color(
            (byte)(actualColor.R * 0.5f),
            (byte)(actualColor.G * 0.5f),
            (byte)(actualColor.B * 0.5f),
            actualColor.A);

        var entity = _game.Create2DPrimitive(shape.Type, new()
        {
            Size = shape.Size,
            Material = _game.CreateFlatMaterial(darkerColor)
        });

        entity.Name = name ?? $"{shape.Type}-{GameConfig.ShapeName}";
        entity.Transform.Position = position.HasValue ? (Vector3)position : GetRandomPosition();

        // Define polygon vertices based on shape type
        Vector2[] polygonVertices = shape.Type switch
        {
            Primitive2DModelType.Square2D or Primitive2DModelType.Rectangle2D => new[]
            {
            new Vector2(-shape.Size.X * 0.5f, -shape.Size.Y * 0.5f), // Bottom-left
            new Vector2(shape.Size.X * 0.5f, -shape.Size.Y * 0.5f),  // Bottom-right
            new Vector2(shape.Size.X * 0.5f, shape.Size.Y * 0.5f),   // Top-right
            new Vector2(-shape.Size.X * 0.5f, shape.Size.Y * 0.5f)   // Top-left
        },
            Primitive2DModelType.Triangle2D => new[]
            {
            new Vector2(0, shape.Size.Y * 0.5f),              // Top
            new Vector2(-shape.Size.X * 0.5f, -shape.Size.Y * 0.5f), // Bottom-left
            new Vector2(shape.Size.X * 0.5f, -shape.Size.Y * 0.5f)   // Bottom-right
        },
            _ => Array.Empty<Vector2>() // For circles, use existing logic
        };

        entity.Add(new MeshOutlineComponent()
        {
            Enabled = true,
            Color = actualColor,
            Intensity = 1f,
            ShapeType = shape.Type,
            OutlineThickness = 1f, // 3 pixels
            Radius = GetRadius(shape),
            PixelScale = 200,
            CapsuleHalfHeight = shape.Size.Y / 2,
            PolygonVertices = polygonVertices
        });

        entity.Scene = _scene;

        return entity;
    }

    private static float GetRadius(Shape2DModel model) => model.Type switch
    {
        Primitive2DModelType.Circle2D => model.Size.X,
        Primitive2DModelType.Capsule => model.Size.X / 2,
        _ => 0f
    };

    private static Vector3 GetRandomPosition() => new(Random.Shared.Next(-5, 5), Random.Shared.Next(10, 30), 0);
}
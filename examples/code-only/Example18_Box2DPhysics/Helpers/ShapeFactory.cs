using Example.Common;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;

namespace Example18_Box2DPhysics.Helpers;

/// <summary>
/// Factory class to create 2D shapes for the Box2D simulation with Box2D.NET-style rendering.
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

    public Shape2DModel? GetShapeModel(Primitive2DModelType? type = null)
    {
        if (type == null)
        {
            int randomIndex = Random.Shared.Next(_shapes.Count);
            return _shapes[randomIndex];
        }

        return _shapes.Find(x => x.Type == type);
    }

    public Entity CreateEntity(Shape2DModel shape, Color? color = null, Vector2? position = null)
    {
        var actualColor = color ?? shape.Color;

        // not sure how to get this
        float pixelScale = 200f;

        // Create entity with SDF-based Box2D style material
        var entity = _game.Create2DPrimitive(shape.Type, new()
        {
            Size = shape.Size,
            Material = _game.CreateFlatMaterial(actualColor),
            RenderGroup = RenderGroup.Group5
        });

        entity.Name = $"{shape.Type}-{GameConfig.ShapeName}";
        entity.Transform.Position = position.HasValue ? (Vector3)position : GetRandomPosition();

        // Define polygon vertices based on shape type
        Vector2[] polygonVertices = shape.Type switch
        {
            Primitive2DModelType.Square2D or Primitive2DModelType.Rectangle2D => new[]
            {
            new Vector2(-shape.Size.X * 0.5f, -shape.Size.Y * 0.5f), // Bottom-left
            new Vector2(shape.Size.X * 0.5f, -shape.Size.Y * 0.5f),  // Bottom-right
            new Vector2(shape.Size.X * 0.5f, shape.Size.Y * 0.5f),   // Top-right
            new Vector2(-shape.Size.X * 0.5f, shape.Size.Y * 0.5f)   // Top-lepft
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
            Color = Color.Green,
            Intensity = 100f,
            ShapeType = shape.Type,
            OutlineThickness = 3f, // 3 pixels
            Radius = shape.Type == Primitive2DModelType.Circle2D ? shape.Size.X : 0f,
            PixelScale = pixelScale,
            PolygonVertices = polygonVertices
        });

        entity.Scene = _scene;
        return entity;
    }

    private static Vector3 GetRandomPosition() => new(Random.Shared.Next(-5, 5), Random.Shared.Next(10, 30), 0);
}
using Example.Common;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace E06_Box2D.Helpers;

/// <summary>
/// The 2D shapes the Box2D demo spawns, each a shape with its dimensions and a colour, and the
/// entities drawn for them.
/// </summary>
public class ShapeFactory(Scene scene)
{
    /// <summary>A green square, <see cref="GameConfig.BoxSize"/> across.</summary>
    public Shape2DModel Square { get; } = new() { Type = Primitive2D.Square(GameConfig.BoxSize.X), Color = Color.Green };

    /// <summary>An orange rectangle of <see cref="GameConfig.RectangleSize"/>.</summary>
    public Shape2DModel Rectangle { get; } = new() { Type = Primitive2D.Rectangle(GameConfig.RectangleSize.X, GameConfig.RectangleSize.Y), Color = Color.Orange };

    /// <summary>A red circle as wide as the square.</summary>
    public Shape2DModel Circle { get; } = new() { Type = Primitive2D.Circle(GameConfig.BoxSize.X / 2), Color = Color.Red };

    /// <summary>A gold circle twice as wide as the square.</summary>
    public Shape2DModel LargeCircle { get; } = new() { Type = Primitive2D.Circle(GameConfig.BoxSize.X), Color = Color.Gold };

    /// <summary>A purple triangle as wide and as tall as the square.</summary>
    public Shape2DModel Triangle { get; } = new() { Type = Primitive2D.Triangle(GameConfig.BoxSize.X, GameConfig.BoxSize.Y), Color = Color.Purple };

    /// <summary>A blue capsule of <see cref="GameConfig.CapsuleSize"/>: its radius and its height end to end.</summary>
    public Shape2DModel Capsule { get; } = new() { Type = Primitive2D.Capsule(GameConfig.CapsuleSize.X, GameConfig.CapsuleSize.Y), Color = Color.Blue };

    private Shape2DModel[]? _all;

    /// <summary>Every model, for a random pick.</summary>
    public IReadOnlyList<Shape2DModel> All => _all ??= [Square, Rectangle, Circle, LargeCircle, Triangle, Capsule];

    public Shape2DModel GetRandomShapeModel()
        => All[Random.Shared.Next(All.Count)];

    public Entity CreateEntity(Shape2DModel shape, Color? overrideColor = null, Vector2? position = null, string? name = null)
    {
        var actualColor = overrideColor ?? shape.Color;

        // The Box2D debug-draw component renders the shape testbed-style - fill plus a
        // pixel-constant border from the same numbers the collider uses. No mesh, no material,
        // no outline shader.
        var entity = new Entity(name ?? $"{shape.Type.Name}-{GameConfig.ShapeName}")
        {
            CreateShapeComponent(shape, actualColor)
        };

        entity.Transform.Position = position.HasValue ? (Vector3)position : GetRandomPosition();
        entity.Scene = scene;

        return entity;
    }

    /// <summary>
    /// Builds the debug-draw outline for a shape model: a circle is one vertex plus a radius, a
    /// capsule two vertices plus a radius, the rest are their corner polygons - mirroring exactly
    /// how ShapeFixtureBuilder builds the matching collider.
    /// </summary>
    private static ShapeComponent CreateShapeComponent(Shape2DModel shape, Color color) => shape.Type switch
    {
        Primitive2D.CircleShape circle => new() { Vertices = [Vector2.Zero], Radius = circle.Radius, Color = color },
        Primitive2D.CapsuleShape capsule => new()
        {
            Vertices = [new(0, -(capsule.Height / 2 - capsule.Radius)), new(0, capsule.Height / 2 - capsule.Radius)],
            Radius = capsule.Radius,
            Color = color,
        },
        Primitive2D.TriangleShape triangle => new()
        {
            Vertices =
            [
                new(-triangle.Size.X * 0.5f, -triangle.Size.Y * 0.5f),
                new(triangle.Size.X * 0.5f, -triangle.Size.Y * 0.5f),
                new(0, triangle.Size.Y * 0.5f),
            ],
            Color = color,
        },
        Primitive2D.RectangleShape rectangle => new()
        {
            Vertices =
            [
                new(-rectangle.Size.X * 0.5f, -rectangle.Size.Y * 0.5f),
                new(rectangle.Size.X * 0.5f, -rectangle.Size.Y * 0.5f),
                new(rectangle.Size.X * 0.5f, rectangle.Size.Y * 0.5f),
                new(-rectangle.Size.X * 0.5f, rectangle.Size.Y * 0.5f),
            ],
            Color = color,
        },
        _ => throw new NotSupportedException($"No outline for a {shape.Type.Name}."),
    };

    private static Vector3 GetRandomPosition() => new(Random.Shared.Next(-5, 5), Random.Shared.Next(10, 30), 0);
}
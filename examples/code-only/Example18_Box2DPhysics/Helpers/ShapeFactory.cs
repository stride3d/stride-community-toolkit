using Example.Common;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Rendering.Gizmos;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace Example18_Box2DPhysics.Helpers;

/// <summary>
/// Factory class to create 2D shapes for the Box2D simulation.
/// </summary>
public class ShapeFactory
{
    private readonly Game _game;
    private readonly Scene _scene;
    private readonly List<Shape2DModel> _shapes =
    [
        new() { Type = Primitive2DModelType.Square2D, Color = Color.Green, Size = GameConfig.BoxSize },
        new() { Type = Primitive2DModelType.Rectangle2D, Color = new Color(255, 165, 0, 55), Size = GameConfig.RectangleSize },
        //new() { Type = Primitive2DModelType.Rectangle2D, Color = new Color(255, 213, 128), Size = GameConfig.RectangleSize },
        new() { Type = Primitive2DModelType.Circle2D, Color = Color.Red, Size = GameConfig.BoxSize / 2 },
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
        var entity = _game.Create2DPrimitive(shape.Type, new()
        {
            Size = shape.Size,
            Material = _game.CreateFlatMaterial(color ?? shape.Color)
        });

        entity.Name = $"{shape.Type}-{GameConfig.ShapeName}";
        entity.Transform.Position = position.HasValue ? (Vector3)position : GetRandomPosition();
        entity.Scene = _scene;

        if (shape.Type == Primitive2DModelType.Rectangle2D)
        {
            var meshData = RectangleProceduralModel.New(size: shape.Size);
            var borderVertices = meshData.Vertices.Select(v => new VertexPositionTexture(new Vector3(v.Position.X, v.Position.Y, 0), v.TextureCoordinate)).ToArray();

            Console.WriteLine($"Vertices: {string.Join(", ", borderVertices)}");

            var material = GizmoEmissiveColorMaterial.Create(_game.GraphicsDevice, Color.Orange);

            var borderIndices = new short[] { 0, 1, 2, 3, 0 };

            Console.WriteLine($"Vertices: {string.Join(", ", borderVertices)}");

            var vertexBuffer = Buffer.Vertex.New(_game.GraphicsDevice, borderVertices);
            var indexBuffer = Buffer.Index.New(_game.GraphicsDevice, borderIndices);

            var meshDraw = new MeshDraw
            {
                StartLocation = 0,
                PrimitiveType = PrimitiveType.LineStrip,
                VertexBuffers = [new VertexBufferBinding(vertexBuffer, new VertexDeclaration(VertexElement.Position<Vector3>(), VertexElement.TextureCoordinate<Vector2>()), borderVertices.Length)],
                //VertexBuffers = [new VertexBufferBinding(vertexBuffer, new VertexDeclaration(VertexElement.Position<Vector2>(), VertexElement.TextureCoordinate<Vector2>()), borderVertices.Length)],
                //VertexBuffers = [new VertexBufferBinding(vertexBuffer, new VertexDeclaration(VertexElement.Position<Vector3>()), borderVertices.Length)],
                IndexBuffer = new IndexBufferBinding(indexBuffer, is32Bit: false, borderIndices.Length),
                DrawCount = borderIndices.Length
            };

            var mesh = new Mesh { Draw = meshDraw };
            var model = new Model { mesh, material };
            var borderEntity = new Entity($"{shape.Type}-Border")
            {
                new ModelComponent { Model = model }
            };

            entity.AddChild(borderEntity);
        }

        return entity;
    }

    private static Vector3 GetRandomPosition() => new(Random.Shared.Next(-5, 5), Random.Shared.Next(10, 30), 0);
}
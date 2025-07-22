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
            //var meshData = RectangleProceduralModel.New(size: shape.Size);
            //var vertices = meshData.Vertices;
            ////var vertices = new[]
            ////{
            ////    new VertexPositionTexture(new Vector3(0.0f, 0.0f, 0.0f), Vector2.Zero),
            ////    new VertexPositionTexture(new Vector3(0.0f, 1.0f, 0.0f), Vector2.Zero),
            ////    new VertexPositionTexture(new Vector3(1.0f, 1.0f, 0.0f), Vector2.Zero),
            ////    new VertexPositionTexture(new Vector3(1.0f, 0.0f, 0.0f), Vector2.Zero),
            ////};
            //var vertices2 = new[]
            //{
            //    new VertexPositionTexture(new Vector3(0.0f, 0.0f, 0.0f), Vector2.Zero),
            //    new VertexPositionTexture(new Vector3(0.0f, 1.0f, 0.0f), Vector2.Zero),
            //    new VertexPositionTexture(new Vector3(1.0f, 1.0f, 0.0f), Vector2.Zero),
            //    new VertexPositionTexture(new Vector3(1.0f, 0.0f, 0.0f), Vector2.Zero),
            //    new VertexPositionTexture(new Vector3(0.0f, 0.0f, 0.0f), Vector2.Zero),
            //};
            //Console.WriteLine($"Vertices: {string.Join(", ", vertices)}");
            //var vertexBuffer = Buffer.Vertex.New(_game.GraphicsDevice, vertices);
            ////var vertexBuffer = Buffer.New(_game.GraphicsDevice, vertices, BufferFlags.VertexBuffer);
            ////var indices = meshData.Indices;
            //var indices = new[] { 0, 1, 2, 3, 0 };
            //Console.WriteLine($"Indices: {string.Join(", ", indices)}");
            //var indexBuffer = Buffer.New(_game.GraphicsDevice, indices, BufferFlags.IndexBuffer);

            var material = GizmoEmissiveColorMaterial.Create(_game.GraphicsDevice, Color.Orange);

            // Calculate half extents
            var halfWidth = (shape.Size.X / 2f) + 0.001f;
            var halfHeight = (shape.Size.Y / 2f) + 0.001f;

            // Define corners in order: bottom-left, top-left, top-right, bottom-right, and repeat first
            var borderVertices = new[]
            {
                new VertexPositionTexture(new Vector3(-halfWidth, -halfHeight, 0), Vector2.Zero),
                new VertexPositionTexture(new Vector3(-halfWidth,  halfHeight, 0), Vector2.Zero),
                new VertexPositionTexture(new Vector3( halfWidth,  halfHeight, 0), Vector2.Zero),
                new VertexPositionTexture(new Vector3( halfWidth, -halfHeight, 0), Vector2.Zero),
                new VertexPositionTexture(new Vector3(-halfWidth, -halfHeight, 0), Vector2.Zero),
            };
            var borderIndices = new short[] { 0, 1, 2, 3, 0 };

            var vertexBuffer = Buffer.Vertex.New(_game.GraphicsDevice, borderVertices);
            var indexBuffer = Buffer.New(_game.GraphicsDevice, borderIndices, BufferFlags.IndexBuffer);

            var meshDraw = new MeshDraw
            {
                StartLocation = 0,
                PrimitiveType = PrimitiveType.LineStrip,
                VertexBuffers = [new VertexBufferBinding(vertexBuffer, new VertexDeclaration(VertexElement.Position<Vector3>(), VertexElement.TextureCoordinate<Vector2>()), borderVertices.Length)],
                IndexBuffer = new IndexBufferBinding(indexBuffer, is32Bit: false, borderIndices.Length),
                DrawCount = borderIndices.Length
            };

            //var meshDraw = new MeshDraw
            //{
            //    StartLocation = 0,
            //    PrimitiveType = PrimitiveType.LineStrip,
            //    VertexBuffers = [new VertexBufferBinding(vertexBuffer, new VertexDeclaration(VertexElement.Position<Vector3>(),
            //                VertexElement.TextureCoordinate<Vector2>()), vertices.Length)],
            //    //VertexBuffers = [new VertexBufferBinding(vertexBuffer, new VertexDeclaration(VertexElement.Position<Vector3>()), vertices.Length)],
            //    //VertexBuffers = [new VertexBufferBinding(vertexBuffer, new VertexDeclaration(VertexElement.Position<Vector2>(), VertexElement.TextureCoordinate<Vector2>()), vertices.Length)],
            //    IndexBuffer = new IndexBufferBinding(indexBuffer, is32Bit: true, indices.Length),
            //    DrawCount = indices.Length
            //};

            var mesh = new Mesh { Draw = meshDraw };
            var model = new Model { mesh, material };
            var borderEntity = new Entity($"{shape.Type}-Border")
            {
                new ModelComponent { Model = model }
            };

            //vertexBuffer.Dispose();
            //indexBuffer.Dispose();

            //borderEntity.Transform.Position = entity.Transform.Position;
            entity.AddChild(borderEntity);
        }

        return entity;
    }

    private static Vector3 GetRandomPosition() => new(Random.Shared.Next(-5, 5), Random.Shared.Next(10, 30), 0);
}
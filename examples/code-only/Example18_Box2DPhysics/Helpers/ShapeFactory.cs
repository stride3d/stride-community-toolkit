using Example.Common;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Buffer = Stride.Graphics.Buffer;

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
        new() { Type = Primitive2DModelType.Rectangle2D, Color = new Color(255, 165, 0, 55), Size = GameConfig.RectangleSize },
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
        var actualColor = color ?? shape.Color;

        // Create main shape with darker fill color (Box2D.NET style)
        var fillColor = DarkenColor(actualColor, 0.3f);
        var entity = _game.Create2DPrimitive(shape.Type, new()
        {
            Size = shape.Size,
            Material = CreateBox2DStyleMaterial(fillColor, actualColor)
        });

        entity.Name = $"{shape.Type}-{GameConfig.ShapeName}";
        entity.Transform.Position = position.HasValue ? (Vector3)position : GetRandomPosition();
        entity.Scene = _scene;

        // Add border for all shape types (Box2D.NET style)
        CreateBox2DStyleBorder(entity, shape, actualColor);

        return entity;
    }

    /// <summary>
    /// Creates a Box2D.NET-style material with proper fill and border characteristics
    /// </summary>
    private Material CreateBox2DStyleMaterial(Color fillColor, Color borderColor)
    {
        return Material.New(_game.GraphicsDevice, new MaterialDescriptor
        {
            Attributes = new MaterialAttributes
            {
                Diffuse = new MaterialDiffuseMapFeature
                {
                    DiffuseMap = new ComputeColor(fillColor.ToColor4()),
                },
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                // Add subtle emission for Box2D.NET-like appearance
                Emissive = new MaterialEmissiveMapFeature
                {
                    EmissiveMap = new ComputeColor(fillColor.ToColor4())
                },
                // Reduce glossiness for flat 2D appearance
                MicroSurface = new MaterialGlossinessMapFeature
                {
                    GlossinessMap = new ComputeFloat(0.1f)
                },
                // Add transparency support for better blending
                Transparency = new MaterialTransparencyBlendFeature()
            }
        });
    }

    /// <summary>
    /// Creates Box2D.NET-style borders with lighter color and proper geometry
    /// </summary>
    private void CreateBox2DStyleBorder(Entity parentEntity, Shape2DModel shape, Color borderColor)
    {
        // Create lighter border color (Box2D.NET style)
        var lightBorderColor = LightenColor(borderColor, 0.4f);

        switch (shape.Type)
        {
            case Primitive2DModelType.Rectangle2D:
            case Primitive2DModelType.Square2D:
                CreateRectangleBorder(parentEntity, shape, lightBorderColor);
                break;

            case Primitive2DModelType.Circle2D:
                CreateCircleBorder(parentEntity, shape, lightBorderColor);
                break;

            case Primitive2DModelType.Triangle2D:
                CreateTriangleBorder(parentEntity, shape, lightBorderColor);
                break;

            case Primitive2DModelType.Capsule:
                CreateCapsuleBorder(parentEntity, shape, lightBorderColor);
                break;
        }
    }

    private void CreateRectangleBorder(Entity parentEntity, Shape2DModel shape, Color borderColor)
    {
        var meshData = RectangleProceduralModel.New(size: shape.Size);
        var borderVertices = meshData.Vertices
            .Select(v => new VertexPositionTexture(
                new Vector3(v.Position.X, v.Position.Y, 0.001f), // Slightly above fill
                v.TextureCoordinate))
            .ToArray();

        // Create proper border loop indices for better line rendering
        var borderIndices = new short[] { 0, 1, 1, 2, 2, 3, 3, 0 };

        CreateBorderMesh(parentEntity, borderVertices, borderIndices, borderColor,
            $"{shape.Type}-Border", PrimitiveType.LineList);
    }

    private void CreateCircleBorder(Entity parentEntity, Shape2DModel shape, Color borderColor)
    {
        const int segments = 64; // Increased for smoother circles
        var radius = Math.Min(shape.Size.X, shape.Size.Y) / 2f;
        var vertices = new List<VertexPositionTexture>();
        var indices = new List<short>();

        // Generate circle vertices
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)(2 * Math.PI * i / segments);
            float x = (float)(radius * Math.Cos(angle));
            float y = (float)(radius * Math.Sin(angle));

            vertices.Add(new VertexPositionTexture(new Vector3(x, y, 0.001f), Vector2.Zero));

            if (i < segments)
            {
                indices.Add((short)i);
                indices.Add((short)((i + 1) % (segments + 1)));
            }
        }

        CreateBorderMesh(parentEntity, vertices.ToArray(), indices.ToArray(), borderColor,
            $"{shape.Type}-Border", PrimitiveType.LineList);
    }

    private void CreateTriangleBorder(Entity parentEntity, Shape2DModel shape, Color borderColor)
    {
        var triangleData = TriangleProceduralModel.New(shape.Size);
        var borderVertices = triangleData.Vertices
            .Take(3) // Only use the triangle vertices, not duplicates
            .Select(v => new VertexPositionTexture(
                new Vector3(v.Position.X, v.Position.Y, 0.001f),
                v.TextureCoordinate))
            .ToArray();

        var borderIndices = new short[] { 0, 1, 1, 2, 2, 0 };

        CreateBorderMesh(parentEntity, borderVertices, borderIndices, borderColor,
            $"{shape.Type}-Border", PrimitiveType.LineList);
    }

    private void CreateCapsuleBorder(Entity parentEntity, Shape2DModel shape, Color borderColor)
    {
        // Enhanced capsule border with smoother curves
        var vertices = new List<VertexPositionTexture>();
        var indices = new List<short>();

        float halfWidth = shape.Size.X / 2f;
        float halfHeight = shape.Size.Y / 2f;
        float radius = halfWidth;

        // Add straight edges
        vertices.Add(new VertexPositionTexture(new Vector3(-halfWidth, halfHeight - radius, 0.001f), Vector2.Zero));
        vertices.Add(new VertexPositionTexture(new Vector3(halfWidth, halfHeight - radius, 0.001f), Vector2.Zero));
        vertices.Add(new VertexPositionTexture(new Vector3(halfWidth, -halfHeight + radius, 0.001f), Vector2.Zero));
        vertices.Add(new VertexPositionTexture(new Vector3(-halfWidth, -halfHeight + radius, 0.001f), Vector2.Zero));

        // Connect the straight edges
        indices.AddRange(new short[] { 0, 1, 1, 2, 2, 3, 3, 0 });

        // Add curved ends with better tessellation
        const int arcSegments = 16; // Increased for smoother curves
        int baseIndex = vertices.Count;

        // Top arc
        for (int i = 0; i <= arcSegments; i++)
        {
            float angle = (float)(Math.PI * i / arcSegments);
            float x = (float)(radius * Math.Cos(angle));
            float y = (float)(radius * Math.Sin(angle)) + (halfHeight - radius);
            vertices.Add(new VertexPositionTexture(new Vector3(x, y, 0.001f), Vector2.Zero));

            if (i < arcSegments)
            {
                indices.Add((short)(baseIndex + i));
                indices.Add((short)(baseIndex + i + 1));
            }
        }

        // Bottom arc
        baseIndex = vertices.Count;
        for (int i = 0; i <= arcSegments; i++)
        {
            float angle = (float)(Math.PI + Math.PI * i / arcSegments);
            float x = (float)(radius * Math.Cos(angle));
            float y = (float)(radius * Math.Sin(angle)) + (-halfHeight + radius);
            vertices.Add(new VertexPositionTexture(new Vector3(x, y, 0.001f), Vector2.Zero));

            if (i < arcSegments)
            {
                indices.Add((short)(baseIndex + i));
                indices.Add((short)(baseIndex + i + 1));
            }
        }

        CreateBorderMesh(parentEntity, vertices.ToArray(), indices.ToArray(), borderColor,
            $"{shape.Type}-Border", PrimitiveType.LineList);
    }

    private void CreateBorderMesh(Entity parentEntity, VertexPositionTexture[] vertices, short[] indices,
        Color borderColor, string entityName, PrimitiveType primitiveType)
    {
        // Create enhanced border material with stronger emission and proper transparency
        var borderMaterial = Material.New(_game.GraphicsDevice, new MaterialDescriptor
        {
            Attributes = new MaterialAttributes
            {
                Diffuse = new MaterialDiffuseMapFeature
                {
                    DiffuseMap = new ComputeColor(borderColor.ToColor4())
                },
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                Emissive = new MaterialEmissiveMapFeature
                {
                    EmissiveMap = new ComputeColor(borderColor.ToColor4()) // Increased emission
                },
                MicroSurface = new MaterialGlossinessMapFeature
                {
                    GlossinessMap = new ComputeFloat(0.0f)
                },
                // Add transparency for better visual blending
                Transparency = new MaterialTransparencyBlendFeature()
            }
        });

        var vertexBuffer = Buffer.Vertex.New(_game.GraphicsDevice, vertices);
        var indexBuffer = Buffer.Index.New(_game.GraphicsDevice, indices);

        var meshDraw = new MeshDraw
        {
            StartLocation = 0,
            PrimitiveType = primitiveType,
            VertexBuffers = [new VertexBufferBinding(vertexBuffer,
                new VertexDeclaration(VertexElement.Position<Vector3>(), VertexElement.TextureCoordinate<Vector2>()),
                vertices.Length)],
            IndexBuffer = new IndexBufferBinding(indexBuffer, is32Bit: false, indices.Length),
            DrawCount = indices.Length
        };

        var mesh = new Mesh { Draw = meshDraw };
        var model = new Model { mesh, borderMaterial };
        var borderEntity = new Entity(entityName)
        {
            new ModelComponent { Model = model }
        };

        parentEntity.AddChild(borderEntity);
    }

    /// <summary>
    /// Creates an enhanced Box2D.NET-style material with better color blending
    /// </summary>
    public Material CreateEnhancedBox2DStyleMaterial(Color baseColor, float borderThickness = 0.05f)
    {
        var fillColor = DarkenColor(baseColor, 0.2f);
        var borderColor = LightenColor(baseColor, 0.3f);

        return Material.New(_game.GraphicsDevice, new MaterialDescriptor
        {
            Attributes = new MaterialAttributes
            {
                Diffuse = new MaterialDiffuseMapFeature
                {
                    DiffuseMap = new ComputeColor(fillColor.ToColor4())
                },
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                Emissive = new MaterialEmissiveMapFeature
                {
                    // Create subtle rim lighting effect
                    EmissiveMap = new ComputeColor(borderColor.ToColor4() * 0.2f)
                },
                MicroSurface = new MaterialGlossinessMapFeature
                {
                    GlossinessMap = new ComputeFloat(0.05f) // Very slight glossiness
                },
                Transparency = new MaterialTransparencyBlendFeature(),
                // Add subtle normal mapping for depth
                Surface = new MaterialNormalMapFeature()
            }
        });
    }

    /// <summary>
    /// Darkens a color by the specified factor (0.0 = no change, 1.0 = black)
    /// </summary>
    private static Color DarkenColor(Color color, float factor)
    {
        factor = Math.Clamp(factor, 0f, 1f);
        return new Color(
            (byte)(color.R * (1f - factor)),
            (byte)(color.G * (1f - factor)),
            (byte)(color.B * (1f - factor)),
            color.A
        );
    }

    /// <summary>
    /// Lightens a color by the specified factor (0.0 = no change, 1.0 = white)
    /// </summary>
    private static Color LightenColor(Color color, float factor)
    {
        factor = Math.Clamp(factor, 0f, 1f);
        return new Color(
            (byte)(color.R + (255 - color.R) * factor),
            (byte)(color.G + (255 - color.G) * factor),
            (byte)(color.B + (255 - color.B) * factor),
            color.A
        );
    }

    private static Vector3 GetRandomPosition() => new(Random.Shared.Next(-5, 5), Random.Shared.Next(10, 30), 0);
}
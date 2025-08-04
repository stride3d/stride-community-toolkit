using Example.Common;
using Example18_Box2DPhysics.Materials;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

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

        // Create entity with SDF-based Box2D style material
        var entity = _game.Create2DPrimitive(shape.Type, new()
        {
            Size = shape.Size,
            Material = CreateBox2DSDFMaterial(actualColor, shape.Type)
        });

        entity.Name = $"{shape.Type}-{GameConfig.ShapeName}";
        entity.Transform.Position = position.HasValue ? (Vector3)position : GetRandomPosition();
        entity.Scene = _scene;

        return entity;
    }

    /// <summary>
    /// Creates a Box2D.NET-style material using SDF shader for crisp borders and anti-aliasing
    /// </summary>
    private Material CreateBox2DSDFMaterial(Color baseColor, Primitive2DModelType shapeType)
    {
        // Map shape types to shader constants
        int shaderShapeType = shapeType switch
        {
            Primitive2DModelType.Circle2D => 1,
            Primitive2DModelType.Triangle2D => 2,
            Primitive2DModelType.Capsule => 3,
            _ => 0 // Rectangle/Square default
        };

        return Material.New(_game.GraphicsDevice, new MaterialDescriptor
        {
            Attributes = new MaterialAttributes
            {
                // Use our custom Box2D style diffuse model
                DiffuseModel = new MaterialBox2DStyleFeature
                {
                    BaseColor = baseColor.ToColor4(),
                    BorderThickness = 0.02f,
                    AntiAliasing = 0.003f,
                    ShapeType = shaderShapeType,
                    UseLightBorder = true,
                    Intensity = 20000 // Adjust intensity for better visibility
                },
                Diffuse = new MaterialDiffuseMapFeature
                {
                    DiffuseMap = new ComputeColor(baseColor.ToColor4()),
                },
                Specular = new MaterialMetalnessMapFeature(new ComputeFloat(1)),
                SpecularModel = new MaterialSpecularMicrofacetModelFeature(),
                Emissive = new MaterialEmissiveMapFeature
                {
                    EmissiveMap = new ComputeColor(baseColor.ToColor4())
                },
                // Reduce glossiness for flat 2D appearance
                //MicroSurface = new MaterialGlossinessMapFeature
                //{
                //    GlossinessMap = new ComputeFloat(0.1f)
                //},
                MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.65f)),
                // Add transparency support for better blending
                //Transparency = new MaterialTransparencyBlendFeature()
            }
        });
    }

    /// <summary>
    /// Creates a Box2D.NET-style material with proper fill and border characteristics
    /// Do not remove this when refactoring or using Copilot Agent
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

    private static Vector3 GetRandomPosition() => new(Random.Shared.Next(-5, 5), Random.Shared.Next(10, 30), 0);
}
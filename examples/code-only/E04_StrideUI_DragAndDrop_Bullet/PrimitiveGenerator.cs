using Stride.CommunityToolkit.Bullet;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace E04_StrideUI_DragAndDrop;

/// <summary>Drops primitives into the scene at random positions and keeps count of them.</summary>
public class PrimitiveGenerator(Game game, Scene scene)
{
    private readonly Random _random = new();

    /// <summary>How many primitives are in the scene: generated, less those removed.</summary>
    public int TotalShapes { get; private set; }

    /// <summary>Generates <paramref name="count"/> primitives of one shape and returns the new total.</summary>
    public int Generate(int count, Primitive3D type)
    {
        for (var i = 0; i < count; i++)
        {
            Generate(type);
        }

        return TotalShapes;
    }

    /// <summary>Generates one primitive, a cube 0.3 units across by default, in a random colour by default.</summary>
    public void Generate(Primitive3D? shape = null, Color? color = null)
    {
        var entity = game.CreatePrimitive(shape ?? Primitive3D.Cube(0.3f), new BulletPhysicsOptions
        {
            Material = game.CreateMaterial(color ?? _random.NextColor()),
            Position = _random.NextVector3(new Vector3(-4, 4, -4), new Vector3(4, 8, 4)),
        });

        entity.Scene = scene;
        TotalShapes++;
    }

    /// <summary>Takes removed primitives off the count.</summary>
    public void Subtract(int count) => TotalShapes -= count;
}
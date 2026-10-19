using E13_SignalR_Shared;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;

namespace E13_SignalR.Station;

/// <summary>
/// Turns a size and a paint into a physical box in the scene. Materials are made once per paint and
/// shared, so a hundred containers cost six materials.
/// </summary>
public sealed class ContainerFactory(Game game)
{
    private readonly Dictionary<ContainerPaint, Material> _materials = [];

    /// <summary>What each size measures and weighs. Mass scales with volume roughly, so a large one lands with authority.</summary>
    public static (Vector3 Box, float Mass) Spec(ContainerSize size) => size switch
    {
        ContainerSize.Small => (new Vector3(1f, 1f, 1f), 1f),
        ContainerSize.Medium => (new Vector3(2f, 1f, 1f), 2f),
        ContainerSize.Large => (new Vector3(3f, 1.5f, 1.5f), 5f),
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    /// <summary>Creates a dynamic container at the given pose and adds it to the root scene.</summary>
    public Entity Create(int id, ContainerSize size, ContainerPaint paint, Vector3 position, Quaternion rotation)
    {
        var (box, mass) = Spec(size);

        var entity = game.CreatePrimitive(Primitive3D.Box(box), new BepuPhysicsOptions
        {
            EntityName = $"Container {id} ({size}, {paint})",
            Material = MaterialFor(paint),
            Position = position,
        });

        // Set before the entity joins the scene: Bepu takes the body's starting pose from the
        // transform when the component attaches, and a random tilt is what makes the drops tumble
        entity.Transform.Rotation = rotation;

        if (entity.Get<BodyComponent>()?.Collider is CompoundCollider compound)
        {
            foreach (var collider in compound.Colliders)
            {
                collider.Mass = mass;
            }
        }

        entity.Scene = game.SceneSystem.SceneInstance.RootScene;

        return entity;
    }

    private Material MaterialFor(ContainerPaint paint)
    {
        if (!_materials.TryGetValue(paint, out var material))
        {
            // Matte and a little rough: rusted steel does not shine
            material = game.CreateMaterial(Hex.ToColor(Paints.Hex(paint)), metalness: 0.15f, glossiness: 0.3f);

            _materials[paint] = material;
        }

        return material;
    }
}
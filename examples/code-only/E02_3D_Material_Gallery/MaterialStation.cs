using Example.Common.Galleries;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Materials;

namespace E02_3D_Material_Gallery;

/// <summary>
/// A station of the material gallery: the ring's frame plus what a material exhibit needs - the
/// pack's textures, a way to build a material from a descriptor, the entities it placed so a
/// rebuild can clear them, and the variations a station cycles on V.
/// </summary>
public sealed class MaterialStation : GalleryStation
{
    /// <summary>Where the trio stands on every station: sphere left, cube in the middle, teapot right.</summary>
    public static readonly Vector3 SphereSpot = new(-2.8f, 1f, 0f);
    // A centimetre above the ground: with its bottom face exactly on the ground plane the two fight for the
    // depth buffer, which the two-sided glass shows as a flickering bottom
    public static readonly Vector3 CubeSpot = new(0f, 0.91f, 0f);
    public static readonly Vector3 TeapotSpot = new(2.8f, 0.4f, 0f);

    // A primitive's size is what its procedural model calls size: a sphere's is its radius, a cube's
    // its edge, the teapot's its overall scale
    private const float SphereRadius = 1f;
    private const float RowRadius = 0.7f;

    private readonly List<Entity> _entities = [];

    public MaterialTextures Textures { get; set; } = null!;

    /// <summary>The entities this station placed, in order.</summary>
    public IReadOnlyList<Entity> Entities => _entities;

    /// <summary>Which variation is showing; a station's setup reads it and builds that one.</summary>
    public int Variation { get; set; }

    /// <summary>The names of the variations the station offers, one per V press; one name means no V.</summary>
    public IReadOnlyList<string> VariationNames { get; private set; } = [];

    /// <summary>Declares the station's variations and returns the one to build, wrapped into range.</summary>
    public int Pick(params string[] names)
    {
        VariationNames = names;
        Variation = (Variation % names.Length + names.Length) % names.Length;

        return Variation;
    }

    /// <summary>
    /// A material from a descriptor, compiled for this game's device with its content manager, so the
    /// engine's lookup-table environment term resolves. The descriptor stays on the material:
    /// <c>Material.New</c> leaves it null, and a material used as a layer needs it, since the generator
    /// composes the layer from its features.
    /// </summary>
    public Material Material(MaterialDescriptor descriptor)
    {
        var material = Stride.Rendering.Material.New(Game.GraphicsDevice, descriptor, Game.Content);

        material.Descriptor = descriptor;

        return material;
    }

    /// <summary>Removes everything the station placed, so a setup can run again for a variation.</summary>
    public void Clear()
    {
        foreach (var entity in _entities) entity.Scene = null;

        _entities.Clear();
    }

    /// <summary>
    /// Places a primitive with a material in station coordinates, turned to face the ring's centre
    /// like everything on a station.
    /// </summary>
    /// <param name="type">Which primitive.</param>
    /// <param name="material">Its material.</param>
    /// <param name="local">Where, in station coordinates.</param>
    /// <param name="size">The primitive's size, or its default.</param>
    public Entity Place(PrimitiveModelType type, Material material, Vector3 local, Vector3? size = null)
    {
        var entity = Game.Create3DPrimitive(type, new Primitive3DEntityOptions
        {
            EntityName = $"Station {Number} {type}",
            Material = material,
            Size = size,
            Position = At(local),
        });

        entity.Transform.Rotation = FacingRotation();
        entity.Scene = Scene;
        _entities.Add(entity);

        return entity;
    }

    /// <summary>Places a model of your own - a MeshBuilder mesh - with a material, in station coordinates.</summary>
    /// <param name="model">The model; its first material slot takes <paramref name="material"/>.</param>
    /// <param name="material">Its material.</param>
    /// <param name="local">Where, in station coordinates.</param>
    /// <param name="scale">A uniform scale.</param>
    /// <param name="rotation">A rotation in station space, on top of facing the ring's centre.</param>
    /// <param name="castShadows">Whether the model casts shadows; off for a material whose shadow pass misbehaves.</param>
    public Entity PlaceModel(Model model, Material material, Vector3 local, float scale = 1f, Quaternion? rotation = null, bool castShadows = true)
    {
        model.Materials.Clear();
        model.Materials.Add(material);

        var entity = new Entity($"Station {Number} model")
        {
            new ModelComponent(model) { IsShadowCaster = castShadows },
        };

        entity.Transform.Position = At(local);
        entity.Transform.Rotation = FacingRotation() * (rotation ?? Quaternion.Identity);
        entity.Transform.Scale = new Vector3(scale);
        entity.Scene = Scene;
        _entities.Add(entity);

        return entity;
    }

    /// <summary>The trio every station shows, so the eye compares stations by shapes it knows: one material on all three.</summary>
    public void PlaceTrio(Material material) => PlaceTrio(material, material, material);

    /// <summary>The trio with a material each.</summary>
    public void PlaceTrio(Material sphere, Material cube, Material teapot)
    {
        Place(PrimitiveModelType.Sphere, sphere, SphereSpot, new Vector3(SphereRadius));
        Place(PrimitiveModelType.Cube, cube, CubeSpot, new Vector3(1.8f));
        Place(PrimitiveModelType.Teapot, teapot, TeapotSpot, new Vector3(2.4f));
    }

    /// <summary>A row of spheres across the station, one material each - the shape of a sweep.</summary>
    /// <param name="materials">Left to right.</param>
    /// <param name="spacing">Centre to centre.</param>
    /// <param name="depth">How far forward of the station's centre the row stands, so two rows can share a station.</param>
    public void PlaceRow(IReadOnlyList<Material> materials, float spacing = 1.7f, float depth = 0f)
    {
        var left = -0.5f * spacing * (materials.Count - 1);

        for (var i = 0; i < materials.Count; i++)
        {
            Place(PrimitiveModelType.Sphere, materials[i], new Vector3(left + i * spacing, RowRadius, depth), new Vector3(RowRadius));
        }
    }
}
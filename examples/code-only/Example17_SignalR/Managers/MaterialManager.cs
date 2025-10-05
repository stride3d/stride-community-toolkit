using Example17_SignalR.Builders;
using Example17_SignalR.Core;
using Example17_SignalR_Shared.Core;
using Stride.Rendering;

namespace Example17_SignalR.Managers;

/// <summary>
/// Provides cached materials per <see cref="EntityType"/> built by <see cref="MaterialBuilder"/>.
/// </summary>
public class MaterialManager
{
    private readonly Dictionary<EntityType, Material> _materials = [];
    private readonly MaterialBuilder _materialBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="MaterialManager"/> class.
    /// </summary>
    public MaterialManager(MaterialBuilder materialBuilder)
    {
        _materialBuilder = materialBuilder;

        AddMaterials();
    }

    private void AddMaterials()
    {
        foreach (var colorType in Colors.Map)
        {
            var material = _materialBuilder.CreateMaterial(colorType.Value);

            _materials.Add(colorType.Key, material);
        }
    }

    /// <summary>
    /// Returns the cached material for the given <paramref name="entityType"/>.
    /// </summary>
    /// <exception cref="ArgumentException">If material was not pre-generated for the type.</exception>
    public Material GetMaterial(EntityType entityType)
    {
        if (_materials.TryGetValue(entityType, out var material))
            return material;

        throw new ArgumentException($"Material for {entityType} not found.");
    }
}
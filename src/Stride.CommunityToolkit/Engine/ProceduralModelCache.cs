using Stride.Rendering;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The models <see cref="GameExtensions.GetOrCreateModel(Stride.Games.IGame, Rendering.ProceduralModels.Primitive3D, Material?)"/>
/// hands out: one per shape and material, kept for the life of the game they were built for.
/// Registered as a service on the game at first use.
/// </summary>
/// <remarks>
/// The shapes are records, so two <c>Primitive3D.Sphere(0.5f)</c> calls are one key; the material is
/// compared by reference, so one material object means one model.
/// </remarks>
internal sealed class ProceduralModelCache
{
    private readonly Dictionary<(object Shape, Material? Material), Model> _models = [];

    /// <summary>The model for a shape and material, built by <paramref name="build"/> the first time it is asked for.</summary>
    public Model GetOrAdd(object shape, Material? material, Func<Model> build)
    {
        if (_models.TryGetValue((shape, material), out var model)) return model;

        model = build();

        _models[(shape, material)] = model;

        return model;
    }
}
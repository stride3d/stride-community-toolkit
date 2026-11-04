using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;
using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The models a game keeps: one per shape and material, shared by every entity that asks for it.
/// </summary>
public static class GameModelExtensions
{
    /// <summary>
    /// Returns one model for a 3D <paramref name="shape"/> and <paramref name="material"/>, generated the first time the pair is asked for and the same object on every later call, so many entities draw the same mesh from the same vertex and index buffers.
    /// </summary>
    /// <param name="game">The game the model is generated for and kept with.</param>
    /// <param name="shape">The shape and its dimensions, from the <see cref="Primitive3D"/> factories.</param>
    /// <param name="material">The material added to the model, or <see langword="null"/> for none. Compared by reference: one material object, one model.</param>
    /// <returns>The shared model.</returns>
    /// <remarks>
    ///
    /// <para><see cref="GameExtensions.CreatePrimitive(IGame, Primitive3D, PrimitiveEntityOptions)"/> generates a mesh and a pair of GPU buffers per call, which is what a scene of a few hundred distinct things wants. A scene of ten thousand spheres wants one mesh: through one shared model they cost about 400 MB where ten thousand generated models cost about 1.5 GB. This is the model to hand an instancing master, or to put on each entity's <see cref="ModelComponent"/>;
    /// <see cref="PrimitiveEntityOptions.ShareModel"/> makes <c>CreatePrimitive</c> do that. </para>
    ///
    /// <para>The model is one object: a material swapped on it, or a mesh changed in it, shows on every entity that shares it. </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="game"/> or <paramref name="shape"/> is <see langword="null"/>.</exception>
    public static Model GetOrCreateModel(this IGame game, Primitive3D shape, Material? material = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(shape);

        return ModelCache(game).GetOrAdd(shape, material, () => GenerateModel(game, ProceduralModelBuilder.Build(shape), material));
    }

    /// <summary>
    /// Returns one model for a flat 2D <paramref name="shape"/> and <paramref name="material"/>, the 2D case of <see cref="GetOrCreateModel(IGame, Primitive3D, Material)"/>.
    /// </summary>
    /// <param name="game">The game the model is generated for and kept with.</param>
    /// <param name="shape">The shape and its dimensions, from the <see cref="Primitive2D"/> factories.</param>
    /// <param name="material">The material added to the model, or <see langword="null"/> for none. Compared by reference: one material object, one model.</param>
    /// <returns>The shared model.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="game"/> or <paramref name="shape"/> is <see langword="null"/>.</exception>
    public static Model GetOrCreateModel(this IGame game, Primitive2D shape, Material? material = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(shape);

        return ModelCache(game).GetOrAdd(shape, material, () => GenerateModel(game, ProceduralModelBuilder.Build(shape), material));
    }

    /// <summary>The model a primitive entity draws: the shared one when the options ask for it, otherwise a fresh one.</summary>
    internal static Model ModelFor(IGame game, Primitive3D shape, PrimitiveEntityOptions options)
        => options.ShareModel ? game.GetOrCreateModel(shape, options.Material) : GenerateModel(game, ProceduralModelBuilder.Build(shape), options.Material);

    /// <inheritdoc cref="ModelFor(IGame, Primitive3D, PrimitiveEntityOptions)"/>
    internal static Model ModelFor(IGame game, Primitive2D shape, PrimitiveEntityOptions options)
        => options.ShareModel ? game.GetOrCreateModel(shape, options.Material) : GenerateModel(game, ProceduralModelBuilder.Build(shape), options.Material);

    /// <summary>A model generated from a procedural description, with the material added when there is one.</summary>
    private static Model GenerateModel(IGame game, PrimitiveProceduralModelBase modelBase, Material? material)
    {
        ArgumentNullException.ThrowIfNull(game);

        var model = modelBase.Generate(game.Services);

        if (material != null) model.Materials.Add(material);

        return model;
    }

    /// <summary>The game's model cache, registered as a service the first time it is needed.</summary>
    private static ProceduralModelCache ModelCache(IGame game)
    {
        if (game.Services.GetService<ProceduralModelCache>() is { } existing) return existing;

        var cache = new ProceduralModelCache();

        game.Services.AddService(cache);

        return cache;
    }
}
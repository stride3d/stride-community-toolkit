using Stride.CommunityToolkit.Rendering.Gizmos;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.CommunityToolkit.Rendering.Lines;

/// <summary>
/// Creates entities that draw polylines as ribbons.
/// </summary>
public static class PolylineExtensions
{
    /// <summary>
    /// Creates an entity drawing <paramref name="points"/> as one continuous ribbon with an emissive, unlit-looking material.
    /// </summary>
    /// <param name="game">The game whose graphics device the mesh is created on.</param>
    /// <param name="points">The line's points, in order. At least two.</param>
    /// <param name="options">Width, colour, glow and plane; <see langword="null"/> for the defaults.</param>
    /// <param name="name">The entity name, or <c>"Polyline"</c>.</param>
    /// <returns>An entity holding a <see cref="ModelComponent"/>; add it to a scene or parent it to another entity.</returns>
    /// <remarks>
    /// The material is drawn double-sided, so the ribbon stays visible from behind. It is still a thin flat
    /// strip, so it disappears when viewed exactly edge-on; see <see cref="PolylineOptions.Normal"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">If <paramref name="game"/> or <paramref name="points"/> is <see langword="null"/>.</exception>
    public static Entity CreatePolyline(this IGame game, IReadOnlyList<Vector3> points, PolylineOptions? options = null, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(points);

        options ??= new PolylineOptions();

        var mesh = PolylineMeshBuilder.Build(game.GraphicsDevice, points, options);

        return ToEntity(game, mesh, options, name ?? "Polyline");
    }

    /// <summary>
    /// Creates an entity drawing many disconnected straight segments as one ribbon mesh - tick marks, grids, arrows.
    /// </summary>
    /// <param name="game">The game whose graphics device the mesh is created on.</param>
    /// <param name="segments">The segments, each a start and an end point. At least one.</param>
    /// <param name="options">Width, colour, glow and plane; <see langword="null"/> for the defaults.</param>
    /// <param name="name">The entity name, or <c>"Segments"</c>.</param>
    /// <returns>An entity holding a <see cref="ModelComponent"/>; add it to a scene or parent it to another entity.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="game"/> or <paramref name="segments"/> is <see langword="null"/>.</exception>
    public static Entity CreateSegments(this IGame game, IReadOnlyList<(Vector3 Start, Vector3 End)> segments, PolylineOptions? options = null, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(segments);

        options ??= new PolylineOptions();

        var mesh = PolylineMeshBuilder.BuildSegments(game.GraphicsDevice, segments, options);

        return ToEntity(game, mesh, options, name ?? "Segments");
    }

    private static Entity ToEntity(IGame game, Mesh mesh, PolylineOptions options, string name)
    {
        var material = GizmoEmissiveColorMaterial.Create(game.GraphicsDevice, options.Color, options.EmissiveIntensity);
        material.Passes[0].CullMode = CullMode.None;

        var model = new Model { mesh, material };

        return new Entity(name) { new ModelComponent(model) };
    }
}
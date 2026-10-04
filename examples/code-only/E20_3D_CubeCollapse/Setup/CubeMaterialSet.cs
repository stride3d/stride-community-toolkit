using Stride.Core.Mathematics;
using Stride.Rendering;

namespace CubeCollapse.Setup;

/// <summary>
/// The cube materials in both hover states, each keyed by the cube's base colour.
/// </summary>
/// <param name="Normal">What every cube wears when the mouse is elsewhere.</param>
/// <param name="Brightened">Worn by every member of a clearable group under the mouse.</param>
public sealed record CubeMaterialSet(
    IReadOnlyDictionary<Color, Material> Normal,
    IReadOnlyDictionary<Color, Material> Brightened);
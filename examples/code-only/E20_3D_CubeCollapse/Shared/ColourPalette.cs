using Stride.Core.Mathematics;

namespace CubeCollapse.Shared;

/// <summary>
/// A named set of cube colours.
/// </summary>
/// <param name="Name">Shown in the palette dropdown. Printable ASCII only - the debug text renderer
/// silently blanks anything else.</param>
/// <param name="Colours">The cube colours, in a fixed order.</param>
/// <param name="Glass">Whether the cubes are tinted glass instead of flat colour; the colours are then the tints.</param>
/// <param name="Glossiness">For glass: how smooth it is, 0.95 clear down to 0.45 frosted.</param>
public sealed record ColourPalette(string Name, IReadOnlyList<Color> Colours, bool Glass = false, float Glossiness = 0.95f);
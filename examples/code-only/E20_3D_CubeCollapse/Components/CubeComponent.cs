using Stride.Core.Mathematics;
using Stride.Engine;

namespace CubeCollapse.Components;

/// <summary>
/// Marks an entity as a playable cube and remembers the colour it was given.
/// </summary>
/// <remarks>
/// The colour is stored here rather than read back from the material, which changes with the palette
/// and the hover. Its presence is what says an entity the raycast found is in play.
/// </remarks>
public class CubeComponent : EntityComponent
{
    /// <summary>
    /// Gets or sets the cube's colour. Cubes match when their colours are equal.
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    /// Gets or sets which column and layer the cube occupies.
    /// </summary>
    /// <remarks>
    /// Maintained by <see cref="Gameplay.CubeGrid"/>, and updated the moment a clear happens rather
    /// than when the cube finishes falling. Matching reads this instead of the transform, so a click
    /// during a collapse still finds the right neighbours.
    /// </remarks>
    public Int3 GridPosition { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CubeComponent"/> class.
    /// </summary>
    /// <remarks>
    /// Every component needs a public parameterless constructor for the serializer; the STRDIAG010
    /// analyser warns without one, even in a code-only project.
    /// </remarks>
    public CubeComponent() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CubeComponent"/> class with a colour.
    /// </summary>
    /// <param name="color">The colour this cube is painted.</param>
    public CubeComponent(Color color) => Color = color;
}
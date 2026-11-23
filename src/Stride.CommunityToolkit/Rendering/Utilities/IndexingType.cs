namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// Specifies index element size used when building indexed geometry.
/// </summary>
public enum IndexingType
{
    /// <summary>
    ///     Do not use vertex indexing
    /// </summary>
    None = 0,

    /// <summary>
    ///     Use 16-bit indices, which the GPU reads as a <see cref="ushort" />: vertices 0 to 65,535
    /// </summary>
    Int16 = 2,

    /// <summary>
    ///     Use a <see cref="int" /> for vertex indices
    /// </summary>
    Int32 = 4
}
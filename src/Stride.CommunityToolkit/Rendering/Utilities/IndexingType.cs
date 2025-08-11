namespace Stride.CommunityToolkit.Rendering.Utilities;

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
    ///     Use a <see cref="short" /> for vertex indices
    /// </summary>
    Int16 = 2,

    /// <summary>
    ///     Use a <see cref="int" /> for vertex indices
    /// </summary>
    Int32 = 4
}
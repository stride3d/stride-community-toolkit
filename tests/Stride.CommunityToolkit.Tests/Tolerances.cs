namespace Stride.CommunityToolkit.Tests;

/// <summary>
/// The tolerances the tests compare floats with, named for how much arithmetic the compared value has been through.
/// </summary>
internal static class Tolerances
{
    /// <summary>A value straight out of one formula: only the last bits may differ.</summary>
    internal const float Float = 1e-5f;

    /// <summary>A value that went through a few steps in a row: a rotation, a triangulation, a framing distance.</summary>
    internal const float Geometry = 1e-4f;

    /// <summary>A value that went through a camera projection or a normalisation of generated geometry.</summary>
    internal const float Projection = 1e-3f;
}
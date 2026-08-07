namespace Stride.CommunityToolkit.Renderers;

/// <summary>
/// Shared default values used across the built-in scene renderers.
/// </summary>
internal static class RendererDefaults
{
    /// <summary>
    /// Content path of the default font used for on-screen debug/overlay text.
    /// </summary>
    public const string DefaultFontPath = "/Stride.Engine/StrideDefaultFont";

    /// <summary>
    /// Default background color used behind on-screen debug/overlay text.
    /// </summary>
    public static readonly Color4 DefaultBackground = new(0.9f, 0.9f, 0.9f, 0.01f);
}
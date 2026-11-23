namespace Stride.CommunityToolkit.Rendering.Gizmos;

/// <summary>
/// The editor's axis colours, one source for every gizmo that draws an axis.
/// </summary>
internal static class GizmoColors
{
    /// <summary>The X axis, Game Studio's red.</summary>
    public static readonly Color X = new(0xFC, 0x37, 0x37);

    /// <summary>The Y axis, Game Studio's green.</summary>
    public static readonly Color Y = new(0x32, 0xE3, 0x35);

    /// <summary>The Z axis, Game Studio's blue.</summary>
    public static readonly Color Z = new(0x2F, 0x6A, 0xE1);
}
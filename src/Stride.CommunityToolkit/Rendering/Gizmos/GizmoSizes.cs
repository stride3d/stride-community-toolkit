namespace Stride.CommunityToolkit.Rendering.Gizmos;

/// <summary>
/// The proportions the gizmos share, so the light gizmo's origin sphere is the translation gizmo's.
/// </summary>
internal static class GizmoSizes
{
    /// <summary>The tessellation of every gizmo primitive: smooth at the cost of vertices, which a gizmo can afford.</summary>
    public const int Tessellation = 64;

    /// <summary>The size of the cone at the end of an axis, which the other proportions derive from.</summary>
    public const float ExtremitySize = 0.15f;

    /// <summary>How much larger the sphere at the origin is than the cones at the extremities.</summary>
    public const float OriginScale = 1.33f;

    /// <summary>The side of the translation gizmo's small planes.</summary>
    public const float PlaneLength = 0.25f;

    /// <summary>The radius of the sphere at the origin, as both gizmos draw it.</summary>
    public const float OriginRadius = OriginScale * ExtremitySize / 3f;
}
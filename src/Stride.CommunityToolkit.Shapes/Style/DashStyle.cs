namespace Stride.CommunityToolkit.Shapes;

/// <summary>A captured dash pattern: length, resolved gap and phase, all in pixels.</summary>
internal readonly record struct DashStyle(float Length, float Gap, float Phase);
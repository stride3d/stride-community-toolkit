using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>A captured glow: width in pixels (0 for none), the colour resolved, and whether it adds light.</summary>
internal readonly record struct GlowStyle(float Width, Color Color, bool Additive);
using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>A captured gradient: whether there is one, the far colour with the fill alpha applied, and the direction.</summary>
internal readonly record struct GradientStyle(bool Enabled, Color Color, Vector2 Direction);
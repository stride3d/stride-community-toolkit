using Box2D.NET;
using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Conversions between Box2D's and Stride's 2D vectors. The two structs hold the same two floats;
/// only the type differs, and these keep that difference out of the physics code.
/// </summary>
internal static class B2Vec2Extensions
{
    /// <summary>The same point or direction as a Box2D vector.</summary>
    internal static B2Vec2 ToB2Vec2(this Vector2 vector) => new(vector.X, vector.Y);

    /// <summary>The same point or direction as a Stride vector.</summary>
    internal static Vector2 ToVector2(this B2Vec2 vector) => new(vector.X, vector.Y);
}
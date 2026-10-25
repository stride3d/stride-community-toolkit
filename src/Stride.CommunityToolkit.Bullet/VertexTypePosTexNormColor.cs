using Stride.Core.Mathematics;
using Stride.Graphics;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.Bullet;

/// <summary>
/// Custom vertex type including position, normal, tangent, UV and color supporting normal mapping.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct VertexTypePosTexNormColor : IEquatable<VertexTypePosTexNormColor>, IVertex
{
    /// <summary>
    /// Initializes a new instance with explicit vertex attribute values.
    /// </summary>
    public VertexTypePosTexNormColor(Vector3 position, Vector3 normal, Vector3 tangent, Vector2 texCoord1, Vector4 color1)
    {
        Position = position;
        Normal = normal;
        Tangent = tangent;
        TexCoord = texCoord1;
        Color = color1;
    }

    /// <summary>Position in object space.</summary>
    public Vector3 Position;

    /// <summary>Vertex normal.</summary>
    public Vector3 Normal;

    /// <summary>Tangent vector (aligned with U texture axis).</summary>
    public Vector3 Tangent;

    /// <summary>Primary texture coordinate.</summary>
    public Vector2 TexCoord;

    /// <summary>Vertex color.</summary>
    public Vector4 Color;

    /// <summary>Size of the vertex struct in bytes.</summary>
    public static readonly int Size = 60;

    /// <summary>Stride vertex declaration describing the layout.</summary>
    public static readonly VertexDeclaration Layout = new(
        VertexElement.Position<Vector3>(),          // 12 = 4 * 3
        VertexElement.Normal<Vector3>(),            // 24
        VertexElement.Tangent<Vector3>(),           // 36
        VertexElement.TextureCoordinate<Vector2>(), // 44
        VertexElement.Color<Vector4>());            // 60

    /// <summary>
    /// Value equality comparison.
    /// </summary>
    public bool Equals(VertexTypePosTexNormColor other)
        => Position.Equals(other.Position) && Normal.Equals(other.Normal) && Tangent.Equals(other.Tangent)
        && Color.Equals(other.Color) && TexCoord.Equals(other.TexCoord);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is VertexTypePosTexNormColor other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Position, Normal, Tangent, TexCoord, Color);

    /// <inheritdoc />
    public VertexDeclaration GetLayout() => Layout;

    /// <summary>
    /// Flips the U texture coordinate horizontally (used when inverting winding order).
    /// </summary>
    public void FlipWinding() => TexCoord.X = 1.0f - TexCoord.X;

    /// <summary>Equality operator.</summary>
    public static bool operator ==(VertexTypePosTexNormColor left, VertexTypePosTexNormColor right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(VertexTypePosTexNormColor left, VertexTypePosTexNormColor right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => $"Position: {Position}, Normal: {Normal}, Tangent {Tangent}, Texcoord: {TexCoord}, Color: {Color}";
}
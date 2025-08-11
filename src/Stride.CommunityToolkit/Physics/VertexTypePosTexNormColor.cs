using Stride.Graphics;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.Physics;

public static partial class HeightmapExtensions
{
    /// <summary>
    /// Custom vertex type including position, normal, tangent, UV and color supporting normal mapping.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VertexTypePosTexNormColor : IEquatable<VertexTypePosTexNormColor>, IVertex
    {
        /// <summary>
        /// Initializes a new instance with explicit vertex attribute values.
        /// </summary>
        public VertexTypePosTexNormColor(Vector3 position, Vector3 normal, Vector3 tangent,
            Vector2 texCoord1, Vector4 color1) : this()
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
        public static readonly VertexDeclaration Layout = new VertexDeclaration(
           VertexElement.Position<Vector3>(),//12=4*3
           VertexElement.Normal<Vector3>(),//24
           VertexElement.Tangent<Vector3>(),//36
           VertexElement.TextureCoordinate<Vector2>(),//44
           VertexElement.Color<Vector4>());

        /// <summary>
        /// Value equality comparison.
        /// </summary>
        public bool Equals(VertexTypePosTexNormColor other)
                => Position.Equals(other.Position) && Normal.Equals(other.Normal) && Tangent.Equals(other.Tangent)
                && Color.Equals(other.Color) && TexCoord.Equals(other.TexCoord);

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is VertexTypePosTexNormColor && Equals((VertexTypePosTexNormColor)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Position.GetHashCode();
                hashCode = (hashCode * 397) ^ Normal.GetHashCode();
                hashCode = (hashCode * 397) ^ TexCoord.GetHashCode();
                hashCode = (hashCode * 397) ^ Color.GetHashCode();
                return hashCode;
            }
        }

        /// <inheritdoc />
        public VertexDeclaration GetLayout()
                => Layout;

        /// <summary>
        /// Flips the U texture coordinate horizontally (used when inverting winding order).
        /// </summary>
        public void FlipWinding()
                => TexCoord.X = (1.0f - TexCoord.X);

        /// <summary>Equality operator.</summary>
        public static bool operator ==(VertexTypePosTexNormColor left, VertexTypePosTexNormColor right)
                => left.Equals(right);

        /// <summary>Inequality operator.</summary>
        public static bool operator !=(VertexTypePosTexNormColor left, VertexTypePosTexNormColor right)
                => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString()
                => string.Format("Position: {0}, Normal: {1}, Tangent {2}, Texcoord: {3}, Color: {4}", Position, Normal, Tangent, TexCoord, Color);
    }
}
using Stride.Graphics;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.Physics;

public static partial class HeightmapExtensions
{
    /// <summary>
    /// Custom vertex type so that we can generate tangents for supporting normal maps
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VertexTypePosTexNormColor : IEquatable<VertexTypePosTexNormColor>, IVertex
    {
        public VertexTypePosTexNormColor(Vector3 position, Vector3 normal, Vector3 tangent,
            Vector2 texCoord1, Vector4 color1) : this()
        {
            Position = position;
            Normal = normal;
            Tangent = tangent;
            TexCoord = texCoord1;
            Color = color1;
        }

        public Vector3 Position;
        public Vector3 Normal;
        public Vector3 Tangent;
        public Vector2 TexCoord;
        public Vector4 Color;

        public static readonly int Size = 60;

        public static readonly VertexDeclaration Layout = new VertexDeclaration(
           VertexElement.Position<Vector3>(),//12=4*3
           VertexElement.Normal<Vector3>(),//24
           VertexElement.Tangent<Vector3>(),//36
           VertexElement.TextureCoordinate<Vector2>(),//44
           VertexElement.Color<Vector4>());

        public bool Equals(VertexTypePosTexNormColor other)
            => Position.Equals(other.Position) && Normal.Equals(other.Normal) && Tangent.Equals(other.Tangent)
            && Color.Equals(other.Color) && TexCoord.Equals(other.TexCoord);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is VertexTypePosTexNormColor && Equals((VertexTypePosTexNormColor)obj);
        }

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

        public VertexDeclaration GetLayout()
            => Layout;

        public void FlipWinding()
            => TexCoord.X = (1.0f - TexCoord.X);

        public static bool operator ==(VertexTypePosTexNormColor left, VertexTypePosTexNormColor right)
            => left.Equals(right);

        public static bool operator !=(VertexTypePosTexNormColor left, VertexTypePosTexNormColor right)
            => !left.Equals(right);

        public override string ToString()
            => string.Format("Position: {0}, Normal: {1}, Tangent {2}, Texcoord: {3}, Color: {4}", Position, Normal, Tangent, TexCoord, Color);

    }

}

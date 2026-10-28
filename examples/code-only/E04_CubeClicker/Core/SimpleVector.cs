using Stride.Core;

namespace E04_CubeClicker.Core;

/// <summary>
/// Stride.Core.Vector isn't generated as the generator can't reach the core without being in it
/// So a workaround has to be made with a local class
/// </summary>
/// <remarks>
/// Equality is written out by hand rather than taken from a <c>record struct</c>: the positional
/// properties a record generates are init-only in a way Stride's data serializer does not populate,
/// and this type exists precisely to be serialized.
/// </remarks>
[DataContract]
public readonly struct SimpleVector : IEquatable<SimpleVector>
{
    public float X { get; init; }

    public float Y { get; init; }

    public float Z { get; init; }

    public SimpleVector(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <inheritdoc />
    public bool Equals(SimpleVector other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is SimpleVector other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    /// <summary>Compares two vectors for equality.</summary>
    public static bool operator ==(SimpleVector left, SimpleVector right) => left.Equals(right);

    /// <summary>Compares two vectors for inequality.</summary>
    public static bool operator !=(SimpleVector left, SimpleVector right) => !left.Equals(right);
}
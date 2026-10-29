using Stride.Core;

namespace E04_CubeClicker.Core;

/// <summary>
/// Stands in for Stride's <c>Vector3</c>, which the YAML generator cannot reach from outside its assembly.
/// </summary>
/// <remarks>
/// Equality is written out by hand rather than taken from a positional <c>record struct</c>:
/// NexVYaml does not support records with constructors, and this type exists to be serialized.
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
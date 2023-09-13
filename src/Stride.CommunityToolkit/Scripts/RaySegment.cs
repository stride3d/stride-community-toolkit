using System.Globalization;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.Scripts;
/// <summary>
/// Represents a three dimensional line based on a 2 points in space.
/// </summary>
[DataContract]
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct RaySegment : IEquatable<RaySegment>, IFormattable
{
    private const string ToStringFormat = "Start:{0} End:{1}";

    /// <summary>
    /// The position in three dimensional space where the ray starts.
    /// </summary>
    public Vector3 Start { get; init; }

    /// <summary>
    /// The position in three dimensional space where the ray ends.
    /// </summary>
    public Vector3 End { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RaySegment"/> struct.
    /// </summary>
    /// <param name="start">The position in three dimensional space where the ray starts.</param>
    /// <param name="end">The position in three dimensional space where the ray ends.</param>
    public RaySegment(Vector3 start, Vector3 end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Length of RaySegment
    /// </summary>
    public float Length => Vector3.Distance(Start, End);

    /// <summary>
    /// Tests for equality between two objects.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><c>true</c> if <paramref name="left"/> has the same value as <paramref name="right"/>; otherwise, <c>false</c>.</returns>
    public static bool operator ==(RaySegment left, RaySegment right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Tests for inequality between two objects.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><c>true</c> if <paramref name="left"/> has a different value than <paramref name="right"/>; otherwise, <c>false</c>.</returns>
    public static bool operator !=(RaySegment left, RaySegment right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Returns a <see cref="string"/> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string"/> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return string.Format(CultureInfo.CurrentCulture, ToStringFormat, Start.ToString(), End.ToString());
    }

    /// <summary>
    /// Returns a <see cref="string"/> that represents this instance.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <returns>
    /// A <see cref="string"/> that represents this instance.
    /// </returns>
    public string ToString(string format)
    {
        return string.Format(CultureInfo.CurrentCulture, ToStringFormat, Start.ToString(format, CultureInfo.CurrentCulture),
            End.ToString(format, CultureInfo.CurrentCulture));
    }

    /// <summary>
    /// Returns a <see cref="string"/> that represents this instance.
    /// </summary>
    /// <param name="formatProvider">The format provider.</param>
    /// <returns>
    /// A <see cref="string"/> that represents this instance.
    /// </returns>
    public string ToString(IFormatProvider formatProvider)
    {
        return string.Format(formatProvider, ToStringFormat, Start.ToString(), End.ToString());
    }

    /// <summary>
    /// Returns a <see cref="string"/> that represents this instance.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="formatProvider">The format provider.</param>
    /// <returns>
    /// A <see cref="string"/> that represents this instance.
    /// </returns>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return string.Format(formatProvider, ToStringFormat, Start.ToString(format, formatProvider),
            End.ToString(format, formatProvider));
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
    /// </returns>
    public override int GetHashCode()
    {
        return Start.GetHashCode() + End.GetHashCode();
    }

    /// <summary>
    /// Determines whether the specified <see cref="Stride.Core.Mathematics.Vector4"/> is equal to this instance.
    /// </summary>
    /// <param name="value">The <see cref="Stride.Core.Mathematics.Vector4"/> to compare with this instance.</param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="Stride.Core.Mathematics.Vector4"/> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(RaySegment value)
    {
        return Start == value.Start && End == value.End;
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to this instance.
    /// </summary>
    /// <param name="value">The <see cref="object"/> to compare with this instance.</param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? value)
    {
        if (value == null)
            return false;
        if (value.GetType() != GetType())
            return false;
        return Equals((RaySegment)value);
    }

    /// <summary>
    /// Performs an explicit conversion from <see cref="RaySegment"/> to <see cref="Ray"/>.
    /// </summary>
    /// <param name="raySegment">The <see cref="RaySegment"/> to convert</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator Ray(RaySegment raySegment)
    {
        var result = new Ray(raySegment.Start, Vector3.Normalize(raySegment.End - raySegment.Start));

        return result;
    }

}
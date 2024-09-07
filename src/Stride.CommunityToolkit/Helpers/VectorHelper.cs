namespace Stride.CommunityToolkit.Helpers;

/// <summary>
/// Provides helper methods for generating and manipulating <see cref="Vector3"/> objects.
/// </summary>
/// <remarks>
/// This class contains utility methods designed to assist with common tasks involving vectors,
/// such as generating random <see cref="Vector3"/> values within a specified range.
/// </remarks>
public static class VectorHelper
{
    private static Random _defaultRandom = new();  // Default instance, non-seeded

    /// <summary>
    /// Generates a random Vector3 with each component (X, Y, Z) within the given range.
    /// By default, min is 0 and max is required.
    /// </summary>
    /// <param name="max">The maximum value for X, Y, and Z components.</param>
    /// <param name="min">The minimum value for X, Y, and Z components. Default is 0.</param>
    /// <param name="random">Optional random instance for custom seeding or performance.</param>
    /// <returns>A random Vector3.</returns>
    public static Vector3 RandomVector3(float max, float min = 0f, Random? random = null)
    {
        random ??= _defaultRandom;
        return new Vector3(
            RandomRange(min, max, random),
            RandomRange(min, max, random),
            RandomRange(min, max, random)
        );
    }

    /// <summary>
    /// Generates a random Vector3 with each component (X, Y, Z) based on the provided ranges.
    /// </summary>
    /// <param name="xRange">Array containing [minX, maxX] values for X component.</param>
    /// <param name="yRange">Array containing [minY, maxY] values for Y component.</param>
    /// <param name="zRange">Array containing [minZ, maxZ] values for Z component.</param>
    /// <param name="random">Optional random instance for custom seeding or performance.</param>
    /// <returns>A random Vector3.</returns>
    public static Vector3 RandomVector3(float[] xRange, float[] yRange, float[] zRange, Random? random = null)
    {
        if (xRange.Length != 2 || yRange.Length != 2 || zRange.Length != 2)
            throw new ArgumentException("Each range array must have exactly two elements [min, max].");

        random ??= _defaultRandom;
        return new Vector3(
            RandomRange(xRange[0], xRange[1], random),
            RandomRange(yRange[0], yRange[1], random),
            RandomRange(zRange[0], zRange[1], random)
        );
    }

    /// <summary>
    /// Helper method to generate a random float between min and max.
    /// </summary>
    private static float RandomRange(float min, float max, Random random)
    {
        return (float)(random.NextDouble() * (max - min) + min);
    }

    /// <summary>
    /// Optionally re-seeds the default random generator for the application.
    /// </summary>
    public static void SeedRandom(int seed)
    {
        _defaultRandom = new Random(seed);
    }
}
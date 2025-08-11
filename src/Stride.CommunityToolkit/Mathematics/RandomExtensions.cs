namespace Stride.CommunityToolkit.Mathematics;

/// <summary>
/// Helper extension methods for <see cref="Random"/> tailored for gameplay and procedural content scenarios.
/// </summary>
/// <remarks>
/// Provides convenience APIs for:
/// <list type="bullet">
/// <item><description>Generating scalar values (<see cref="NextSingle"/>) compatible with Stride math types.</description></item>
/// <item><description>Sampling uniformly within 2D rectangles (<see cref="NextPoint(Random, RectangleF)"/>) and 3D bounding boxes (<see cref="NextPoint(Random, BoundingBox)"/>).</description></item>
/// <item><description>Producing random direction vectors in 2D/3D (<see cref="NextDirection2D"/>, <see cref="NextDirection3D"/>).</description></item>
/// <item><description>Sampling a point uniformly inside a circle (<see cref="PointInACircle"/>) using area-correct square–root radial distribution.</description></item>
/// <item><description>Creating a random opaque color (<see cref="NextColor"/>).</description></item>
/// </list>
/// All methods validate the <see cref="Random"/> instance and use single-precision math to align with Stride's <c>Vector2</c>, <c>Vector3</c>, and <c>Color</c> types.
/// </remarks>
public static class RandomExtensions
{
    /// <summary>
    /// Generates a random <see cref="float"/>.
    /// </summary>
    /// <param name="random">An instance of <see cref="Random"/>.</param>
    /// <returns>A random <see cref="float"/>.</returns>
    /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
    public static float NextSingle(this Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        return (float)random.NextDouble();
    }

    /// <summary>
    /// Generates a random point in 2D space within the specified region.
    /// </summary>
    /// <param name="random">An instance of <see cref="Random"/>.</param>
    /// <param name="region">A 2D region in which point is generated.</param>
    /// <returns>A random point in 2D space within the specified region.</returns>
    /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
    public static Vector2 NextPoint(this Random random, RectangleF region)
    {
        ArgumentNullException.ThrowIfNull(random);

        return new Vector2(
            random.NextSingle() * region.Width + region.X,
            random.NextSingle() * region.Height + region.Y);
    }

    /// <summary>
    /// Generates a random point in 3D space within the specified region.
    /// </summary>
    /// <param name="random">An instance of <see cref="Random"/>.</param>
    /// <param name="region">A 3D region in which point is generated.</param>
    /// <returns>A random point whose X/Y/Z components lie within <paramref name="region"/>'s bounds.</returns>
    /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
    public static Vector3 NextPoint(this Random random, BoundingBox region)
    {
        ArgumentNullException.ThrowIfNull(random);

        Vector3 minimum = region.Minimum;
        Vector3 difference = region.Maximum - minimum;

        return new Vector3(
            random.NextSingle() * difference.X + minimum.X,
            random.NextSingle() * difference.Y + minimum.Y,
            random.NextSingle() * difference.Z + minimum.Z);
    }

    /// <summary>
    /// Generates a random normalized 2D direction vector.
    /// </summary>
    /// <param name="random">An instance of <see cref="Random"/>.</param>
    /// <returns>A unit-length (or zero) direction vector in the XY plane.</returns>
    /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
    public static Vector2 NextDirection2D(this Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        return Vector2.Normalize(new Vector2(random.NextSingle(), random.NextSingle()));
    }

    /// <summary>
    /// Generates a random normalized 3D direction vector.
    /// </summary>
    /// <param name="random">An instance of <see cref="Random"/>.</param>
    /// <returns>A unit-length (or zero) direction vector in 3D space.</returns>
    /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
    public static Vector3 NextDirection3D(this Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        return Vector3.Normalize(new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle()));
    }

    /// <summary>
    /// Generates a random point uniformly distributed inside a circle of the given <paramref name="radius"/>.
    /// </summary>
    /// <param name="random">An instance of <see cref="Random"/>.</param>
    /// <param name="radius">Radius of circle. Default 1.0f.</param>
    /// <returns>A random point whose distance from the origin is &lt;= <paramref name="radius"/>.</returns>
    /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
    public static Vector2 PointInACircle(this Random random, float radius = 1.0f)
    {
        ArgumentNullException.ThrowIfNull(random);

        // Use a single angle and sqrt on radius factor to achieve uniform area distribution.
        var angle = random.NextSingle() * MathF.PI * 2f;
        var r = MathF.Sqrt(random.NextSingle()) * radius;

        return new Vector2(MathF.Cos(angle) * r, MathF.Sin(angle) * r);
    }

    /// <summary>
    /// Generates a random color.
    /// </summary>
    /// <param name="random">An instance of <see cref="Random"/>.</param>
    /// <returns>A random color. Alpha is set to 255. </returns>
    /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
    public static Color NextColor(this Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        return new Color(NextDirection3D(random));
    }
}
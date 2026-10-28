namespace Stride.CommunityToolkit.Mathematics;

/// <summary>
/// Helper extension methods for <see cref="Random"/> tailored for gameplay and procedural content scenarios.
/// </summary>
/// <remarks>
/// Provides convenience APIs for:
/// <list type="bullet">
/// <item><description>Generating scalar values (<see cref="NextSingle(Random)"/>, <see cref="NextSingle(Random, float, float)"/>) compatible with Stride math types.</description></item>
/// <item><description>Sampling uniformly within 2D rectangles (<see cref="NextPoint(Random, RectangleF)"/>), 3D bounding boxes (<see cref="NextPoint(Random, BoundingBox)"/>) and component ranges (<see cref="NextVector3(Random, Vector3, Vector3)"/>).</description></item>
/// <item><description>Producing random direction vectors in 2D/3D (<see cref="NextDirection2D"/>, <see cref="NextDirection3D"/>).</description></item>
/// <item><description>Sampling a point uniformly inside a circle (<see cref="PointInACircle"/>) using area-correct square–root radial distribution.</description></item>
/// <item><description>Creating a random opaque color (<see cref="NextColor"/>), vivid by default or in another <see cref="ColorTone"/>.</description></item>
/// </list>
/// All methods validate the <see cref="Random"/> instance and use single-precision math to align with Stride's <c>Vector2</c>, <c>Vector3</c>, and <c>Color</c> types.
/// They extend the caller's own <see cref="Random"/>, so a world built from <c>new Random(seed)</c> comes out the same for the same seed.
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
    /// Generates a random <see cref="float"/> between <paramref name="min"/> and <paramref name="max"/>.
    /// </summary>
    /// <param name="random">An instance of <see cref="Random"/>.</param>
    /// <param name="min">The lowest value returned.</param>
    /// <param name="max">The value the range runs up to.</param>
    /// <returns>A random <see cref="float"/> in the range.</returns>
    /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
    public static float NextSingle(this Random random, float min, float max)
    {
        ArgumentNullException.ThrowIfNull(random);

        return min + random.NextSingle() * (max - min);
    }

    /// <summary>
    /// Generates a random point with each component between the matching components of <paramref name="min"/> and <paramref name="max"/>.
    /// </summary>
    /// <param name="random">An instance of <see cref="Random"/>.</param>
    /// <param name="min">The lowest X, Y and Z.</param>
    /// <param name="max">The highest X, Y and Z.</param>
    /// <returns>A random point inside the box the two corners span.</returns>
    /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
    public static Vector3 NextVector3(this Random random, Vector3 min, Vector3 max)
    {
        ArgumentNullException.ThrowIfNull(random);

        return new Vector3(random.NextSingle(min.X, max.X), random.NextSingle(min.Y, max.Y), random.NextSingle(min.Z, max.Z));
    }

    /// <summary>
    /// Generates a random vector with every component between <paramref name="min"/> and <paramref name="max"/>.
    /// </summary>
    /// <param name="random">An instance of <see cref="Random"/>.</param>
    /// <param name="min">The lowest value of each component.</param>
    /// <param name="max">The highest value of each component.</param>
    /// <returns>A random vector inside the cube the range spans.</returns>
    /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
    public static Vector3 NextVector3(this Random random, float min, float max)
        => random.NextVector3(new Vector3(min), new Vector3(max));

    /// <summary>
    /// Generates a random unit direction in the XY plane, every angle equally likely.
    /// </summary>
    /// <param name="random">An instance of <see cref="Random"/>.</param>
    /// <returns>A unit-length direction vector in the XY plane.</returns>
    /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
    public static Vector2 NextDirection2D(this Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        var angle = random.NextSingle() * MathUtil.TwoPi;

        return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
    }

    /// <summary>
    /// Generates a random unit direction in 3D, every direction equally likely.
    /// </summary>
    /// <param name="random">An instance of <see cref="Random"/>.</param>
    /// <returns>A unit-length direction vector.</returns>
    /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
    public static Vector3 NextDirection3D(this Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        // A uniform height and a uniform angle round it cover the sphere evenly
        var z = random.NextSingle() * 2f - 1f;
        var angle = random.NextSingle() * MathUtil.TwoPi;
        var ring = MathF.Sqrt(1f - z * z);

        return new Vector3(ring * MathF.Cos(angle), ring * MathF.Sin(angle), z);
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
        var angle = random.NextSingle() * MathUtil.TwoPi;
        var r = MathF.Sqrt(random.NextSingle()) * radius;

        return new Vector2(MathF.Cos(angle) * r, MathF.Sin(angle) * r);
    }

    /// <summary>
    /// Generates a random opaque colour: a hue spread evenly round the wheel, with the saturation and
    /// brightness drawn from the range <paramref name="tone"/> names.
    /// </summary>
    /// <param name="random">An instance of <see cref="Random"/>.</param>
    /// <param name="tone">The part of the colour space to draw from. Defaults to <see cref="ColorTone.Vivid"/>.</param>
    /// <returns>A random colour with alpha 255.</returns>
    /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
    public static Color NextColor(this Random random, ColorTone tone = ColorTone.Vivid)
    {
        ArgumentNullException.ThrowIfNull(random);

        var (saturation, value) = tone switch
        {
            ColorTone.Vivid => (random.NextSingle(0.7f, 1f), random.NextSingle(0.8f, 1f)),
            ColorTone.Light => (random.NextSingle(0.2f, 0.5f), random.NextSingle(0.9f, 1f)),
            ColorTone.Dark => (random.NextSingle(0.5f, 1f), random.NextSingle(0.2f, 0.45f)),
            _ => (random.NextSingle(), random.NextSingle()),
        };

        return (Color)new ColorHSV(random.NextSingle() * 360f, saturation, value, 1f).ToColor();
    }
}
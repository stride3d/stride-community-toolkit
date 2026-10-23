namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

/// <summary>
/// A flat 2D primitive shape in the XY plane with its dimensions: the thing
/// <c>game.CreatePrimitive(Primitive2D.Circle(0.5f))</c> builds a model from, and a physics package derives the
/// matching collider from. A 2D shape still lives in a 3D world; the thickness its collider gets along Z is
/// <see cref="Depth"/>, one unit unless the factory is told otherwise. The model is flat whatever the depth.
/// </summary>
/// <remarks>
/// The set is closed: every shape is one of the nested records below, created through the static factories, which
/// name exactly the dimensions the shape has. The defaults are the ones the toolkit's 2D helpers always used.
/// </remarks>
public abstract record Primitive2D
{
    private Primitive2D() { }

    /// <summary>The thickness along Z of the collider a physics package fits to the shape, a positive finite number checked by the factories. The model is flat whatever it is.</summary>
    public float Depth { get; init; } = 1f;

    /// <exception cref="ArgumentOutOfRangeException">The depth is not a positive finite number.</exception>
    private static float Checked(float depth)
        => float.IsFinite(depth) && depth > 0f ? depth : throw new ArgumentOutOfRangeException(nameof(depth), depth, "The collider depth must be a positive finite number.");

    /// <summary>The shape's name for labels: <c>Square</c> for a rectangle with equal sides, otherwise the case's name without its <c>Shape</c> suffix: <c>Rectangle</c>, <c>Circle</c>, <c>Polygon</c>.</summary>
    public string Name => this is RectangleShape { Size: var s } && s.X == s.Y ? "Square" : GetType().Name[..^"Shape".Length];

    /// <summary>A rectangle of the given width (X) and height (Y).</summary>
    public static Primitive2D Rectangle(float width = 0.5f, float height = 1f, float depth = 1f) => new RectangleShape(new Vector2(width, height)) { Depth = Checked(depth) };

    /// <summary>A square <paramref name="size"/> units along every side.</summary>
    public static Primitive2D Square(float size = 1f, float depth = 1f) => new RectangleShape(new Vector2(size)) { Depth = Checked(depth) };

    /// <summary>A circle of the given radius.</summary>
    public static Primitive2D Circle(float radius = 0.5f, int tessellation = 32, float depth = 1f) => new CircleShape(radius, tessellation) { Depth = Checked(depth) };

    /// <summary>A capsule standing along Y: <paramref name="height"/> from end to end, with semicircular ends of <paramref name="radius"/>.</summary>
    public static Primitive2D Capsule(float radius = 0.25f, float height = 1f, int tessellation = 16, float depth = 1f) => new CapsuleShape(radius, height, tessellation) { Depth = Checked(depth) };

    /// <summary>An isosceles triangle <paramref name="width"/> across its base and <paramref name="height"/> from base to apex.</summary>
    public static Primitive2D Triangle(float width = 1f, float height = 1f, float depth = 1f) => new TriangleShape(new Vector2(width, height)) { Depth = Checked(depth) };

    /// <summary>A regular polygon with <paramref name="sides"/> corners on a circle of <paramref name="radius"/>.</summary>
    public static Primitive2D RegularPolygon(float radius = 0.5f, int sides = 6, float depth = 1f) => new RegularPolygonShape(radius, sides) { Depth = Checked(depth) };

    /// <summary>A convex polygon from its corners, in order around the outline; at least three distinct points.</summary>
    public static Primitive2D Polygon(params Vector2[] vertices) => Polygon(1f, vertices);

    /// <summary>A convex polygon from its corners, with the collider's depth along Z.</summary>
    public static Primitive2D Polygon(float depth, params Vector2[] vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);

        if (vertices.Length < 3)
            throw new ArgumentException("A polygon needs at least three vertices.", nameof(vertices));

        return new PolygonShape(vertices) { Depth = Checked(depth) };
    }

    /// <summary>A rectangle; <see cref="Rectangle(float, float, float)"/> and <see cref="Square(float, float)"/> make one.</summary>
    public sealed record RectangleShape(Vector2 Size) : Primitive2D;

    /// <summary>A circle; <see cref="Circle(float, int, float)"/> makes one.</summary>
    public sealed record CircleShape(float Radius, int Tessellation) : Primitive2D;

    /// <summary>A capsule; <see cref="Capsule(float, float, int, float)"/> makes one. <see cref="Height"/> is the whole shape, end to end.</summary>
    public sealed record CapsuleShape(float Radius, float Height, int Tessellation) : Primitive2D;

    /// <summary>A triangle; <see cref="Triangle(float, float, float)"/> makes one.</summary>
    public sealed record TriangleShape(Vector2 Size) : Primitive2D;

    /// <summary>A regular polygon; <see cref="RegularPolygon(float, int, float)"/> makes one.</summary>
    public sealed record RegularPolygonShape(float Radius, int Sides) : Primitive2D;

    /// <summary>A polygon from its corners; <see cref="Polygon(Vector2[])"/> makes one.</summary>
    public sealed record PolygonShape(Vector2[] Vertices) : Primitive2D;
}
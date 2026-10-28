using Stride.CommunityToolkit.Rendering.Text.Meshes;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

/// <summary>
/// A 3D primitive shape with its dimensions: the thing <c>game.CreatePrimitive(Primitive3D.Sphere(0.5f))</c> builds
/// a model from, and a physics package derives the matching collider from.
/// </summary>
/// <remarks>
/// <para>
/// The set is closed: every shape is one of the nested records below, created through the static factories, which
/// name exactly the dimensions the shape has. A sphere takes a radius, a cylinder a radius and a height, a box three
/// extents; there is no shared size whose meaning changes per shape. The defaults are those of Stride's own
/// procedural models, so a factory called with no arguments gives the same mesh as <c>new SphereProceduralModel()</c>.
/// </para>
/// <para>
/// The mesh builder and each physics package switch over the same set, so a shape added to one and missed in the
/// other is a compile error, not a mesh that does not match its collider.
/// </para>
/// </remarks>
public abstract record Primitive3D
{
    private Primitive3D() { }

    /// <summary>The shape's name for labels: <c>Cube</c> for a box with three equal extents, otherwise the case's name without its <c>Shape</c> suffix: <c>Box</c>, <c>Sphere</c>, <c>Teapot</c>.</summary>
    public string Name => this is BoxShape { Size: var s } && s.X == s.Y && s.Y == s.Z ? "Cube" : GetType().Name[..^"Shape".Length];

    /// <summary>A cube <paramref name="size"/> units along every edge.</summary>
    public static Primitive3D Cube(float size = 1f) => new BoxShape(new Vector3(size));

    /// <summary>A box with the given full extents along X, Y and Z.</summary>
    public static Primitive3D Box(Vector3 size) => new BoxShape(size);

    /// <summary>A sphere of the given radius.</summary>
    public static Primitive3D Sphere(float radius = 0.5f, int tessellation = 16) => new SphereShape(radius, tessellation);

    /// <summary>A capsule: a cylinder of <paramref name="length"/> between two hemispheres of <paramref name="radius"/>, along Y.</summary>
    public static Primitive3D Capsule(float radius = 0.35f, float length = 0.5f, int tessellation = 8) => new CapsuleShape(radius, length, tessellation);

    /// <summary>A cylinder of the given radius and height, along Y.</summary>
    public static Primitive3D Cylinder(float radius = 0.5f, float height = 1f, int tessellation = 32) => new CylinderShape(radius, height, tessellation);

    /// <summary>A cone with the given base radius and height, its apex up.</summary>
    public static Primitive3D Cone(float radius = 0.5f, float height = 1f, int tessellation = 16) => new ConeShape(radius, height, tessellation);

    /// <summary>A torus: a ring of <paramref name="radius"/> from the centre to the middle of the tube, whose tube is <paramref name="thickness"/> across.</summary>
    public static Primitive3D Torus(float radius = 0.375f, float thickness = 0.125f, int tessellation = 32) => new TorusShape(radius, thickness, tessellation);

    /// <summary>A flat plane of the given width (X) and length (Z), facing up, optionally subdivided.</summary>
    public static Primitive3D Plane(float width = 1f, float length = 1f, int tessellationX = 1, int tessellationY = 1) => new PlaneShape(width, length, tessellationX, tessellationY);

    /// <summary>The Utah teapot, scaled uniformly by <paramref name="size"/>.</summary>
    public static Primitive3D Teapot(float size = 1f, int tessellation = 8) => new TeapotShape(size, tessellation);

    /// <summary>A triangular prism: a triangle <c>X</c> wide and <c>Y</c> tall, extruded <c>Z</c> along the depth.</summary>
    public static Primitive3D TriangularPrism(Vector3 size) => new TriangularPrismShape(size);

    /// <summary>
    /// Solid lettering: <paramref name="text"/> in <paramref name="font"/> (the toolkit's embedded font when <see langword="null"/>),
    /// <paramref name="size"/> units per EM, extruded <paramref name="depth"/> along Z, with the <paramref name="anchor"/> of the
    /// text block at the entity's origin: its centre by default, like every other shape.
    /// </summary>
    public static Primitive3D Text(string text, MeshFont? font = null, float size = 1f, float depth = 0.25f, Anchor? anchor = Anchor.MiddleCenter)
        => new TextShape(text, font ?? MeshFont.Default, new TextMeshOptions { Size = size, Depth = depth, Anchor = anchor });

    /// <summary>Solid lettering with every option of <see cref="TextMeshOptions"/>: lines, alignment, spacing, curve quality.</summary>
    public static Primitive3D Text(string text, MeshFont font, TextMeshOptions options) => new TextShape(text, font, options);

    /// <summary>A box; <see cref="Cube(float)"/> and <see cref="Box(Vector3)"/> make one.</summary>
    public sealed record BoxShape(Vector3 Size) : Primitive3D;

    /// <summary>A sphere; <see cref="Sphere(float, int)"/> makes one.</summary>
    public sealed record SphereShape(float Radius, int Tessellation) : Primitive3D;

    /// <summary>A capsule; <see cref="Capsule(float, float, int)"/> makes one. <see cref="Length"/> is the cylinder between the two hemispheres.</summary>
    public sealed record CapsuleShape(float Radius, float Length, int Tessellation) : Primitive3D;

    /// <summary>A cylinder; <see cref="Cylinder(float, float, int)"/> makes one.</summary>
    public sealed record CylinderShape(float Radius, float Height, int Tessellation) : Primitive3D;

    /// <summary>A cone; <see cref="Cone(float, float, int)"/> makes one.</summary>
    public sealed record ConeShape(float Radius, float Height, int Tessellation) : Primitive3D;

    /// <summary>A torus; <see cref="Torus(float, float, int)"/> makes one.</summary>
    public sealed record TorusShape(float Radius, float Thickness, int Tessellation) : Primitive3D;

    /// <summary>A plane; <see cref="Plane(float, float, int, int)"/> makes one.</summary>
    public sealed record PlaneShape(float Width, float Length, int TessellationX, int TessellationY) : Primitive3D;

    /// <summary>A teapot; <see cref="Teapot(float, int)"/> makes one.</summary>
    public sealed record TeapotShape(float Size, int Tessellation) : Primitive3D;

    /// <summary>A triangular prism; <see cref="TriangularPrism(Vector3)"/> makes one.</summary>
    public sealed record TriangularPrismShape(Vector3 Size) : Primitive3D;

    /// <summary>Solid lettering; <see cref="Text(string, MeshFont, float, float, Anchor?)"/> makes one.</summary>
    public sealed record TextShape(string Content, MeshFont Font, TextMeshOptions Options) : Primitive3D;
}
namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

/// <summary>
/// Specifies the type of primitive model to be created. Available options: Sphere, Cube, Cylinder, Torus, Plane, InfinitePlane, Teapot, Cone, Capsule. Source Stride.Assets.Presentation.Preview.
/// </summary>
/// <remarks>
/// This enumeration provides a variety of basic geometric shapes that can be utilized for creating 3D models in the game.
/// </remarks>
public enum PrimitiveModelType
{
    /// <summary>
    /// Represents a capsule primitive model.
    /// </summary>
    Capsule,

    /// <summary>
    /// Represents a cone primitive model.
    /// </summary>
    Cone,

    /// <summary>
    /// Represents a cube primitive model.
    /// </summary>
    Cube,

    /// <summary>
    /// Represents a cylinder primitive model.
    /// </summary>
    Cylinder,

    /// <summary>
    /// Represents an infinite plane primitive model.
    /// </summary>
    InfinitePlane,

    /// <summary>
    /// Represents a plane primitive model.
    /// </summary>
    Plane,

    /// <summary>
    /// Represents a rectangular prism primitive model.
    /// </summary>
    RectangularPrism,

    /// <summary>
    /// Represents a sphere primitive model.
    /// </summary>
    Sphere,

    /// <summary>
    /// Represents a teapot primitive model.
    /// </summary>
    Teapot,

    /// <summary>
    /// Represents a torus primitive model.
    /// </summary>
    Torus,

    /// <summary>
    /// Represents a triangular prism primitive model.
    /// </summary>
    TriangularPrism
}
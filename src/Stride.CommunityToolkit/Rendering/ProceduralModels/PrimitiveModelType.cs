namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

/// <summary>
/// Specifies the type of primitive model to be created. Available options: Sphere, Cube, Cylinder, Torus, Plane, InfinitePlane, Teapot, Cone, Capsule. Source Stride.Assets.Presentation.Preview.
/// </summary>
/// <remarks>
/// This enumeration provides a variety of basic geometric shapes that can be utilized for creating 3D models in the game.
/// </remarks>
public enum PrimitiveModelType
{
    Capsule,
    Cone,
    Cube,
    Cylinder,
    InfinitePlane,
    Plane,
    RectangularPrism,
    Sphere,
    Teapot,
    Torus,
    TriangularPrism
}
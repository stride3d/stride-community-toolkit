namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

/// <summary>
/// Specifies the types of 2D primitive models that can be created.
/// </summary>
public enum Primitive2DModelType
{
    /// <summary>
    /// A capsule shape, which is a rectangle with semi-circular ends.
    /// </summary>
    Capsule,

    /// <summary>
    /// A circle shape with a customizable radius.
    /// </summary>
    Circle,

    /// <summary>
    /// A polygon shape with a customizable number of sides.
    /// </summary>
    Polygon,

    /// <summary>
    /// A quadrilateral shape, which can represent any four-sided polygon.
    /// </summary>
    Quad,

    /// <summary>
    /// A rectangular shape with customizable width and height.
    /// </summary>
    Rectangle,

    /// <summary>
    /// A square shape with equal width and height.
    /// </summary>
    Square,

    /// <summary>
    /// A triangular shape with three sides.
    /// </summary>
    Triangle
}
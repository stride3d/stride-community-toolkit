namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// The plane a <see cref="ReferenceGrid"/> lies in when it shows world coordinates.
/// </summary>
public enum GridPlane
{
    /// <summary>
    /// Chosen from the camera: <see cref="XY"/> under an orthographic camera, as in a 2D scene, and
    /// <see cref="XZ"/> under a perspective one.
    /// </summary>
    Auto,

    /// <summary>The upright plane a 2D scene is built in. Covers whatever the camera sees.</summary>
    XY,

    /// <summary>The ground plane of a 3D scene. Covers a square around the origin.</summary>
    XZ,
}
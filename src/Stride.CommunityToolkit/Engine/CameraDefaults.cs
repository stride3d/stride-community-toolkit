namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides default settings for camera initialization within a game or scene,
/// including default positions, rotations, and the main camera name.
/// </summary>
public static class CameraDefaults
{
    /// <summary>
    /// Specifies the default initial position for a 3D camera within the game or scene.
    /// This position is set as (6, 6, 6) by default, representing the X, Y, and Z coordinates.
    /// </summary>
    public static readonly Vector3 Initial3DPosition = new(6, 6, 6);

    /// <summary>
    /// Specifies the default initial rotation (in degrees) for a 3D camera within the game or scene.
    /// The rotation is set as (45, -30, 0) by default, representing rotations around the Yaw (X), Pitch (Y), and Roll (Z) axes.
    /// </summary>
    public static readonly Vector3 Initial3DRotation = new(45, -30, 0);

    /// <summary>
    /// Specifies the default initial position for a 2D camera within the game or scene.
    /// This position is set as (0, 0, 50), placing the camera far enough from the origin to see the 2D plane.
    /// </summary>
    public static readonly Vector3 Initial2DPosition = new(0, 0, 50);

    /// <summary>
    /// Specifies the default initial rotation for a 2D camera within the game or scene.
    /// This rotation is set as (0, 0, 0) by default, representing no rotation around the X, Y, and Z axes.
    /// </summary>
    public static readonly Vector3 Initial2DRotation = new();

    /// <summary>
    /// The default name for the main camera used in Stride game projects.
    /// </summary>
    public const string MainCameraName = "Main";
}
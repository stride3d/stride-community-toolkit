namespace Stride.CommunityToolkit.Engine;

public static class CameraDefaults
{
    /// <summary>
    /// Specifies the initial position for a camera within the game or scene.
    /// This position is set as (6, 6, 6) by default.
    /// </summary>
    public static readonly Vector3 InitialPosition = new(6, 6, 6);

    /// <summary>
    /// Specifies the initial rotation (in degrees) for a camera within the game or scene.
    /// This rotation is set as (45, -30, 0) by default, representing rotations around the X, Y, and Z axes respectively.
    /// </summary>
    public static readonly Vector3 InitialRotation = new(45, -30, 0);

    /// <summary>
    /// Main camera name used be default in Stride
    /// </summary>
    public const string MainCameraName = "Main";
}
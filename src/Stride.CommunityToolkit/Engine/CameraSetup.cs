using Stride.Engine;
using Stride.Engine.Processors;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The camera of a <see cref="SceneSetup"/>: its projection, where it starts, and whether it gets the camera
/// controller. Built through <see cref="Perspective"/> or <see cref="Orthographic"/>, then <see cref="WithController"/>.
/// </summary>
/// <param name="Projection">Perspective or orthographic.</param>
/// <param name="Position">The initial position.</param>
/// <param name="Rotation">The initial yaw, pitch and roll, in degrees.</param>
/// <param name="Controller">Whether the camera controller is added, after the camera is aimed, so its reset pose is the aimed one.</param>
/// <param name="Name">The entity name, which <see cref="GameExtensions.GetCameraEntity"/> finds it by.</param>
public sealed record CameraSetup(CameraProjectionMode Projection, Vector3 Position, Vector3 Rotation, bool Controller = false, string Name = CameraDefaults.MainCameraName)
{
    /// <summary>A perspective camera, at <see cref="CameraDefaults.Initial3DPosition"/> looking at the origin unless told otherwise.</summary>
    public static CameraSetup Perspective(Vector3? position = null, Vector3? rotation = null)
        => new(CameraProjectionMode.Perspective, position ?? CameraDefaults.Initial3DPosition, rotation ?? CameraDefaults.Initial3DRotation);

    /// <summary>An orthographic camera, at <see cref="CameraDefaults.Initial2DPosition"/> looking down Z unless told otherwise.</summary>
    public static CameraSetup Orthographic(Vector3? position = null, Vector3? rotation = null)
        => new(CameraProjectionMode.Orthographic, position ?? CameraDefaults.Initial2DPosition, rotation ?? CameraDefaults.Initial2DRotation);

    /// <summary>The same camera with the controller: the 3D one for a perspective camera, the 2D one for an orthographic camera.</summary>
    public CameraSetup WithController() => this with { Controller = true };

    internal Entity Apply(Game game)
    {
        var entity = game.Add3DCamera(Name, Position, Rotation, Projection);

        if (!Controller) return entity;

        if (Projection == CameraProjectionMode.Orthographic)
            entity.Add2DCameraController();
        else
            entity.Add3DCameraController();

        return entity;
    }
}
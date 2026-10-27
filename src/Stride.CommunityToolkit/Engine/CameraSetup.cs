using Stride.CommunityToolkit.Rendering;
using Stride.Engine;
using Stride.Engine.Processors;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The camera of a <see cref="SceneSetup"/>: its projection, where it starts, and whether it gets the camera
/// controller. Built through <see cref="Perspective"/> or <see cref="Orthographic"/>, then <see cref="WithController"/>.
/// </summary>
/// <param name="Projection">Perspective or orthographic.</param>
/// <param name="Position">The initial position.</param>
/// <param name="Rotation">
/// The initial rotation in degrees, packed as <c>X = Yaw</c>, <c>Y = Pitch</c>, <c>Z = Roll</c> - the same convention
/// <see cref="GameExtensions.Add3DCamera"/> uses for its <c>initialRotation</c>, and the order the camera controller's F2 panel prints.
/// </param>
/// <param name="Controller">Whether the camera controller is added, after the camera is aimed, so its reset pose is the aimed one.</param>
/// <param name="Name">The entity name, which <see cref="GameExtensions.GetCameraEntity"/> finds it by.</param>
/// <param name="HelpPosition">Where the 3D controller's key help goes, or <see langword="null"/> for the overlay's own anchor.</param>
/// <param name="OrthographicSize">The height of the view in world units for an orthographic camera, or <see langword="null"/> for the engine's default of 10.</param>
/// <param name="ShowHelp">Whether the 3D controller puts its key help on the overlay at all.</param>
public sealed record CameraSetup(CameraProjectionMode Projection, Vector3 Position, Vector3 Rotation, bool Controller = false, string Name = CameraDefaults.MainCameraName, Anchor? HelpPosition = null, float? OrthographicSize = null, bool ShowHelp = true)
{
    /// <summary>A perspective camera, at <see cref="CameraDefaults.Initial3DPosition"/> looking at the origin unless told otherwise.</summary>
    /// <param name="position">The initial position, or <see langword="null"/> for <see cref="CameraDefaults.Initial3DPosition"/>.</param>
    /// <param name="rotation">The initial rotation in degrees, <c>X = Yaw</c>, <c>Y = Pitch</c>, <c>Z = Roll</c>, or <see langword="null"/> for <see cref="CameraDefaults.Initial3DRotation"/>.</param>
    public static CameraSetup Perspective(Vector3? position = null, Vector3? rotation = null)
        => new(CameraProjectionMode.Perspective, position ?? CameraDefaults.Initial3DPosition, rotation ?? CameraDefaults.Initial3DRotation);

    /// <summary>An orthographic camera, at <see cref="CameraDefaults.Initial2DPosition"/> looking down Z unless told otherwise.</summary>
    /// <param name="position">The initial position, or <see langword="null"/> for <see cref="CameraDefaults.Initial2DPosition"/>.</param>
    /// <param name="rotation">The initial rotation in degrees, <c>X = Yaw</c>, <c>Y = Pitch</c>, <c>Z = Roll</c>, or <see langword="null"/> for <see cref="CameraDefaults.Initial2DRotation"/>.</param>
    /// <param name="orthographicSize">The height of the view in world units, or <see langword="null"/> for the engine's default of 10.</param>
    public static CameraSetup Orthographic(Vector3? position = null, Vector3? rotation = null, float? orthographicSize = null)
        => new(CameraProjectionMode.Orthographic, position ?? CameraDefaults.Initial2DPosition, rotation ?? CameraDefaults.Initial2DRotation, OrthographicSize: orthographicSize);

    /// <summary>The same camera with the controller: the 3D one for a perspective camera, the 2D one for an orthographic camera.</summary>
    /// <param name="helpPosition">Where the 3D controller's key help goes; <see langword="null"/> leaves the overlay's own anchor.</param>
    /// <param name="showHelp"><see langword="false"/> keeps the 3D controller but puts no key help on the overlay.</param>
    public CameraSetup WithController(Anchor? helpPosition = null, bool showHelp = true) => this with { Controller = true, HelpPosition = helpPosition, ShowHelp = showHelp };

    internal Entity Apply(Game game)
    {
        var entity = game.Add3DCamera(Name, Position, Rotation, Projection);

        if (OrthographicSize is { } size)
            entity.Get<CameraComponent>().OrthographicSize = size;

        if (!Controller) return entity;

        if (Projection == CameraProjectionMode.Orthographic)
            entity.Add2DCameraController();
        else
            entity.Add3DCameraController(HelpPosition, showHelp: ShowHelp);

        return entity;
    }
}
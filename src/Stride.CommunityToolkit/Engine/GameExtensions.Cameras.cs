using Stride.CommunityToolkit.Rendering;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Input;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The camera helpers: adding a 2D or 3D camera to the root scene, attaching a controller, moving and rotating it, and
/// finding its entity.
/// </summary>
public static partial class GameExtensions
{
    private const string NoCameraSlotMessage = "Cannot add camera: The GraphicsCompositor does not have any camera slots defined.";

    /// <summary>
    /// Adds a 2D camera entity to the game's root scene with customizable position and rotation, defaulting to orthographic projection.
    /// </summary>
    /// <param name="game">The Game instance to which the camera entity will be added.</param>
    /// <param name="cameraName">Name for the camera entity and camera slot. Defaults to "Main". If null, the entity is not named.</param>
    /// <param name="initialPosition">Initial position for the camera entity. If not provided, the camera will be positioned at a default 2D position.</param>
    /// <param name="initialRotation">Initial rotation for the camera entity specified in degrees. If not provided, the camera will be rotated to the default 2D orientation.</param>
    /// <returns>The created Entity object representing the 2D camera.</returns>
    /// <remarks>
    /// The camera entity will be created with an orthographic projection mode and added to the game's root scene. It will also be assigned to the first available camera slot in the GraphicsCompositor.
    /// </remarks>
    public static Entity Add2DCamera(this IGame game, string? cameraName = CameraDefaults.MainCameraName, Vector3? initialPosition = null, Vector3? initialRotation = null)
    {
        return game.Add3DCamera(
            cameraName,
            initialPosition ?? CameraDefaults.Initial2DPosition,
            initialRotation ?? CameraDefaults.Initial2DRotation,
            CameraProjectionMode.Orthographic);
    }

    /// <summary>
    /// Adds a 3D camera entity to the game's root scene with customizable position, rotation, projection mode and default camera name "Main".
    /// </summary>
    /// <param name="game">The Game instance to which the camera entity will be added.</param>
    /// <param name="cameraName">Name for the camera entity and camera slot. Defaults to "Main". If null, the entity is not named.</param>
    /// <param name="initialPosition">Initial position for the camera entity. If not provided, the camera will be positioned at a default 3D position (6, 6, 6).</param>
    /// <param name="initialRotation">Initial rotation for the camera entity specified in degrees. If not provided, the camera will be rotated to face towards the origin with default angles (Yaw: 45, Pitch: -30, Roll: 0).</param>
    /// <param name="projectionMode">The projection mode for the camera (Perspective or Orthographic). Defaults to Perspective.</param>
    /// <returns>The created Entity object representing the 3D camera.</returns>
    /// <remarks>
    /// The camera entity will be created with the specified projection mode and added to the game's root scene. It will also be assigned to the first available camera slot in the GraphicsCompositor.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="GraphicsCompositor"/> is not set in the game's <see cref="SceneSystem"/>, or has no camera slot.</exception>
    public static Entity Add3DCamera(this IGame game, string? cameraName = CameraDefaults.MainCameraName,
        Vector3? initialPosition = null, Vector3? initialRotation = null,
        CameraProjectionMode projectionMode = CameraProjectionMode.Perspective)
    {
        var cameras = game.RequireGraphicsCompositor().Cameras;

        if (cameras.Count == 0)
            throw new InvalidOperationException(NoCameraSlotMessage);

        cameras[0].Name = cameraName;

        var entity = new Entity(cameraName)
        {
            new CameraComponent
            {
                Projection = projectionMode,
                Slot = cameras[0].ToSlotId(),
            }
        };

        entity.Transform.Position = initialPosition ?? CameraDefaults.Initial3DPosition;
        entity.Transform.Rotation = RotationFromDegrees(initialRotation ?? CameraDefaults.Initial3DRotation);

        entity.Scene = game.GetSceneSystem().SceneInstance.RootScene;

        return entity;
    }

    /// <summary>
    /// Adds a 2D camera controller to the specified camera entity in the game's current scene.
    /// </summary>
    /// <remarks>This method extends the game to simplify attaching a 2D camera controller to a camera entity.
    /// The camera entity must exist in the root scene; otherwise, an exception is thrown.</remarks>
    /// <param name="game">The game instance containing the scene and camera entities to which the controller will be added.</param>
    /// <param name="cameraName">The name of the camera entity to attach the 2D camera controller to. If not specified, the main camera name is
    /// used.</param>
    /// <returns>The camera entity to which the 2D camera controller was added.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no camera entity with the specified name exists in the current scene.</exception>
    /// <param name="helpToggleKey">The key that collapses and expands the camera's help.</param>
    /// <param name="helpCollapsed">Whether the help starts collapsed to a single reminder line. Collapsed by default.</param>
    public static Entity Add2DCameraController(this IGame game,
        string? cameraName = CameraDefaults.MainCameraName,
        Keys helpToggleKey = Keys.F2,
        bool helpCollapsed = true)
    {
        var cameraEntity = GetCameraEntity(game, cameraName);

        cameraEntity.Add2DCameraController(helpToggleKey, helpCollapsed);

        return cameraEntity;
    }

    /// <summary>
    /// Adds a 3D camera controller to the specified camera entity in the game's current scene.
    /// </summary>
    /// <param name="game">The game instance containing the scene and camera entities to which the controller will be added.</param>
    /// <param name="helpPosition">Where the shared <see cref="Rendering.Overlay.TextOverlay"/> is drawn. <see langword="null"/>, the default,
    /// leaves the overlay's own position alone.</param>
    /// <param name="cameraName">The name of the camera entity to attach the 3D camera controller to. If not specified, the main camera name is
    /// used.</param>
    /// <returns>The camera entity to which the 3D camera controller was added.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no camera entity with the specified name exists in the current scene.</exception>
    /// <param name="helpToggleKey">The key that collapses and expands the camera's help.</param>
    /// <param name="helpCollapsed">Whether the help starts collapsed to a single reminder line. Collapsed by default.</param>
    /// <param name="showHelp"><see langword="false"/> keeps the controller but puts no key help on the overlay.</param>
    public static Entity Add3DCameraController(this IGame game,
        Anchor? helpPosition = null,
        string? cameraName = CameraDefaults.MainCameraName,
        Keys helpToggleKey = Keys.F2,
        bool helpCollapsed = true,
        bool showHelp = true)
    {
        var cameraEntity = GetCameraEntity(game, cameraName);

        cameraEntity.Add3DCameraController(helpPosition, helpToggleKey, helpCollapsed, showHelp);

        return cameraEntity;
    }

    /// <summary>
    /// Moves the existing camera to a new position, leaving its rotation alone.
    /// </summary>
    /// <param name="game">The game whose camera is moved.</param>
    /// <param name="position">The new world position.</param>
    /// <param name="cameraName">The camera entity's name. Defaults to the main camera name.</param>
    /// <returns>The camera entity, so calls can be chained.</returns>
    /// <exception cref="InvalidOperationException">No camera entity with that name exists in the root scene.</exception>
    /// <remarks>
    /// <para>
    /// For adjusting the camera that the scene setup has already created. Calling
    /// <see cref="Add3DCamera"/> a second time is not the way to do it: that method builds a
    /// <em>new</em> entity, binds it to camera slot 0 and adds it to the root scene, leaving two camera
    /// entities competing for one slot.
    /// </para>
    /// <para>
    /// Paired with <see cref="SetCameraRotation"/> rather than folded into one call, so that each line
    /// of an example says exactly what it does.
    /// </para>
    /// </remarks>
    public static Entity SetCameraPosition(this IGame game, Vector3 position,
        string? cameraName = CameraDefaults.MainCameraName)
    {
        var cameraEntity = game.GetCameraEntity(cameraName);

        cameraEntity.Transform.Position = position;

        return cameraEntity;
    }

    /// <summary>
    /// Rotates the existing camera, leaving its position alone.
    /// </summary>
    /// <param name="game">The game whose camera is rotated.</param>
    /// <param name="rotation">
    /// The rotation in degrees, packed as <c>X = Yaw</c>, <c>Y = Pitch</c>, <c>Z = Roll</c> - the same
    /// convention <see cref="Add3DCamera"/> uses for its <c>initialRotation</c>, and the order the
    /// camera controller's F2 panel prints.
    /// </param>
    /// <param name="cameraName">The camera entity's name. Defaults to the main camera name.</param>
    /// <returns>The camera entity, so calls can be chained.</returns>
    /// <exception cref="InvalidOperationException">No camera entity with that name exists in the root scene.</exception>
    /// <remarks>
    /// Yaw/pitch/roll rather than a quaternion because these numbers are meant to be read off the screen
    /// and typed back in. See <see cref="SetCameraPosition"/> for why this is not
    /// <see cref="Add3DCamera"/>.
    /// </remarks>
    public static Entity SetCameraRotation(this IGame game, Vector3 rotation,
        string? cameraName = CameraDefaults.MainCameraName)
    {
        var cameraEntity = game.GetCameraEntity(cameraName);

        cameraEntity.Transform.Rotation = RotationFromDegrees(rotation);

        return cameraEntity;
    }

    /// <summary>
    /// Gets the camera entity by name from the game's root scene.
    /// </summary>
    /// <param name="game">The game whose root scene is searched.</param>
    /// <param name="cameraName">The camera entity's name. Defaults to the main camera name.</param>
    /// <returns>The camera entity.</returns>
    /// <exception cref="InvalidOperationException">No entity with that name exists in the root scene.</exception>
    /// <remarks>
    /// The escape hatch for anything <see cref="SetCameraPosition"/> and <see cref="SetCameraRotation"/>
    /// do not cover - reading the transform, attaching a component, or replacing the camera outright.
    /// </remarks>
    public static Entity GetCameraEntity(this IGame game, string? cameraName = CameraDefaults.MainCameraName)
        => game.GetSceneSystem().SceneInstance.RootScene.FindEntity(cameraName!) // null finds the unnamed camera Add3DCamera(cameraName: null) makes
            ?? throw new InvalidOperationException($"No camera entity found with the name '{cameraName}' in the root scene.");

    /// <summary>A rotation from degrees packed as <c>X = Yaw</c>, <c>Y = Pitch</c>, <c>Z = Roll</c>: the camera helpers' convention.</summary>
    private static Quaternion RotationFromDegrees(Vector3 yawPitchRoll)
        => Quaternion.RotationYawPitchRoll(
            MathUtil.DegreesToRadians(yawPitchRoll.X),
            MathUtil.DegreesToRadians(yawPitchRoll.Y),
            MathUtil.DegreesToRadians(yawPitchRoll.Z));
}
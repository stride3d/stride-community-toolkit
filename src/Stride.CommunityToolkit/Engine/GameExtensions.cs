using Stride.CommunityToolkit.Renderers;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Scripts;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Colors;
using Stride.Rendering.Compositing;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides extension methods for the <see cref="Game"/> class to simplify common game setup tasks,
/// such as adding cameras, lights, and ground entities, as well as configuring scenes
/// and running the game with custom logic.
/// </summary>
public static class GameExtensions
{
    private const string NoCameraSlotMessage = "Cannot add camera: The GraphicsCompositor does not have any camera slots defined.";

    /// <summary>
    /// Starts the game loop, calling <paramref name="start"/> once the root scene exists and
    /// <paramref name="update"/> on every frame after that.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The engine creates the root scene only inside <see cref="GameBase.Run(GameContext)"/>, so
    /// scene setup cannot happen before this call. <c>Run</c> schedules a script that invokes
    /// <paramref name="start"/> on the first frame, then loops <paramref name="update"/> once per frame
    /// until the game exits. Both callbacks run on the game thread, inside the script system, with
    /// the same timing and exception behaviour as a <see cref="StartupScript"/> or <see cref="SyncScript"/>:
    /// an exception thrown by either one propagates out of this method.
    /// </para>
    /// <para>
    /// <paramref name="start"/> may be asynchronous - see the <see cref="Run(Game, Func{Scene, Task}, Action{Scene, GameTime}, GameContext)"/>
    /// overload. <paramref name="update"/> is deliberately synchronous: it is the per-frame callback, and
    /// anything that needs to await across frames belongs in an async <paramref name="start"/> that runs
    /// its own loop.
    /// </para>
    /// </remarks>
    /// <param name="game">The game to run.</param>
    /// <param name="start">Called once, with the root scene, before the first <paramref name="update"/>. Optional.</param>
    /// <param name="update">
    /// Called every frame with the current root scene and the current <see cref="GameBase.UpdateTime"/>.
    /// The scene is read fresh each frame, so after a scene switch it is the new root scene. Optional.
    /// </param>
    /// <param name="context">
    /// The windowing context to run in - a host control to render into, a fixed initial size, an SDL or
    /// headless backend. Leave <see langword="null"/> to let the engine pick the platform default.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="game"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// using var game = new Game();
    ///
    /// game.Run(start: Start, update: Update);
    ///
    /// void Start(Scene rootScene) => game.SetupBase3DScene();
    ///
    /// void Update(Scene rootScene, GameTime time) { /* per frame */ }
    /// </code>
    /// </example>
    public static void Run(this Game game, Action<Scene>? start = null, Action<Scene, GameTime>? update = null, GameContext? context = null)
    {
        RunCore(game, start is null ? null : scene => { start(scene); return Task.CompletedTask; }, update, context);
    }

    /// <summary>
    /// Starts the game loop with an asynchronous <paramref name="start"/>: the update loop begins only
    /// after the returned task completes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <paramref name="start"/> runs as a script, so it can wait on the script system between steps -
    /// <c>await game.Script.NextFrame()</c> to let physics attach, or
    /// <see cref="ScriptSystemExtensions.Delay(ScriptSystem, float)"/> for a countdown - without writing
    /// a <see cref="StartupScript"/> or <see cref="AsyncScript"/> class. A <paramref name="start"/> that never
    /// completes is valid, and is how to write a per-frame loop that also awaits: the same idiom as
    /// <see cref="AsyncScript.Execute"/>.
    /// </para>
    /// <para>
    /// C# binds an <c>async</c> lambda or a <see cref="Task"/>-returning method group to this overload
    /// and a synchronous one to <see cref="Run(Game, Action{Scene}, Action{Scene, GameTime}, GameContext)"/>,
    /// so an <c>async</c> start is never silently compiled as <c>async void</c>.
    /// </para>
    /// </remarks>
    /// <param name="game">The game to run.</param>
    /// <param name="start">Called once with the root scene; the update loop starts when its task completes.</param>
    /// <param name="update">
    /// Called every frame with the current root scene and the current <see cref="GameBase.UpdateTime"/>.
    /// The scene is read fresh each frame, so after a scene switch it is the new root scene. Optional.
    /// </param>
    /// <param name="context">
    /// The windowing context to run in. Leave <see langword="null"/> to let the engine pick the platform default.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="game"/> or <paramref name="start"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// using var game = new Game();
    ///
    /// game.Run(start: async rootScene =>
    /// {
    ///     game.SetupBase3DScene();
    ///
    ///     await game.Script.Delay(3);   // countdown, splash, loading...
    ///
    ///     SpawnPlayer(rootScene);
    /// }, update: Update);
    /// </code>
    /// </example>
    public static void Run(this Game game, Func<Scene, Task> start, Action<Scene, GameTime>? update = null, GameContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(start);

        RunCore(game, start, update, context);
    }

    private static void RunCore(Game game, Func<Scene, Task>? start, Action<Scene, GameTime>? update, GameContext? context)
    {
        ArgumentNullException.ThrowIfNull(game);

        game.Script.Scheduler.Add(RootScript);

        // Opt-in, environment-driven, and a no-op unless a capture was asked for. Every example reaches
        // the loop through Run, which is what makes this one place instead of sixty.
        ScreenshotCapture.TrySchedule(game);

        game.Run(context);

        async Task RootScript()
        {
            if (start is not null)
                await start(RootScene());

            if (update is null) return;

            while (true)
            {
                update(RootScene(), game.UpdateTime);

                await game.Script.NextFrame();
            }
        }

        // Re-read every frame on purpose. Both SceneSystem.SceneInstance and SceneInstance.RootScene
        // have public setters and replacing them is how a game switches scenes; a scene captured once
        // here would keep handing update the detached one.
        Scene RootScene() => game.SceneSystem.SceneInstance.RootScene;
    }

    /// <summary>
    /// Configures the game for 2D rendering by setting up the necessary graphics compositor and camera.
    /// </summary>
    /// <param name="game">The game instance to configure for 2D rendering.</param>
    /// <param name="clearColor">The color used to clear the screen. Defaults to <see cref="Color.CornflowerBlue"/> if not specified.</param>
    public static void SetupBase2D(this Game game, Color? clearColor = null)
    {
        game.Add2DGraphicsCompositor(clearColor).AddUIStage();
        game.Add2DCamera();
    }

    /// <summary>
    /// Sets up essential components for the game, including a GraphicsCompositor, a camera, and a directional light.
    /// </summary>
    /// <remarks>
    /// This method performs the following operations:
    /// <list type="bullet">
    /// <item><description>Adds a default GraphicsCompositor to the game's SceneSystem and applies a clean UI stage.</description></item>
    /// <item><description>Adds a camera to the game.</description></item>
    /// <item><description>Adds a directional light to the game.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="game">The Game instance that will receive the base setup.</param>
    public static void SetupBase3D(this Game game)
    {
        game.AddGraphicsCompositor().AddCleanUIStage();
        game.Add3DCamera();
        game.AddDirectionalLight();
    }

    /// <summary>
    /// Adds a default graphics compositor with post-processing effects enabled to the specified game.
    /// </summary>
    /// <param name="game">The game to which the graphics compositor will be added. Cannot be null.</param>
    /// <param name="clearColor">The color used to clear the screen. Defaults to <see cref="Color.CornflowerBlue"/> if not specified.</param>
    /// <param name="msaa">Multisample anti-aliasing level. Defaults to <see cref="MultisampleCount.None"/>; use <see cref="MultisampleCount.X4"/>
    /// when the scene draws thin geometry such as line meshes, which otherwise flickers as it moves. Clamped to what the device supports.</param>
    /// <returns>The newly created <see cref="GraphicsCompositor"/> with post-processing effects enabled.</returns>
    public static GraphicsCompositor AddGraphicsCompositor(this Game game, Color? clearColor = null, MultisampleCount msaa = MultisampleCount.None)
    {
        var graphicsCompositor = GraphicsCompositorHelper.CreateDefault(enablePostEffects: true, clearColor: clearColor);

        if (graphicsCompositor.SingleView is ForwardRenderer forwardRenderer)
        {
            forwardRenderer.MSAALevel = msaa;
        }

        game.SceneSystem.GraphicsCompositor = graphicsCompositor;

        return graphicsCompositor;
    }

    /// <summary>
    /// Adds a 2D graphics compositor to the specified game, optionally setting a clear color.
    /// </summary>
    /// <remarks>This method sets the graphics compositor of the game's scene system to a default 2D
    /// configuration without post-processing effects. The clear color can be specified to customize the background
    /// color of the rendered scene.</remarks>
    /// <param name="game">The game to which the 2D graphics compositor will be added. Cannot be null.</param>
    /// <param name="clearColor">The color used to clear the screen. Defaults to <see cref="Color.CornflowerBlue"/> if not specified.</param>
    /// <param name="msaa">Multisample anti-aliasing level. Defaults to <see cref="MultisampleCount.None"/>; use <see cref="MultisampleCount.X4"/>
    /// when the scene draws thin geometry such as line meshes, which otherwise flickers as it moves. Clamped to what the device supports.</param>
    /// <returns>The newly created 2D graphics compositor.</returns>
    public static GraphicsCompositor Add2DGraphicsCompositor(this Game game, Color? clearColor = null, MultisampleCount msaa = MultisampleCount.None)
    {
        var graphicsCompositor = GraphicsCompositorHelper2D.CreateDefault(enablePostEffects: false, clearColor: clearColor, msaa: msaa);

        game.SceneSystem.GraphicsCompositor = graphicsCompositor;

        return graphicsCompositor;
    }

    /// <summary>
    /// Adds a 2D camera entity to the game's root scene with customizable position and rotation, defaulting to orthographic projection.
    /// </summary>
    /// <param name="game">The Game instance to which the camera entity will be added.</param>
    /// <param name="cameraName">Optional name for the camera entity and camera slot. Defaults to "MainCamera" if not provided. If null, the entity will not be named.</param>
    /// <param name="initialPosition">Initial position for the camera entity. If not provided, the camera will be positioned at a default 2D position.</param>
    /// <param name="initialRotation">Initial rotation for the camera entity specified in degrees. If not provided, the camera will be rotated to the default 2D orientation.</param>
    /// <returns>The created Entity object representing the 2D camera.</returns>
    /// <remarks>
    /// The camera entity will be created with an orthographic projection mode and added to the game's root scene. It will also be assigned to the first available camera slot in the GraphicsCompositor.
    /// </remarks>
    public static Entity Add2DCamera(this Game game, string? cameraName = CameraDefaults.MainCameraName, Vector3? initialPosition = null, Vector3? initialRotation = null)
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
    /// <param name="cameraName">Optional name for the camera entity and camera slot. Defaults to "MainCamera" if not provided. If null, the entity will not be named.</param>
    /// <param name="initialPosition">Initial position for the camera entity. If not provided, the camera will be positioned at a default 3D position (6, 6, 6).</param>
    /// <param name="initialRotation">Initial rotation for the camera entity specified in degrees. If not provided, the camera will be rotated to face towards the origin with default angles (Yaw: 45, Pitch: -30, Roll: 0).</param>
    /// <param name="projectionMode">The projection mode for the camera (Perspective or Orthographic). Defaults to Perspective.</param>
    /// <returns>The created Entity object representing the 3D camera.</returns>
    /// <remarks>
    /// The camera entity will be created with the specified projection mode and added to the game's root scene. It will also be assigned to the first available camera slot in the GraphicsCompositor.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if the GraphicsCompositor does not have any camera slots defined.</exception>
    public static Entity Add3DCamera(this Game game, string? cameraName = CameraDefaults.MainCameraName,
        Vector3? initialPosition = null, Vector3? initialRotation = null,
        CameraProjectionMode projectionMode = CameraProjectionMode.Perspective)
    {
        var cameras = game.SceneSystem.GraphicsCompositor.Cameras;

        if (cameras.Count == 0)
            throw new InvalidOperationException(NoCameraSlotMessage);

        var cameraSlot = cameras[0];

        cameraSlot.Name = cameraName;

        initialPosition ??= CameraDefaults.Initial3DPosition;
        initialRotation ??= CameraDefaults.Initial3DRotation;

        var entity = new Entity(cameraName)
        {
            new CameraComponent
            {
                Projection = projectionMode,
                Slot = cameras[0].ToSlotId(),
            }
        };

        entity.Transform.Position = initialPosition.Value;
        entity.Transform.Rotation = Quaternion.RotationYawPitchRoll(
            MathUtil.DegreesToRadians(initialRotation.Value.X),
            MathUtil.DegreesToRadians(initialRotation.Value.Y),
            MathUtil.DegreesToRadians(initialRotation.Value.Z)
        );

        entity.Scene = game.SceneSystem.SceneInstance.RootScene;

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
    public static Entity Add2DCameraController(this Game game,
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
    /// <param name="displayPosition">Where the shared <see cref="Scripts.Utilities.DebugOverlay"/> is drawn. <see langword="null"/>, the default,
    /// leaves the overlay's own position alone; <see cref="DisplayPosition.None"/> registers no camera help at all.</param>
    /// <param name="cameraName">The name of the camera entity to attach the 2D camera controller to. If not specified, the main camera name is
    /// used.</param>
    /// <returns>The camera entity to which the 3D camera controller was added.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no camera entity with the specified name exists in the current scene.</exception>
    /// <param name="helpToggleKey">The key that collapses and expands the camera's help.</param>
    /// <param name="helpCollapsed">Whether the help starts collapsed to a single reminder line. Collapsed by default.</param>
    public static Entity Add3DCameraController(this Game game,
        DisplayPosition? displayPosition = null,
        string? cameraName = CameraDefaults.MainCameraName,
        Keys helpToggleKey = Keys.F2,
        bool helpCollapsed = true)
    {
        var cameraEntity = GetCameraEntity(game, cameraName);

        cameraEntity.Add3DCameraController(displayPosition, helpToggleKey, helpCollapsed);

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
    /// For adjusting the camera that <c>SetupBase3DScene</c> has already created. Calling
    /// <see cref="Add3DCamera"/> a second time is not the way to do it: that method builds a
    /// <em>new</em> entity, binds it to camera slot 0 and adds it to the root scene, leaving two camera
    /// entities competing for one slot.
    /// </para>
    /// <para>
    /// Paired with <see cref="SetCameraRotation"/> rather than folded into one call, so that each line
    /// of an example says exactly what it does.
    /// </para>
    /// </remarks>
    public static Entity SetCameraPosition(this Game game, Vector3 position,
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
    public static Entity SetCameraRotation(this Game game, Vector3 rotation,
        string? cameraName = CameraDefaults.MainCameraName)
    {
        var cameraEntity = game.GetCameraEntity(cameraName);

        cameraEntity.Transform.Rotation = Quaternion.RotationYawPitchRoll(
            MathUtil.DegreesToRadians(rotation.X),
            MathUtil.DegreesToRadians(rotation.Y),
            MathUtil.DegreesToRadians(rotation.Z));

        return cameraEntity;
    }

    /// <summary>
    /// Adds a directional light entity to the game's root scene with optional customization.
    /// </summary>
    /// <param name="game">The Game instance to which the directional light will be added.</param>
    /// <param name="entityName">Optional name for the new directional light entity. If null, the entity will not be named.</param>
    /// <param name="enableShadows">Whether the light casts shadows. Defaults to <see langword="true"/>.</param>
    /// <param name="intensity">Brightness of the light. Defaults to 20.</param>
    /// <returns>The created Entity object representing the directional light.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a directional light with the following default settings:
    /// - Intensity: 20.0f
    /// - Position: (0, 2.0f, 0)
    /// - Rotation: X-axis rotated by -30 degrees and Y-axis rotated by -180 degrees.
    /// - Shadow Enabled: True
    /// - Shadow Size: Large
    /// - Shadow Filter: PCF (Percentage Closer Filtering) with a filter size of 5x5
    /// </para>
    /// <para>The entity will be added to the game's root scene. You can customize the light properties by accessing the returned Entity object.</para>
    /// <para>
    /// Note that <paramref name="enableShadows"/> only controls shadows <em>cast</em> by geometry onto
    /// other geometry. It does not flatten shading: a surface facing away from the light is still
    /// darkened by the diffuse N·L term, whatever this is set to. For a surface that reads as the same
    /// colour from every angle, use an emissive material such as <see cref="CreateFlatMaterial"/>.
    /// </para>
    /// </remarks>
    public static Entity AddDirectionalLight(this Game game, string? entityName = "Directional Light", bool enableShadows = true, float intensity = 20.0f)
    {
        var entity = new Entity(entityName)
        {
            new LightComponent
            {
                Intensity = intensity,
                Type = new LightDirectional
                {
                    Color = new ColorRgbProvider(Color.White),
                    Shadow =
                    {
                        Enabled = enableShadows,
                        Size = LightShadowMapSize.Large,
                        Filter = new LightShadowMapFilterTypePcf { FilterSize = LightShadowMapFilterTypePcfSize.Filter5x5 },
                        PartitionMode = new LightDirectionalShadowMap.PartitionLogarithmic(),
                        ComputeTransmittance = false
                    }
                }
            }
        };

        entity.Transform.Position = new Vector3(0, 2.0f, 0);
        entity.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(-30.0f)) *
                                    Quaternion.RotationY(MathUtil.DegreesToRadians(-180.0f));

        entity.Scene = game.SceneSystem.SceneInstance.RootScene;

        return entity;
    }

    /// <summary>
    /// Adds directional lighting from multiple angles to the current scene, enhancing scene illumination.
    /// </summary>
    /// <param name="game">The game instance to which the lighting will be added.</param>
    /// <param name="intensity">The intensity of the light sources.</param>
    /// <param name="showLightGizmo">Specifies whether to display a gizmo for the light in the editor. Default is true.</param>
    /// <remarks>
    /// <para>
    /// This method creates six directional lights positioned around a central point, each aiming from a unique angle to simulate uniform lighting from all directions.
    /// The lights are added at predefined positions and rotations to cover the scene evenly.
    /// </para>
    /// <para>
    /// There really are six. Until this was corrected the array held only five - both horizontal axes,
    /// both depth axes, and a single vertical one - so every object in the scene was left permanently
    /// unlit from either above or below, which reads as an object whose colour is wrong on one face
    /// rather than as a missing light.
    /// </para>
    /// </remarks>
    public static void AddAllDirectionLighting(this Game game, float intensity = 5, bool showLightGizmo = true)
    {
        var position = new Vector3(7f, 2f, 0);

        // A directional light shines along its entity's forward axis, so these six rotations aim one
        // light down each of the six world axes
        var rotations = new[]
        {
            Quaternion.Identity,
            Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(180)),
            Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90)),
            Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(270)),
            Quaternion.RotationAxis(Vector3.UnitY, MathUtil.DegreesToRadians(90)),
            Quaternion.RotationAxis(Vector3.UnitY, MathUtil.DegreesToRadians(270))
        };

        foreach (var rotation in rotations)
        {
            var entity = new Entity
            {
                new LightComponent
                {
                    Intensity = intensity,
                    Type = new LightDirectional { Color = new ColorRgbProvider(Color.White) }
                }
            };

            entity.Transform.Position = position;
            entity.Transform.Rotation = rotation;
            entity.Scene = game.SceneSystem.SceneInstance.RootScene;

            if (showLightGizmo)
                entity.AddLightDirectionalGizmo(game.GraphicsDevice);
        }
    }

    /// <summary>
    /// Adds a three-point studio lighting rig - key, fill and rim - aimed at the scene's centre.
    /// </summary>
    /// <param name="game">The game instance to which the lighting will be added.</param>
    /// <param name="intensity">The key light's intensity. The fill and rim are derived from it. Default is 15.</param>
    /// <param name="yawDegrees">
    /// Horizontal direction the rig faces, in degrees about the world Y axis. The default of 45 faces
    /// the toolkit's default camera at (6, 6, 6); pass the yaw of your own camera to swing the whole
    /// rig around with it.
    /// </param>
    /// <param name="enableShadows">Whether the key light casts shadows. Only the key ever does. Default is true.</param>
    /// <param name="showLightGizmo">Specifies whether to display a gizmo for each light in the scene. Default is true.</param>
    /// <returns>The three light entities - key, fill and rim - for further adjustment.</returns>
    /// <remarks>
    /// <para>
    /// This is the photographer's rig: a bright <b>key</b> light from high on one side of the camera
    /// gives every surface its main shading and the scene its shadows; a dim <b>fill</b> from low on
    /// the other side lifts the key's shadows so they read as shadow rather than black; and a
    /// <b>rim</b> from high behind the subject traces a bright edge along silhouettes, separating
    /// objects from the background. Because the three intensities differ, curved and angled surfaces
    /// shade differently on every face - which is what makes shapes look solid.
    /// </para>
    /// <para>
    /// Compare <see cref="AddAllDirectionLighting"/>, which lights evenly from all six axes: nothing
    /// is ever dark, but everything is lit the same, so shape flattens. Prefer the studio rig for
    /// showing off models and lettering; prefer all-direction lighting when reading a surface's
    /// colour matters more than reading its form, as on a game board of colour-coded cubes.
    /// </para>
    /// <para>
    /// Directional lights ignore position, so the rig works whatever the scene's scale; positions are
    /// set anyway so the gizmos hover where each light conceptually sits.
    /// </para>
    /// </remarks>
    public static (Entity Key, Entity Fill, Entity Rim) AddStudioLighting(this Game game, float intensity = 15f, float yawDegrees = 45f, bool enableShadows = true, bool showLightGizmo = false)
    {
        // Azimuth is measured about Y: a light at azimuth a sits on the (sin a, _, cos a) side of the
        // scene and shines toward the centre. Elevation tilts it down from the horizon.
        var key = CreateStudioLight("Studio Key Light", intensity, yawDegrees + 30f, elevationDegrees: 40f, enableShadows);
        var fill = CreateStudioLight("Studio Fill Light", intensity * 0.35f, yawDegrees - 40f, elevationDegrees: 15f, enableShadows: false);
        var rim = CreateStudioLight("Studio Rim Light", intensity * 0.7f, yawDegrees + 180f, elevationDegrees: 55f, enableShadows: false);

        foreach (var entity in (ReadOnlySpan<Entity>)[key, fill, rim])
        {
            entity.Scene = game.SceneSystem.SceneInstance.RootScene;

            if (showLightGizmo)
                entity.AddLightDirectionalGizmo(game.GraphicsDevice);
        }

        return (key, fill, rim);
    }

    /// <summary>
    /// Creates one light of the studio rig, aimed at the scene centre from the given compass direction.
    /// </summary>
    private static Entity CreateStudioLight(string name, float intensity, float azimuthDegrees, float elevationDegrees, bool enableShadows)
    {
        var entity = new Entity(name)
        {
            new LightComponent
            {
                Intensity = intensity,
                Type = new LightDirectional
                {
                    Color = new ColorRgbProvider(Color.White),
                    Shadow =
                    {
                        Enabled = enableShadows,
                        Size = LightShadowMapSize.Large,
                        Filter = new LightShadowMapFilterTypePcf { FilterSize = LightShadowMapFilterTypePcfSize.Filter5x5 },
                        PartitionMode = new LightDirectionalShadowMap.PartitionLogarithmic(),
                        ComputeTransmittance = false
                    }
                }
            }
        };

        var azimuth = MathUtil.DegreesToRadians(azimuthDegrees);
        var elevation = MathUtil.DegreesToRadians(elevationDegrees);

        // Yaw about Y first, then tilt down - the same composition the toolkit camera uses. A light
        // shines along its forward (-Z) axis, so azimuth 0 with no tilt shines from +Z toward -Z.
        entity.Transform.Rotation = Quaternion.RotationY(azimuth) * Quaternion.RotationX(-elevation);

        // Purely cosmetic for a directional light: park the gizmo where the light conceptually sits
        const float gizmoRadius = 5f;

        entity.Transform.Position = new Vector3(
            MathF.Sin(azimuth) * gizmoRadius,
            MathF.Tan(elevation) * gizmoRadius,
            MathF.Cos(azimuth) * gizmoRadius);

        return entity;
    }

    /// <summary>
    /// Adds a ground gizmo to the game's root scene, attached to an existing ground entity.
    /// </summary>
    /// <param name="game">The <see cref="Game"/> instance in which the ground gizmo will be added.</param>
    /// <param name="position">
    /// The position of the gizmo in 3D space. If null, the gizmo will be placed at the origin (0, 0, 0).
    /// </param>
    /// <param name="showAxisName">
    /// A boolean indicating whether the axis names (X, Y, Z) should be displayed on the gizmo. Default is false.
    /// </param>
    /// <param name="rotateAxisNames">
    /// A boolean indicating whether the axis names should rotate to always face the camera. Default is true.
    /// </param>
    /// <remarks>
    /// The gizmo is added as a child to an existing ground entity. If the ground entity is not found, the method will return without adding the gizmo.
    /// The gizmo helps visualize the ground with axis indicators in 3D space.
    /// </remarks>
    public static void AddGroundGizmo(this Game game, Vector3? position = null, bool showAxisName = false, bool rotateAxisNames = true)
    {
        var groundEntity = game.SceneSystem.SceneInstance.RootScene.Entities.FirstOrDefault(w => w.Name == GameDefaults.DefaultGroundName);

        if (groundEntity is null) return;

        // The axis letters are world-space text, so the renderer that draws them has to be present.
        // Doing it here rather than leaving it to the caller keeps this a single call - and without it
        // the letters simply never appear, with no error to explain why.
        if (showAxisName)
        {
            game.SceneSystem.GraphicsCompositor?.EnsureSceneRenderer(() => new WorldTextRenderer());
        }

        var gizmoEntity = new Entity("Gizmo");

        gizmoEntity.AddGizmo(game.GraphicsDevice, showAxisName: showAxisName, rotateAxisNames: rotateAxisNames);

        gizmoEntity.Transform.Position = position ?? Vector3.Zero;

        groundEntity.AddChild(gizmoEntity);
    }

    /// <summary>
    /// Adds a profiler to the game, which can be toggled on/off with Left Shift + Left Ctrl + P and provides other keyboard shortcuts.
    /// Changing the filtering mode with F1, altering the sorting mode with F2, navigating result pages with F3 and F4,
    /// and adjusting the refresh interval with the plus and minus keys.
    /// </summary>
    /// <param name="game">The game to which the profiler will be added.</param>
    /// <param name="entityName">Optional name for the entity to which the <see cref="GameProfiler"/> script will be attached.</param>
    /// <returns>The entity to which the <see cref="GameProfiler"/> script was attached.</returns>
    /// <remarks>
    /// This extension method creates an entity and attaches a <see cref="GameProfiler"/> script to it, enabling in-game profiling.
    /// The profiler's behavior can be interacted with using various keyboard shortcuts as described in the <see cref="GameProfiler"/> class.
    /// </remarks>
    public static Entity AddProfiler(this Game game, string? entityName = "Game Profiler")
    {
        var entity = new Entity(entityName) { new GameProfiler() };

        entity.Scene = game.SceneSystem.SceneInstance.RootScene;

        return entity;
    }

    /// <summary>
    /// Creates a basic material with optional color, specular reflection, and microsurface smoothness values.
    /// </summary>
    /// <param name="game">The game instance used to access the graphics device.</param>
    /// <param name="color">The color of the material. Defaults to null, which will use the _defaultMaterialColor.</param>
    /// <param name="specular">The specular reflection factor of the material. Defaults to 1.0f.</param>
    /// <param name="microSurface">The microsurface smoothness value of the material. Defaults to 0.65f.</param>
    /// <returns>A new material instance with the specified or default attributes.</returns>
    public static Material CreateMaterial(this IGame game, Color? color = null, float specular = 1.0f, float microSurface = 0.65f)
    {
        var materialDescription = new MaterialDescriptor
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(color ?? GameDefaults.DefaultMaterialColor)),
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                Specular = new MaterialMetalnessMapFeature(new ComputeFloat(specular)),
                SpecularModel = new MaterialSpecularMicrofacetModelFeature(),
                MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(microSurface))
            }
        };

        return Material.New(game.GraphicsDevice, materialDescription);
    }

    /// <summary>
    /// Creates a material with flat colors ideal for 2D rendering, using emissive color unaffected by lighting.
    /// </summary>
    /// <param name="game">The game instance used to access the graphics device.</param>
    /// <param name="color">The color of the material, including alpha. Uses white if not specified.</param>
    /// <returns>A new material instance with flat coloring.</returns>
    public static Material CreateFlatMaterial(this IGame game, Color? color = null)
    {
        var materialColor = color ?? Color.White;

        var materialDescription = new MaterialDescriptor
        {
            Attributes =
            {
                Emissive = new MaterialEmissiveMapFeature(new ComputeColor(materialColor)),
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(materialColor)),
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                Specular = null,
                SpecularModel = null
            }
        };

        return Material.New(game.GraphicsDevice, materialDescription);
    }

    /// <summary>
    /// Saves a screenshot of the current frame to the specified file path.
    /// </summary>
    /// <param name="game">The game instance providing the current graphics context and render target.</param>
    /// <param name="fileName">The file path where the screenshot will be saved.</param>
    /// <param name="fileType">The image file format to use when saving the screenshot.</param>
    public static void TakeScreenShot(this IGame game, string fileName, ImageFileType fileType = ImageFileType.Png)
    {
        using var stream = File.Create(fileName);

        var commandList = game.GraphicsContext.CommandList;
        commandList.RenderTarget.Save(commandList, stream, fileType);
    }

    /// <summary>
    /// Adds a scene renderer to the game's <see cref="GraphicsCompositor"/>.
    /// </summary>
    /// <param name="game">The <see cref="Game"/> instance to add the renderer to.</param>
    /// <param name="renderer">The scene renderer to be added, inheriting from <see cref="SceneRendererBase"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="renderer"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="GraphicsCompositor"/> is not set in the game's <see cref="SceneSystem"/>.</exception>
    public static void AddSceneRenderer(this Game game, SceneRendererBase renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);

        var graphicsCompositor = game.SceneSystem.GraphicsCompositor ??
                                 throw new InvalidOperationException(GameDefaults.GraphicsCompositorNotSet);

        graphicsCompositor.AddSceneRenderer(renderer);
    }

    /// <summary>
    /// Registers the renderer that draws <see cref="WorldTextComponent"/>, once, however many times
    /// this is called.
    /// </summary>
    /// <param name="game">The game whose compositor the renderer is added to.</param>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="GraphicsCompositor"/> is not set in the game's <see cref="SceneSystem"/>.</exception>
    /// <remarks>
    /// A <see cref="WorldTextComponent"/> without this renderer simply never appears - no error, no
    /// log line, just absent text - so call this whenever world text is used. Helpers that create
    /// world text themselves, such as <see cref="AddGroundGizmo"/> with axis names, call it on your
    /// behalf; calling it again is harmless, because a duplicate renderer is not added.
    /// </remarks>
    public static void AddWorldTextRenderer(this Game game)
    {
        var graphicsCompositor = game.SceneSystem.GraphicsCompositor ??
                                 throw new InvalidOperationException(GameDefaults.GraphicsCompositorNotSet);

        graphicsCompositor.EnsureSceneRenderer(() => new WorldTextRenderer());
    }

    /// <summary>
    /// Registers the renderer that draws <see cref="EntityTextComponent"/>, once, however many times
    /// this is called.
    /// </summary>
    /// <param name="game">The game whose compositor the renderer is added to.</param>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="GraphicsCompositor"/> is not set in the game's <see cref="SceneSystem"/>.</exception>
    /// <remarks>
    /// The screen-space counterpart of <see cref="AddWorldTextRenderer"/>, with the same failure mode
    /// when forgotten: components collect, nothing draws them, and no error says why.
    /// </remarks>
    public static void AddEntityTextRenderer(this Game game)
    {
        var graphicsCompositor = game.SceneSystem.GraphicsCompositor ??
                                 throw new InvalidOperationException(GameDefaults.GraphicsCompositorNotSet);

        graphicsCompositor.EnsureSceneRenderer(() => new EntityTextRenderer());
    }

    /// <summary>
    /// Adds a root render feature to the game's graphics compositor.
    /// </summary>
    /// <param name="game">The game instance to which the render feature will be added. Cannot be null.</param>
    /// <param name="renderFeature">The root render feature to add. Cannot be null.</param>
    public static void AddRootRenderFeature(this Game game, RootRenderFeature renderFeature)
    {
        game.SceneSystem.GraphicsCompositor.AddRootRenderFeature(renderFeature);
    }

    /// <summary>
    /// Adds particle rendering capabilities to the specified game.
    /// </summary>
    /// <remarks>This method extends the game's graphics compositor by incorporating stages and features
    /// necessary for rendering particles. Ensure that the game has a valid scene system before invoking this
    /// method.</remarks>
    /// <param name="game">The game to which particle rendering stages and features will be added. Cannot be null.</param>
    public static void AddParticleRenderer(this Game game)
    {
        game.SceneSystem.GraphicsCompositor.AddParticleStagesAndFeatures();
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
    public static Entity GetCameraEntity(this Game game, string? cameraName = CameraDefaults.MainCameraName)
    {
        var cameraEntity = game.SceneSystem.SceneInstance.RootScene.Entities.FirstOrDefault(w => w.Name == cameraName);

        if (cameraEntity is null)
        {
            throw new InvalidOperationException(
                $"No camera entity found with the name '{cameraName}' in the root scene.");
        }

        return cameraEntity;
    }
}
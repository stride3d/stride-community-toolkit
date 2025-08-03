using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Scripts;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Graphics;
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
    /// <summary>
    /// Initializes the game, starts the game loop, and handles game events.
    /// </summary>
    /// <remarks>
    /// This method performs the following actions:
    /// 1. Schedules the root script for execution.
    /// 2. Initiates the game loop by calling <see cref="GameBase.Run(GameContext)"/>.
    /// 3. Invokes the provided <paramref name="start"/> and <paramref name="update"/> delegates.
    /// </remarks>
    /// <param name="game">The Game instance to initialize and run.</param>
    /// <param name="context">Optional GameContext to be used. Defaults to null.</param>
    /// <param name="start">Optional action to execute at the start of the game. Takes the root scene as a parameter.</param>
    /// <param name="update">Optional action to execute during each game loop iteration. Takes the root scene and game time as parameters.</param>
    public static void Run(this Game game, GameContext? context = null, Action<Scene>? start = null, Action<Scene, GameTime>? update = null)
    {
        game.Script.Scheduler.Add(RootScript);

        game.Run(context);

        async Task RootScript()
        {
            start?.Invoke(GetRootScene());

            if (update == null) return;

            while (true)
            {
                update.Invoke(GetRootScene(), game.UpdateTime);

                await game.Script.NextFrame();
            }
        }

        Scene GetRootScene() => game.SceneSystem.SceneInstance.RootScene;
    }

    /// <summary>
    /// Initializes the game, starts the game loop, and handles game events.
    /// </summary>
    /// <remarks>
    /// This method performs the following actions:
    /// 1. Schedules the root script for execution.
    /// 2. Initiates the game loop by calling <see cref="GameBase.Run(GameContext)"/>.
    /// 3. Invokes the provided <paramref name="start"/> and <paramref name="update"/> delegates.
    /// </remarks>
    /// <param name="game">The Game instance to initialize and run.</param>
    /// <param name="context">Optional GameContext to be used. Defaults to null.</param>
    /// <param name="start">Optional action to execute at the start of the game. Takes the game as a parameter.</param>
    /// <param name="update">Optional action to execute during each game loop iteration. Takes the game as a parameter.</param>
    public static void Run(this Game game, GameContext? context = null, Action<Game>? start = null, Action<Game>? update = null)
    {
        game.Script.Scheduler.Add(RootScript);

        game.Run(context);

        async Task RootScript()
        {
            start?.Invoke(game);

            if (update == null) return;

            while (true)
            {
                update.Invoke(game);

                await game.Script.NextFrame();
            }
        }
    }

    /// <summary>
    /// Configures the game for 2D rendering by setting up the necessary graphics compositor and camera.
    /// </summary>
    /// <param name="game">The game instance to configure for 2D rendering.</param>
    /// <param name="clearColor">The optional background color used to clear the screen. If not specified, a default color is used.</param>
    public static void SetupBase2D(this Game game, Color? clearColor = null)
    {
        //game.AddGraphicsCompositor2();
        game.Add2DGraphicsCompositor(clearColor);
        game.Add2DCamera().Add2DCameraController();
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
    /// <returns>The newly created <see cref="GraphicsCompositor"/> with post-processing effects enabled.</returns>
    public static GraphicsCompositor AddGraphicsCompositor(this Game game)
    {
        var graphicsCompositor = GraphicsCompositorHelper.CreateDefault(enablePostEffects: true);

        game.SceneSystem.GraphicsCompositor = graphicsCompositor;

        return graphicsCompositor;
    }

    public static GraphicsCompositor AddGraphicsCompositor2(this Game game)
    {
        var graphicsCompositor = GraphicsCompositorHelper.CreateDefault(enablePostEffects: false);

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
    /// <param name="clearColor">The optional clear color for the graphics compositor. If null, a default color is used.</param>
    /// <returns>The newly created 2D graphics compositor.</returns>
    public static GraphicsCompositor Add2DGraphicsCompositor(this Game game, Color? clearColor = null)
    {
        var graphicsCompositor = GraphicsCompositorHelper2D.CreateDefault(enablePostEffects: false, clearColor: clearColor);

        game.SceneSystem.GraphicsCompositor = graphicsCompositor;

        return graphicsCompositor;
    }

    /// <summary>
    /// Creates a material with flat colors that aren't affected by lighting, ideal for 2D rendering.
    /// </summary>
    /// <param name="game">The game instance used to access the graphics device.</param>
    /// <param name="color">The color of the material. Uses white if not specified.</param>
    /// <returns>A new material instance with flat coloring unaffected by lighting.</returns>
    public static Material CreateFlatMaterial(this IGame game, Color? color = null)
    {
        var materialColor = color ?? Color.White;

        var materialDescription = new MaterialDescriptor
        {
            Attributes =
        {
            // Add diffuse color
            Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(materialColor))
            {
                // The Enabled property controls whether the feature affects rendering
                Enabled = true
            },

            // Use a simple diffuse model, we'll disable lighting with lighting features removal
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),

            // Disable specular reflections completely
            Specular = null,
            SpecularModel = null,

            // Add emissive for consistent color rendering regardless of lighting
            Emissive = new MaterialEmissiveMapFeature(new ComputeColor(materialColor))
        }
        };

        return Material.New(game.GraphicsDevice, materialDescription);
    }

    // Helper method to remove lighting features from a GraphicsCompositor
    private static void RemoveLightingFeatures(GraphicsCompositor compositor)
    {
        // Find and remove all lighting-related render features
        foreach (var renderFeature in compositor.RenderFeatures)
        {
            if (renderFeature is MeshRenderFeature meshRenderFeature)
            {
                // We need to store features to remove in a separate list to avoid collection modification during enumeration
                var featuresToRemove = new List<SubRenderFeature>();

                // Use the correct type for the collection
                foreach (var feature in meshRenderFeature.RenderFeatures)
                {
                    // Check if this is a lighting-related feature
                    if (feature is ForwardLightingRenderFeature ||
                        feature.GetType().Name.Contains("Shadow"))
                    {
                        // Cast is safe since all items in meshRenderFeature.RenderFeatures are SubRenderFeature
                        featuresToRemove.Add(feature);
                    }
                }

                // Remove all identified features
                foreach (var feature in featuresToRemove)
                {
                    meshRenderFeature.RenderFeatures.Remove(feature);
                }
            }
        }
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
    public static Entity Add3DCamera(this Game game, string? cameraName = CameraDefaults.MainCameraName, Vector3? initialPosition = null, Vector3? initialRotation = null, CameraProjectionMode projectionMode = CameraProjectionMode.Perspective)
    {
        if (game.SceneSystem.GraphicsCompositor.Cameras.Count == 0)
        {
            throw new InvalidOperationException("Cannot add camera: The GraphicsCompositor does not have any camera slots defined.");
        }

        var cameraSlot = game.SceneSystem.GraphicsCompositor.Cameras[0];

        cameraSlot.Name = cameraName;

        initialPosition ??= CameraDefaults.Initial3DPosition;
        initialRotation ??= CameraDefaults.Initial3DRotation;

        var entity = new Entity(cameraName)
        {
            new CameraComponent
            {
                Projection = projectionMode,
                Slot =  game.SceneSystem.GraphicsCompositor.Cameras[0].ToSlotId(),
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
    /// Adds a directional light entity to the game's root scene with optional customization.
    /// </summary>
    /// <param name="game">The Game instance to which the directional light will be added.</param>
    /// <param name="entityName">Optional name for the new directional light entity. If null, the entity will not be named.</param>
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
    /// </remarks>
    public static Entity AddDirectionalLight(this Game game, string? entityName = "Directional Light")
    {
        var entity = new Entity(entityName)
        {
            new LightComponent
            {
                Intensity =  20.0f,
                Type = new LightDirectional
                {
                    Color = new ColorRgbProvider(Color.White),
                    Shadow =
                    {
                        Enabled = true,
                        Size = LightShadowMapSize.Large,
                        Filter = new LightShadowMapFilterTypePcf { FilterSize = LightShadowMapFilterTypePcfSize.Filter5x5 },
                        PartitionMode = new LightDirectionalShadowMap.PartitionLogarithmic(),
                        ComputeTransmittance = false
                    }
                }
            }
        };

        entity.Transform.Position = new Vector3(0, 2.0f, 0);
        entity.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(-30.0f)) * Quaternion.RotationY(MathUtil.DegreesToRadians(-180.0f));

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
    /// This method creates six directional lights positioned around a central point, each aiming from a unique angle to simulate uniform lighting from all directions.
    /// The lights are added at predefined positions and rotations to cover the scene evenly.
    /// </remarks>
    public static void AddAllDirectionLighting(this Game game, float intensity = 5, bool showLightGizmo = true)
    {
        var position = new Vector3(7f, 2f, 0);

        CreateLightEntity(GetLight(), intensity, position);

        CreateLightEntity(GetLight(), intensity, position, Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(180)));

        CreateLightEntity(GetLight(), intensity, position, Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(270)));

        CreateLightEntity(GetLight(), intensity, position, Quaternion.RotationAxis(Vector3.UnitY, MathUtil.DegreesToRadians(90)));

        CreateLightEntity(GetLight(), intensity, position, Quaternion.RotationAxis(Vector3.UnitY, MathUtil.DegreesToRadians(270)));

        LightDirectional GetLight() => new() { Color = GetColor(Color.White) };

        static ColorRgbProvider GetColor(Color color) => new(color);

        void CreateLightEntity(ILight light, float intensity, Vector3 position, Quaternion? rotation = null)
        {
            var entity = new Entity() {
                new LightComponent {
                    Intensity =  intensity,
                    Type = light
                }};

            entity.Transform.Position = position;
            entity.Transform.Rotation = rotation ?? Quaternion.Identity;
            entity.Scene = game.SceneSystem.SceneInstance.RootScene;

            if (showLightGizmo)
            {
                entity.AddLightDirectionalGizmo(game.GraphicsDevice);
            }
        }
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

        var gizmoEntity = new Entity("Gizmo");

        gizmoEntity.AddGizmo(game.GraphicsDevice, showAxisName: showAxisName, rotateAxisNames: rotateAxisNames);

        gizmoEntity.Transform.Position = position ?? Vector3.Zero;

        groundEntity.AddChild(gizmoEntity);
    }

    /// <summary>
    /// Adds a profiler to the game, which can be toggled on/off with Left Shift + Left Ctrl + P, and provides other keyboard shortcuts.
    /// Changing the filtering mode with F1, altering the sorting mode with F2, navigating result pages with F3 and F4,
    /// and adjusting the refresh interval with the plus and minus keys.
    /// </summary>
    /// <param name="game">The game to which the profiler will be added.</param>
    /// <param name="entityName">Optional name for the entity to which the <see cref="GameProfiler"/> script will be attached.</param>
    /// <returns>The entity to which the <see cref="GameProfiler"/> script was attached.</returns>
    /// <remarks>
    /// This extension method creates an entity and attaches a <see cref="GameProfiler"/> script to it, enabling in-game profiling.
    /// The profiler's behaviour can be interacted with using various keyboard shortcuts as described in the <see cref="GameProfiler"/> class.
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
                    Specular =  new MaterialMetalnessMapFeature(new ComputeFloat(specular)),
                    SpecularModel = new MaterialSpecularMicrofacetModelFeature(),
                    MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(microSurface))
                }
        };

        return Material.New(game.GraphicsDevice, materialDescription);
        //options.Size /= 2;

    }

    /// <summary>
    /// Saves a screenshot of the current frame to the specified file path.
    /// </summary>
    /// <param name="game"></param>
    /// <param name="fileName">creates a file with this name</param>
    /// <param name="fileType"></param>
    public static void TakeScreenShot(this IGame game, string fileName, ImageFileType fileType = ImageFileType.Png)
    {
        using (var stream = File.Create(fileName))
        {
            var commandList = game.GraphicsContext.CommandList;
            commandList.RenderTarget.Save(commandList, stream, fileType);
        }
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

        var graphicsCompositor = game.SceneSystem.GraphicsCompositor ?? throw new InvalidOperationException(GameDefaults.GraphicsCompositorNotSet);

        graphicsCompositor.AddSceneRenderer(renderer);
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
    /// Adds an <see cref="EntityDebugSceneRenderer"/> to the game's <see cref="GraphicsCompositor"/> for rendering entity debug information.
    /// </summary>
    /// <param name="game">The <see cref="Game"/> instance to which the entity debug renderer will be added.</param>
    /// <param name="options">Optional settings to customize the appearance of the debug renderer. If not provided, default options will be used.</param>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="GraphicsCompositor"/> is not set in the game's <see cref="SceneSystem"/>.</exception>
    /// <remarks>
    /// This method adds a custom <see cref="EntityDebugSceneRenderer"/> to the game's graphics compositor, allowing the display of debug information
    /// such as entity names and positions in a 3D scene. The renderer can be customized using the <paramref name="options"/> parameter,
    /// which allows the user to define font size, color, and other settings.
    /// </remarks>
    /// <example>
    /// The following example demonstrates how to add an entity debug renderer with default settings:
    /// <code>
    /// game.EntityDebugSceneRenderer();
    /// </code>
    /// You can also specify custom options:
    /// <code>
    /// var options = new EntityDebugRendererOptions { FontSize = 16, FontColor = Color.Red };
    /// game.EntityDebugSceneRenderer(options);
    /// </code>
    /// </example>
    public static void AddEntityDebugSceneRenderer(this Game game, EntityDebugSceneRendererOptions? options = null)
    {
        var graphicsCompositor = game.SceneSystem.GraphicsCompositor ?? throw new InvalidOperationException(GameDefaults.GraphicsCompositorNotSet);

        graphicsCompositor.AddEntityDebugRenderer(options);
    }
}
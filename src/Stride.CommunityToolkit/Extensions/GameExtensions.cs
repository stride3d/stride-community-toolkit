using Stride.CommunityToolkit.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Scripts;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Graphics;
using Stride.Physics;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;
using Stride.Rendering.Skyboxes;

namespace Stride.CommunityToolkit.Extensions;

public static class GameExtensions
{
    private const string SkyboxTexture = "skybox_texture_hdr.dds";

    /// <summary>
    /// Initializes the game, starts the game loop, and handles game events.
    /// </summary>
    /// <remarks>
    /// This method performs the following actions:
    /// 1. Schedules the root script for execution.
    /// 2. Initiates the game loop by calling <see cref="Game.Run(GameContext)"/>.
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
    /// Sets up essential components for the game including a GraphicsCompositor, a camera, and a directional light.
    /// </summary>
    /// <remarks>
    /// This method performs the following operations:
    /// 1. Adds a default GraphicsCompositor to the game's SceneSystem and applies a clean UI stage.
    /// 2. Adds a camera to the game.
    /// 3. Adds a directional light to the game.
    /// </remarks>
    /// <param name="game">The Game instance that will receive the base setup.</param>
    public static void SetupBase(this Game game)
    {
        game.AddGraphicsCompositor().AddCleanUIStage();
        game.AddCamera();
        game.AddDirectionalLight();
    }

    /// <summary>
    /// Sets up a default 3D scene for the game, similar to creating an empty project through the editor.
    /// </summary>
    /// <remarks>
    /// This method performs the following setup operations in sequence:
    /// 1. Adds a default GraphicsCompositor to the game's SceneSystem and applies a clean UI stage.
    /// 2. Adds a camera to the game and sets it up with a MouseLookCamera component.
    /// 3. Adds a directional light to the game scene.
    /// 4. Adds a skybox to the game scene.
    /// 5. Adds ground geometry to the game scene.
    /// </remarks>
    /// <param name="game">The Game instance for which the base 3D scene will be set up.</param>
    public static void SetupBase3DScene(this Game game)
    {
        game.AddGraphicsCompositor().AddCleanUIStage();
        game.AddMouseLookCamera(game.AddCamera());
        game.AddDirectionalLight();
        game.AddSkybox();
        game.AddGround();
    }

    /// <summary>
    /// Adds a default GraphicsCompositor with enabled post-effects to the specified Game instance and sets it as the game's SceneSystem GraphicsCompositor.
    /// </summary>
    /// <param name="game">The Game instance to which the GraphicsCompositor will be added.</param>
    /// <returns>The newly configured GraphicsCompositor instance with enabled post-effects.</returns>
    public static GraphicsCompositor AddGraphicsCompositor(this Game game)
    {
        // Create a default GraphicsCompositor with enabled post-effects.
        var graphicsCompositor = GraphicsCompositorHelper.CreateDefault(true);

        // Set the GraphicsCompositor for the game's SceneSystem
        game.SceneSystem.GraphicsCompositor = graphicsCompositor;

        return graphicsCompositor;
    }

    /// <summary>
    /// Adds a camera entity to the game's root scene with customizable position and rotation.
    /// </summary>
    /// <param name="game">The Game instance to which the camera entity will be added.</param>
    /// <param name="entityName">Optional name for the camera entity. If null, the entity will not be named.</param>
    /// <param name="initialPosition">Initial position for the camera entity. If null, the camera will be positioned at the default position (6, 6, 6).</param>
    /// <param name="initialRotation">Initial rotation for the camera entity specified in degrees. If null, the camera will be rotated to face towards the origin with default angles (Yaw: 45, Pitch: -30, Roll: 0).</param>
    /// <returns>The created Entity object representing the camera.</returns>
    /// <remarks>
    /// The camera entity will be created with a perspective projection mode and will be added to the game's root scene.
    /// It will also be assigned to the first available camera slot in the GraphicsCompositor.
    /// </remarks>
    public static Entity AddCamera(this Game game, string? entityName = null, Vector3? initialPosition = null, Vector3? initialRotation = null)
    {
        initialPosition ??= CameraDefaults.InitialPosition;
        initialRotation ??= CameraDefaults.InitialRotation;

        var entity = new Entity(entityName)
        {
            new CameraComponent
            {
                Projection = CameraProjectionMode.Perspective,
                Slot =  game.SceneSystem.GraphicsCompositor.Cameras[0].ToSlotId()
            }
        };

        entity.Transform.Position = initialPosition.Value;
        entity.Transform.Rotation = Quaternion.RotationYawPitchRoll(
            MathUtil.DegreesToRadians(initialRotation.Value.X),
            MathUtil.DegreesToRadians(initialRotation.Value.Y),
            MathUtil.DegreesToRadians(initialRotation.Value.Z)
        );

        game.SceneSystem.SceneInstance.RootScene.Entities.Add(entity);

        return entity;
    }

    /// <summary>
    /// Gets the time elapsed since the last game update in seconds as a single-precision floating-point number.
    /// </summary>
    /// <param name="gameTime">The IGame interface providing access to game timing information.</param>
    /// <returns>The time elapsed since the last game update in seconds.</returns>
    public static float DeltaTime(this IGame gameTime)
    {
        return (float)gameTime.UpdateTime.Elapsed.TotalSeconds;
    }

    /// <summary>
    /// Gets the time elapsed since the last game update in seconds as a double-precision floating-point number.
    /// </summary>
    /// <param name="gameTime">The IGame interface providing access to game timing information.</param>
    /// <returns>The time elapsed since the last game update in seconds with double precision.</returns>
    public static double DeltaTimeAccurate(this IGame gameTime)
    {
        return gameTime.UpdateTime.Elapsed.TotalSeconds;
    }

    public static Entity AddDirectionalLight(this Game game, string? entityName = null)
    {
        var entity = new Entity(entityName) { new LightComponent
            {
                Intensity =  20.0f,
                Type = new LightDirectional
                {
                    Shadow =
                    {
                        Enabled = true,
                        Size = LightShadowMapSize.Large,
                        Filter = new LightShadowMapFilterTypePcf { FilterSize = LightShadowMapFilterTypePcfSize.Filter5x5 },
                    }
                }
            } };

        entity.Transform.Position = new Vector3(0, 2.0f, 0);
        entity.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(-30.0f)) * Quaternion.RotationY(MathUtil.DegreesToRadians(-180.0f));

        game.SceneSystem.SceneInstance.RootScene.Entities.Add(entity);

        return entity;
    }

    public static Entity AddSkybox(this Game game, string? entityName = null)
    {
        using var stream = new FileStream($"{AppContext.BaseDirectory}Resources\\{SkyboxTexture}", FileMode.Open, FileAccess.Read);

        var texture = Texture.Load(game.GraphicsDevice, stream, TextureFlags.ShaderResource, GraphicsResourceUsage.Dynamic);

        var skyboxGeneratorContext = new SkyboxGeneratorContext(game);

        var skybox = new Skybox();

        skybox = SkyboxGenerator.Generate(skybox, skyboxGeneratorContext, texture);

        var entity = new Entity(entityName) {
                new BackgroundComponent { Intensity = 1.0f, Texture = texture },
                new LightComponent {
                    Intensity = 1.0f,
                    Type = new LightSkybox() { Skybox = skybox } }
        };

        entity.Transform.Position = new Vector3(0.0f, 2.0f, -2.0f);

        game.SceneSystem.SceneInstance.RootScene.Entities.Add(entity);

        return entity;
    }

    /// <summary>
    /// The camera entity can be moved using W, A, S, D, Q and E, arrow keys, a gamepad's left stick or dragging/scaling using multi-touch.
    /// Rotation is achieved using the Numpad, the mouse while holding the right mouse button, a gamepad's right stick, or dragging using single-touch.
    /// </summary>
    /// <param name="game"></param>
    /// <param name="cameraEntityName"></param>
    public static void AddMouseLookCamera(this Game game, Entity? cameraEntity)
    {
        cameraEntity?.Add(new BasicCameraController());
    }

    /// <summary>
    /// Adds a ground with default Size 10,10.
    /// </summary>
    /// <param name="game"></param>
    /// <param name="entityName"></param>
    /// <param name="size"></param>
    /// <param name="includeCollider">Adds a collider</param>
    /// <returns></returns>
    public static Entity AddGround(this Game game, string? entityName = null, Vector2? size = null, bool includeCollider = true)
    {
        var materialDescription = new MaterialDescriptor
        {
            Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color.FromBgra(0xFF242424))),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Specular =  new MaterialMetalnessMapFeature(new ComputeFloat(0.0f)),
                    SpecularModel = new MaterialSpecularMicrofacetModelFeature(),
                    MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.1f))
                }
        };

        var material = Material.New(game.GraphicsDevice, materialDescription);

        var validSize = size ?? new Vector2(10.0f, 10.0f);

        var groundModel = new PlaneProceduralModel
        {
            Size = validSize,
            MaterialInstance = { Material = material }
        };

        var model = groundModel.Generate(game.Services);

        var entity = new Entity(entityName) { new ModelComponent(model) };

        if (includeCollider)
        {
            var groundCollider = new StaticColliderComponent();

            groundCollider.ColliderShapes.Add(new BoxColliderShapeDesc()
            {
                Size = new Vector3(validSize.X, 1, validSize.Y),
                LocalOffset = new Vector3(0, -0.5f, 0)
            });

            entity.Add(groundCollider);
        }

        game.SceneSystem.SceneInstance.RootScene.Entities.Add(entity);

        return entity;
    }

    /// <summary>
    /// Basic default material
    /// </summary>
    /// <param name="game"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    public static Material NewDefaultMaterial(this Game game, Color? color = null)
    {
        var defaultMaterialColor = Color.FromBgra(0xFF8C8C8C);

        var materialDescription = new MaterialDescriptor
        {
            Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(color ?? defaultMaterialColor)),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Specular =  new MaterialMetalnessMapFeature(new ComputeFloat(1.0f)),
                    SpecularModel = new MaterialSpecularMicrofacetModelFeature(),
                    MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.65f))
                }
        };

        return Material.New(game.GraphicsDevice, materialDescription);
    }

    /// <summary>
    /// Creates an entity with a primitive procedural model with a primitive mesh renderer and adds appropriate collider except for Torus, Teapot and Plane.
    /// </summary>
    /// <param name="game"></param>
    /// <param name="type"></param>
    /// <param name="entityName"></param>
    /// <param name="material"></param>
    /// <param name="includeCollider">Adds a default collider except for Torus, Teapot and Plane. Default true.</param>
    /// <returns></returns>
    public static Entity CreatePrimitive(this Game game, PrimitiveModelType type, string? entityName = null, Material? material = null, bool includeCollider = true)
    {
        var proceduralModel = GetProceduralModel(type);

        var model = proceduralModel.Generate(game.Services);

        model.Materials.Add(material);

        var entity = new Entity(entityName) { new ModelComponent(model) };

        if (!includeCollider) return entity;

        var colliderShape = GetColliderShape(type);

        if (colliderShape is null) return entity;

        var collider = new RigidbodyComponent();

        collider.ColliderShapes.Add(colliderShape);

        entity.Add(collider);

        return entity;
    }

    /// <summary>
    /// Toggle profiling Left Shift + Left Ctrl + P, Toggle filtering mode F1
    /// </summary>
    /// <param name="game"></param>
    public static Entity AddProfiler(this Game game, string? entityName = null)
    {
        var entity = new Entity(entityName) { new GameProfiler() };

        game.SceneSystem.SceneInstance.RootScene.Entities.Add(entity);

        return entity;
    }

    private static PrimitiveProceduralModelBase GetProceduralModel(PrimitiveModelType type)
        => type switch
        {
            PrimitiveModelType.Plane => new PlaneProceduralModel(),
            PrimitiveModelType.Sphere => new SphereProceduralModel(),
            PrimitiveModelType.Cube => new CubeProceduralModel(),
            PrimitiveModelType.Cylinder => new CylinderProceduralModel(),
            PrimitiveModelType.Torus => new TorusProceduralModel(),
            PrimitiveModelType.Teapot => new TeapotProceduralModel(),
            PrimitiveModelType.Cone => new ConeProceduralModel(),
            PrimitiveModelType.Capsule => new CapsuleProceduralModel(),
            _ => throw new InvalidOperationException(),
        };

    private static IInlineColliderShapeDesc? GetColliderShape(PrimitiveModelType type)
        => type switch
        {
            PrimitiveModelType.Plane => null,
            PrimitiveModelType.Sphere => new SphereColliderShapeDesc(),
            PrimitiveModelType.Cube => new BoxColliderShapeDesc(),
            PrimitiveModelType.Cylinder => new CylinderColliderShapeDesc(),
            PrimitiveModelType.Torus => null,
            PrimitiveModelType.Teapot => null,
            PrimitiveModelType.Cone => new ConeColliderShapeDesc(),
            PrimitiveModelType.Capsule => new CapsuleColliderShapeDesc(),
            _ => throw new InvalidOperationException(),
        };

    /// <summary>
    /// Shows the current FPS.
    /// </summary>
    /// <param name="game"></param>
    /// <returns></returns>
    public static float FPS(this Game game)
    {
        return game.UpdateTime.FramePerSecond;
    }
}

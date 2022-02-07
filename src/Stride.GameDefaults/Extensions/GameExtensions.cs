namespace Stride.GameDefaults.Extensions;

public static class GameExtensions
{
    private const string SkyboxTexture = "skybox_texture_hdr.dds";
    private static readonly Color _defaulMaterialColor = Color.FromBgra(0xFF8C8C8C);

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
    /// Sets up the mininum: Graphics Compositor, Camera and Light
    /// </summary>
    /// <param name="game"></param>
    public static void SetupBase(this Game game)
    {
        game.AddGraphicsCompositor();
        game.AddCamera();
        game.AddLight();
    }

    /// <summary>
    /// Sets up the default scene similarly like in Stride.Assets.Entities, SceneBaseFactory; Graphics Compositor, Camera and Light, Skybox, MouseLookCamera, Ground
    /// </summary>
    /// <param name="game"></param>
    public static void SetupBase3DScene(this Game game)
    {
        game.AddGraphicsCompositor();

        game.AddMouseLookCamera(game.AddCamera());

        game.AddLight();

        game.AddSkybox();

        game.AddGround();
    }

    public static GraphicsCompositor AddGraphicsCompositor(this Game game)
    {
        // This is already build in Stride engine
        //var graphicsCompositor = GraphicsCompositorHelper.CreateDefault(true);

        // Just some extra things added
        //((ForwardRenderer)graphicsCompositor.SingleView).PostEffects = (PostProcessingEffects?)new PostProcessingEffects
        //{
        //    DepthOfField = { Enabled = false },
        //    ColorTransforms = { Transforms = { new ToneMap() } },
        //};


        // This might be used instead, I would like to update it with Clean UI
        // https://github.com/herocrab/StrideCleanUI
        var graphicsCompositor = GraphicsCompositorBuilder.Create();

        game.SceneSystem.GraphicsCompositor = graphicsCompositor;

        return graphicsCompositor;
    }

    public static Entity AddCamera(this Game game, string? entityName = null)
    {
        var entity = new Entity(entityName) { new CameraComponent {
            Projection = CameraProjectionMode.Perspective,
            Slot =  game.SceneSystem.GraphicsCompositor.Cameras[0].ToSlotId()}
        };

        entity.Transform.Position = new(6, 6, 6);
        entity.Transform.Rotation = Quaternion.RotationYawPitchRoll(
            MathUtil.DegreesToRadians(45),
            MathUtil.DegreesToRadians(-30),
            MathUtil.DegreesToRadians(0));

        game.SceneSystem.SceneInstance.RootScene.Entities.Add(entity);

        return entity;
    }

    public static Entity AddLight(this Game game, string? entityName = null)
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
        using var stream = new FileStream($"Resources\\{SkyboxTexture}", FileMode.Open, FileAccess.Read);

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
        var materialDescription = new MaterialDescriptor
        {
            Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(color ?? _defaulMaterialColor)),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Specular =  new MaterialMetalnessMapFeature(new ComputeFloat(1.0f)),
                    SpecularModel = new MaterialSpecularMicrofacetModelFeature(),
                    MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.65f))
                }
        };

        return Material.New(game.GraphicsDevice, materialDescription);
    }

    /// <summary>
    /// Creates a game object with a primitive mesh renderer and adds appropriate collider except for Torus, Teapot and Plane.
    /// </summary>
    /// <param name="game"></param>
    /// <param name="type"></param>
    /// <param name="entityName"></param>
    /// <param name="material"></param>
    /// <param name="includeCollider">Adds a default collider except for Torus, Teapot and Plane</param>
    /// <returns></returns>
    public static Entity CreatePrimitive(this Game game, PrimitiveModelType type, string? entityName = null, Material? material = null, bool includeCollider = true)
    {
        PrimitiveProceduralModelBase proceduralModel = type switch
        {
            PrimitiveModelType.Plane => new PlaneProceduralModel(),
            PrimitiveModelType.Sphere => new SphereProceduralModel(),
            PrimitiveModelType.Cube => new CubeProceduralModel(),
            PrimitiveModelType.Cylinder => new CylinderProceduralModel(),
            PrimitiveModelType.Torus => new TorusProceduralModel(),
            PrimitiveModelType.Teapot => new TeapotProceduralModel(),
            PrimitiveModelType.Cone => new ConeProceduralModel(),
            PrimitiveModelType.Capsule => new CapsuleProceduralModel(),
            _ => throw new NotImplementedException(),
        };

        var model = proceduralModel.Generate(game.Services);

        model.Materials.Add(material);

        var entity = new Entity(entityName) { new ModelComponent(model) };

        if (!includeCollider) return entity;
        
        IInlineColliderShapeDesc? colliderShape = type switch
        {
            PrimitiveModelType.Plane => null,
            PrimitiveModelType.Sphere => new SphereColliderShapeDesc(),
            PrimitiveModelType.Cube => new BoxColliderShapeDesc(),
            PrimitiveModelType.Cylinder => new CylinderColliderShapeDesc(),
            PrimitiveModelType.Torus => null,
            PrimitiveModelType.Teapot => null,
            PrimitiveModelType.Cone => new ConeColliderShapeDesc(),
            PrimitiveModelType.Capsule => new CapsuleColliderShapeDesc(),
            _ => throw new NotImplementedException(),
        };

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
}
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

const float IntensityChangeStep = 0.5f;
DebugTextPrinter? instructions = null;
LightComponent? skyBoxLightComponent = null;
float skyBoxLightIntensity = 0;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    game.SetupBase3DScene();
    var skyboxEntity = game.AddSkybox();

    skyBoxLightComponent = skyboxEntity.GetComponent<LightComponent>();
    skyBoxLightIntensity = skyBoxLightComponent?.Intensity ?? 1;

    Create3DPrimitive(scene, new Vector3(-5f, 0.5f, -1f), game.CreateMaterial(Color.Green));
    Create3DPrimitive(scene, new Vector3(-5f, 0.5f, -3f), game.CreateMaterial(Color.Green, 0.1f, 0.1f));
    Create3DPrimitive(scene, new Vector3(-5f, 0.5f, -5f), game.CreateMaterial(Color.Green, 4f, 0.75f));
    Create3DPrimitive(scene, new Vector3(-1f, 0.5f, -1f), GetMaterial1());
    Create3DPrimitive(scene, new Vector3(1f, 0.5f, -1f), GetMaterial2());
    Create3DPrimitive(scene, new Vector3(0f, 0.5f, 1f), GetMaterial3());

    InitializeDebugTextPrinter();
}

void Create3DPrimitive(Scene scene, Vector3 position, Material material)
{
    var entity = game.Create3DPrimitive(PrimitiveModelType.Cube, new() { Material = material });
    entity.Transform.Position = position;
    entity.Scene = scene;
}

void Update(Scene scene, GameTime time)
{
    if (skyBoxLightComponent == null) return;

    if (game.Input.IsKeyPressed(Keys.Z))
    {
        skyBoxLightIntensity -= IntensityChangeStep;

        skyBoxLightComponent.Intensity = skyBoxLightIntensity;
    }

    if (game.Input.IsKeyPressed(Keys.X))
    {
        skyBoxLightIntensity += IntensityChangeStep;

        skyBoxLightComponent.Intensity = skyBoxLightIntensity;
    }

    // Display on-screen instructions for the user
    DisplayInstructions();
}

Material GetMaterial1()
{
    return Material.New(game.GraphicsDevice, new()
    {
        Attributes = new()
        {
            MicroSurface = new MaterialGlossinessMapFeature
            {
                GlossinessMap = new ComputeFloat(0.9f)
            },
            Diffuse = new MaterialDiffuseMapFeature
            {
                DiffuseMap = new ComputeColor(new Color4(1, 0.3f, 0.5f, 1))
            },
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialMetalnessMapFeature
            {
                MetalnessMap = new ComputeFloat(0.0f)
            },
            SpecularModel = new MaterialSpecularMicrofacetModelFeature
            {
                Environment = new MaterialSpecularMicrofacetEnvironmentGGXPolynomial()
            },
        }
    });
}

Material GetMaterial2()
{
    return Material.New(game.GraphicsDevice, new()
    {
        Attributes = new()
        {
            MicroSurface = new MaterialGlossinessMapFeature
            {
                GlossinessMap = new ComputeFloat(0.9f)
            },
            Diffuse = new MaterialDiffuseMapFeature
            {
                DiffuseMap = new ComputeColor(Color.Blue)
            },
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialMetalnessMapFeature
            {
                MetalnessMap = new ComputeFloat(0.0f)
            },
            SpecularModel = new MaterialSpecularMicrofacetModelFeature
            {
                Environment = new MaterialSpecularMicrofacetEnvironmentGGXPolynomial()
            },
        }
    });
}

Material GetMaterial3()
{
    return Material.New(game.GraphicsDevice, new()
    {
        Attributes = new()
        {
            MicroSurface = new MaterialGlossinessMapFeature
            {
                GlossinessMap = new ComputeFloat(0.1f)
            },
            Diffuse = new MaterialDiffuseMapFeature
            {
                DiffuseMap = new ComputeColor(Color.Gold)
            },
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialMetalnessMapFeature
            {
                MetalnessMap = new ComputeFloat(0.8f)
            },
            SpecularModel = new MaterialSpecularMicrofacetModelFeature
            {
                Environment = new MaterialSpecularMicrofacetEnvironmentGGXPolynomial()
            },
        }
    });
}

void DisplayInstructions()
{
    instructions?.Print(GenerateInstructions(skyBoxLightIntensity));
}

void InitializeDebugTextPrinter()
{
    var screenSize = new Int2(game.GraphicsDevice.Presenter.BackBuffer.Width, game.GraphicsDevice.Presenter.BackBuffer.Height);

    instructions = new DebugTextPrinter()
    {
        DebugTextSystem = game.DebugTextSystem,
        TextSize = new(205, 17 * 4),
        ScreenSize = screenSize,
        Instructions = GenerateInstructions(skyBoxLightIntensity)
    };

    instructions.Initialize(DisplayPosition.BottomLeft);
}

static List<TextElement> GenerateInstructions(float skyBoxLightIntensity)
    => [
            new("GAME INSTRUCTIONS"),
            //new("Click the golden sphere and drag to move it (Y-axis locked)"),
            new("Hold Z to decrease, X to increase Skybox light intensity", Color.Yellow),
            new($"Intensity: {skyBoxLightIntensity}", Color.Yellow),
        ];

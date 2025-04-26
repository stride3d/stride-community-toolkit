using Example17_SignalR.Core;
using Example17_SignalR.Services;
using Example17_SignalR_Shared.Core;
using Example17_SignalR_Shared.Dtos;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Helpers;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Example17_SignalR.Scripts;

public class ScreenManagerScript : AsyncScript
{
    private readonly Dictionary<EntityType, Material> _materials = [];

    public override async Task Execute()
    {
        var screenService = Services.GetService<ScreenService>();

        if (screenService == null) return;

        AddMaterials();

        try
        {
            await screenService.Connection.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting connection: {ex.Message}");
        }

        var countReceiver = new EventReceiver<CountDto>(GlobalEvents.CountReceivedEventKey);

        while (Game.IsRunning)
        {
            // This example will be waitig for the event to be received
            // the rest of the code will be executed when the event is received
            //var result = await countReceiver.ReceiveAsync();
            //var formattedMessage = $"From Script: {result.Type}: {result.Count}";
            //Console.WriteLine(formattedMessage);

            // This example will be checking if the event is received
            // the rest of the code will be executed every frame
            if (countReceiver.TryReceive(out var countDto))
            {
                CreatePrimitives(countDto);
            }

            await Script.NextFrame();
        }
    }

    private void CreatePrimitives(CountDto countDto)
    {
        var formattedMessage = $"From Script: {countDto.Type}: {countDto.Count}";

        Console.WriteLine(formattedMessage);

        for (var i = 0; i < countDto.Count; i++)
        {
            var entity = Game.Create3DPrimitive(PrimitiveModelType.Cube,
                new()
                {
                    EntityName = $"Entity",
                    Material = _materials[countDto.Type],
                });

            entity.Transform.Position = VectorHelper.RandomVector3([-5, 5], [5, 10], [-5, 5]);
            entity.Scene = Entity.Scene;
        }
    }

    private void AddMaterials()
    {
        foreach (var colorType in Colours.ColourTypes)
        {
            var material = CreateMaterial(colorType.Value);

            _materials.Add(colorType.Key, material);
        }
    }

    public Material CreateMaterial(Color? color = null, float specular = 1.0f, float microSurface = 0.65f)
    {
        var materialDescription2 = new MaterialDescriptor
        {
            Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(color ?? GameDefaults.DefaultMaterialColor)),
                    //DiffuseModel = new MaterialLightmapModelFeature(),
                    Specular =  new MaterialMetalnessMapFeature(new ComputeFloat(specular)),
                    SpecularModel = new MaterialSpecularMicrofacetModelFeature(),
                    MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(microSurface))
                }
        };

        var materialDescription = new MaterialDescriptor
        {
            Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(color ?? GameDefaults.DefaultMaterialColor)),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Specular =  new MaterialMetalnessMapFeature(new ComputeFloat(0)),
                    SpecularModel = new MaterialSpecularMicrofacetModelFeature()
                    {
                        Fresnel = new MaterialSpecularMicrofacetFresnelSchlick(),
                        Visibility = new MaterialSpecularMicrofacetVisibilitySmithSchlickGGX(),
                        NormalDistribution = new MaterialSpecularMicrofacetNormalDistributionGGX(),
                        Environment = new MaterialSpecularMicrofacetEnvironmentGGXLUT(),
                    },
                    MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0)),
                }
        };

        return Material.New(Game.GraphicsDevice, materialDescription);
    }
}
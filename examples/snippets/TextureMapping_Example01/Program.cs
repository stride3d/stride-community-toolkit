using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();

    var texturePath = "Stride-logo.png";

    using var textureFile = File.Open(texturePath, FileMode.Open);

    var texture = Texture.Load(game.GraphicsDevice, textureFile);

    var materialDescriptor = new MaterialDescriptor
    {
        Attributes =
       new MaterialAttributes
       {
           Diffuse = new MaterialDiffuseMapFeature(new ComputeTextureColor(texture)),
           DiffuseModel = new MaterialDiffuseLambertModelFeature(),
           CullMode = CullMode.Back
       }
    };

    var material = Material.New(game.GraphicsDevice, materialDescriptor);

    var entity = game.Create3DPrimitive(PrimitiveModelType.Cube, new()
    {
        Material = material,
    });
    entity.Transform.Position = new Vector3(0, 8, 0);
    entity.Scene = rootScene;
}
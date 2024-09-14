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
    // Set up a base 3D scene with default lighting and camera
    game.SetupBase3DScene();

    // Load the texture from a file
    var texturePath = "Stride-logo.png";

    using var textureFile = File.Open(texturePath, FileMode.Open);

    var texture = Texture.Load(game.GraphicsDevice, textureFile);

    // Create a material descriptor and assign the loaded texture to it
    var materialDescriptor = new MaterialDescriptor
    {
        Attributes =
       new MaterialAttributes
       {
           Diffuse = new MaterialDiffuseMapFeature(new ComputeTextureColor(texture)),

           // Configures using the Lambert lighting model,
           // which simulates how light interacts with the surface
           DiffuseModel = new MaterialDiffuseLambertModelFeature(),

           // Specifies the back-face culling mode
           CullMode = CullMode.Back
       }
    };

    // Create a material instance from the descriptor
    var material = Material.New(game.GraphicsDevice, materialDescriptor);

    // Create a 3D cube primitive and assign the material to it
    var entity = game.Create3DPrimitive(PrimitiveModelType.Cube, new()
    {
        Material = material,
    });

    entity.Transform.Position = new Vector3(0, 8, 0);

    // Add the cube to the root scene
    entity.Scene = rootScene;
}
using Example04_MyraUI;
using Stride.CommunityToolkit.Extensions;
using Stride.Engine;
using Stride.Rendering.Compositing;

using var game = new Game();
game.Run(start: Start);

void Start(Scene rootScene)
{
    SetupBase3DScene();
}

void SetupBase3DScene()
{
    ConfigureGraphicsCompositor();
    game.AddMouseLookCamera(game.AddCamera());
    game.AddDirectionalLight();
    game.AddSkybox();
    game.AddGround();
}

// Create a custom graphics compositor to include the Myra UI renderer as a renderstage
void ConfigureGraphicsCompositor()
{
    // get the default graphics compositor
    var graphicsCompositor = game.AddGraphicsCompositor();

    // create a new SceneRendererCollection
    var scenRendererCollection = new SceneRendererCollection();

    // add the default renderstage to a new SceneRendererCollection
    // fun fact if you comment this out the Myra UI will render but the 3D scene will not
    scenRendererCollection.Children.Add(graphicsCompositor.Game);
    // add the Myra UI renderer to the new SceneRendererCollection
    scenRendererCollection.Children.Add(new MyraRenderer());

    // Set the new collection as the main Scene Renderer
    graphicsCompositor.Game = scenRendererCollection;
}
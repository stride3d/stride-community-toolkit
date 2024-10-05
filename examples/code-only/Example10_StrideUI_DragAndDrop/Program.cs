using Example10_StrideUI_DragAndDrop;
using Example10_StrideUI_DragAndDrop.UI;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.UI;
using Stride.UI.Controls;

SpriteFont? _font;
DragAndDropContainer _rootElement = new();
TextBlock? _textBlock = null;
CubesGenerator? _cubesGenerator = null;
int _windowId = 1;
int _cubesCount = 100;
int _totalCubes = 0;

using var game = new Game();

game.Run(start: Start);

void Start(Scene scene)
{
    //game.SetupBase3DScene();
    game.AddGraphicsCompositor().AddCleanUIStage().AddSceneRenderer(new EntityDebugRenderer());
    game.Add3DCamera().Add3DCameraController();
    game.AddDirectionalLight();
    game.Add3DGround();
    game.AddProfiler();
    game.AddSkybox();

    _cubesGenerator = new CubesGenerator(game, scene);

    AddCapsule(scene);

    LoadFont();

    AddUI(scene);
}

void AddCapsule(Scene scene)
{
    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);

    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = scene;
}

void LoadFont()
{
    _font = game.Content.Load<SpriteFont>("StrideDefaultFont");
}

void AddUI(Scene scene)
{
    var uiEntity = CreateUIEntity();

    _rootElement.Children.Add(CreateMainWindow());

    uiEntity.Scene = scene;
}

Entity CreateUIEntity()
{
    return
    [
        new UIComponent
        {
            Page = new UIPage { RootElement = _rootElement },
            RenderGroup = RenderGroup.Group31
        }
    ];
}

DragAndDropCanvas CreateMainWindow()
{
    var canvas = CreateWindow("Main Window");

    _textBlock = GetTextBlock(GetTotal());

    _textBlock.Margin = new Thickness(10, 140, 0, 0);

    canvas.Children.Add(_textBlock);

    return canvas;
}


DragAndDropCanvas CreateWindow(string title, Vector3? position = null)
{
    var canvas = new DragAndDropCanvas(title, _font!, position);
    canvas.SetPanelZIndex(_rootElement.GetNewZIndex());

    var newWindowButton = GetButton("New Window", new Vector2(10, 50));
    newWindowButton.PreviewTouchUp += NewWindowButton_PreviewTouchUp;
    canvas.Children.Add(newWindowButton);

    var generateItemsButton = GetButton("Generate Items", new Vector2(10, 90));
    generateItemsButton.PreviewTouchUp += GenerateItemsButton_PreviewTouchUp;
    canvas.Children.Add(generateItemsButton);

    return canvas;
}

void GenerateItemsButton_PreviewTouchUp(object? sender, TouchEventArgs e)
{
    GenerateCubes(_cubesCount);

    if (_textBlock is null) return;

    _textBlock.Text = GetTotal();
}

void NewWindowButton_PreviewTouchUp(object? sender, TouchEventArgs e)
    => _rootElement.Children.Add(CreateWindow($"Window {_windowId++}"));

UIElement GetButton(string title, Vector2 position) => new Button
{
    Content = GetTextBlock(title),
    BackgroundColor = new Color(100, 100, 100, 200),
    Margin = new Thickness(position.X, position.Y, 0, 0),
};

TextBlock GetTextBlock(string title) => new TextBlock
{
    Text = title,
    TextColor = Color.White,
    TextSize = 16,
    Font = _font,
    TextAlignment = TextAlignment.Center,
    VerticalAlignment = VerticalAlignment.Center
};

void GenerateCubes(int count)
{
    if (_cubesGenerator is null) return;

    for (int i = 0; i < count; i++)
    {
        _cubesGenerator.Generate(PrimitiveModelType.Sphere);

        _totalCubes++;
    }
}

string GetTotal() => $"Total Cubes: {_totalCubes}";
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

SpriteFont? _font;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();

    AddCapsule(rootScene);

    LoadFont();

    AddWindow(rootScene);
}

void AddCapsule(Scene rootScene)
{
    var entity = game.CreatePrimitive(PrimitiveModelType.Capsule);

    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = rootScene;
}

void LoadFont()
{
    _font = game.Content.Load<SpriteFont>("StrideDefaultFont");
}

void AddWindow(Scene rootScene)
{
    var uiEntity = CreateUIEntity();

    uiEntity.Scene = rootScene;
}

Entity CreateUIEntity()
{
    return new Entity
    {
        new UIComponent
        {
            Page = new UIPage { RootElement = CreateCanvas() },
            RenderGroup = RenderGroup.Group31
        }
    };
}

Canvas CreateCanvas()
{
    var canvas = new Canvas { Width = 300, Height = 100, BackgroundColor = new Color(248, 177, 149, 100) };

    canvas.Children.Add(CreateTextBlock(_font));

    return canvas;
}

TextBlock CreateTextBlock(SpriteFont? _font)
{
    if (_font is null)
    {
        Console.WriteLine("Font is null");
    }

    return new TextBlock
    {
        Text = "Hello, World",
        TextColor = Color.White,
        TextSize = 20,
        Margin = new Thickness(3, 3, 3, 0),
        Font = _font
    };
}
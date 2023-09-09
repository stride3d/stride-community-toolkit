using Stride.CommunityToolkit.Extensions;
using Stride.CommunityToolkit.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.Examples.Models;

public static class CapsuleAndWindowExample
{
    private static SpriteFont? _font;

    public static void Run()
    {
        using var game = new Game();

        game.Run(start: Start);

        void Start(Scene rootScene)
        {
            game.SetupBase3DScene();

            AddCapsule(rootScene, game.CreatePrimitive(PrimitiveModelType.Capsule));

            _font = game.Content.Load<SpriteFont>("StrideDefaultFont");

            AddWindow(rootScene);
        }
    }

    private static void AddCapsule(Scene rootScene, Entity entity)
    {
        entity.Transform.Position = new Vector3(0, 8, 0);
        entity.Scene = rootScene;
    }

    private static void AddWindow(Scene rootScene)
    {
        var uiEntity = new Entity
            {
                new UIComponent
                {
                    Page = new UIPage { RootElement = GetCanvas() },
                    RenderGroup = RenderGroup.Group31
                }
            };

        uiEntity.Scene = rootScene;
    }

    private static Canvas GetCanvas()
    {
        var canvas = new Canvas { Width = 300, Height = 100, BackgroundColor = new Color(248, 177, 149, 100) };

        canvas.Children.Add(new TextBlock
        {
            Text = "Hello, World",
            TextColor = Color.White,
            TextSize = 20,
            Margin = new Thickness(3, 3, 3, 0),
            Font = _font
        });

        return canvas;
    }
}
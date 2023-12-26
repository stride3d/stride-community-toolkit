using Example06_SaveTheCube;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Example06_CubeClicker;
public class ClickHandlerComponent : SyncScript
{
    public required DataSaver<UiData> DataSaver { get; init; }
    public TextBlockCreator TextBlockCreator { get; } = new();
    readonly List<(TextBlock text, IClickable clickable)> _clickableBlocks = [];


    public void Start()
    {
        var _font = Game.Content.Load<SpriteFont>("StrideDefaultFont");
        var entity = TextBlockCreator.CreateUIEntity(_font);
        entity.Scene = SceneSystem.SceneInstance.RootScene;
        foreach (var clickable in DataSaver.Data.Clickables)
        {
            var textBlock = TextBlockCreator.CreateTextBlock(_font);
            _clickableBlocks.Add((textBlock, clickable));
        }
    }
    public override void Update()
    {
        var mouseDown = Input.Mouse.DownButtons;
        foreach(var mouseButton in mouseDown)
        {
            foreach(var (text, clickable) in _clickableBlocks)
            {
                if (clickable.CanHandle(mouseButton))
                {
                    clickable.HandleClick(text);
                    Console.WriteLine("Pressed Mouse: "+ mouseButton);
                }
            }
        }
        if(Input.Keyboard.IsKeyDown(Keys.S))
        {
            DataSaver.Save();
        }
        if(Input.Keyboard.IsKeyDown(Keys.L))
        {
            DataSaver.TryLoad(out var data);
            DataSaver.Data = data.Data;
            Start();
        }
    }
}

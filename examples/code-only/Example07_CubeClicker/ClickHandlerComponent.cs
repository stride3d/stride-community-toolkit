using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.UI;
using Stride.UI.Controls;

namespace Example07_CubeClicker;

public class ClickHandlerComponent : AsyncScript
{
    public required DataSaver<UiData> DataSaver { get; init; }
    public TextBlockCreator TextBlockCreator { get; } = new();

    readonly List<(TextBlock text, IClickable clickable)> _clickableBlocks = [];

    private void InitHandler()
    {
        // get a font to render
        var _font = Game.Content.Load<SpriteFont>("StrideDefaultFont");

        if (TextBlockCreator.Grid != null)
        {
            // this is for reloading, flush the old textboxes
            // changing the text may be more performant
            TextBlockCreator.Grid.Children.Clear();
        }
        else
        {
            // create a new UI and add it to the scene
            var entity = TextBlockCreator.CreateUIEntity(_font);
            entity.Scene = SceneSystem.SceneInstance.RootScene;
        }
        AddClickables(_font);
    }

    private void AddClickables(SpriteFont font)
    {
        for (int i = 0; i < DataSaver.Data.Clickables.Count; i++)
        {
            IClickable? clickable = DataSaver.Data.Clickables[i];
            // create a new TextBlock
            var textBlock = TextBlockCreator.CreateTextBlock(font);
            // get the current state of the Clickable
            textBlock.Text = clickable.ToString();
            // add it to the UI
            _clickableBlocks.Add((textBlock, clickable));
            // apply the gridrow, so it gets ordered automatically in the UI
            textBlock.SetGridRow(i);
        }
    }

    public override async Task Execute()
    {
        InitHandler();
        while (Game.IsRunning)
        {
            var mouseDown = Input.Mouse.ReleasedButtons;
            foreach (var mouseButton in mouseDown)
                foreach (var (text, clickable) in _clickableBlocks)
                    // when the mousebutton suits the clickable
                    if (clickable.CanHandle(mouseButton))
                        // react to the mouse click
                        clickable.HandleClick(text);

            // Save the Data
            if (Input.Keyboard.IsKeyReleased(Keys.S))
                await DataSaver.SaveAsync();

            // Load the Data
            if (Input.Keyboard.IsKeyReleased(Keys.L))
            {
                DataSaver.Data = await DataSaver.TryLoadAsync();

                // Here we trigger a flush of the textboxes
                InitHandler();
            }

            // Delete the Datafile
            if (Input.Keyboard.IsKeyReleased(Keys.D))
                DataSaver.Delete();

            // We have to await the next frame. If we don't do this, our game will be stuck in an infinite loop
            await Script.NextFrame();
        }
    }
}
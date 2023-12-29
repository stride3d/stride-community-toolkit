using Stride.Engine;

namespace Example07_CubeClicker;

public class ClickHandlerComponent : AsyncScript
{
    private GameUI? _gameUI;

    private void Initialize() => _gameUI = Game.Services.GetService<GameUI>();

    public override async Task Execute()
    {
        Initialize();

        while (Game.IsRunning)
        {
            var releasedButtons = Input.Mouse.ReleasedButtons;

            foreach (var item in releasedButtons)
            {
                _gameUI?.HandleClick(item);
            }

            // Delete the Datafile
            //if (Input.Keyboard.IsKeyReleased(Keys.D))
            //    DataSaver.Delete();

            // We have to await the next frame. If we don't do this, our game will be stuck in an infinite loop
            await Script.NextFrame();
        }
    }
}
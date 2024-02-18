using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;

using var game = new Game();

SpriteBatch? spriteBatch = null;

SpriteFont? font = null;

game.Run(start: (Scene rootScene) =>
{
    game.SetupBase3DScene();
    game.AddSkybox();

    font = game.Content.Load<SpriteFont>("StrideDefaultFont");
    spriteBatch = new SpriteBatch(game.GraphicsDevice);

    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);

    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = rootScene;
}, update: Update);

void Update(Scene scene, GameTime time)
{

    // don't forget the begin
    spriteBatch.Begin(game.GraphicsContext);

    // draw the text "Helloworld!" in red from the center of the screen
    spriteBatch.DrawString(font, "Helloworld!", new Vector2(0.5f, 0.5f), Color.Red);

    // don't forget the end
    spriteBatch.End();
}
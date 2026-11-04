using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace E09_3D_SceneRenderer;

/// <summary>
/// A startup script that adds a renderer of its own to the compositor and draws one label over its entity
/// with a <see cref="SpriteBatch"/>, at the entity's position projected to the screen.
/// </summary>
public class SpriteBatchRendererScript : StartupScript
{
    private SpriteBatch? _spriteBatch;

    private SpriteFont? _font;

    private DelegateSceneRenderer? _renderer;

    private const float FontSize = 12;

    private CameraComponent? _camera;

    private Vector2 _offset = new(0, -80);

    /// <summary>
    /// Creates the sprite batch, loads the font and adds a <see cref="DelegateSceneRenderer"/> to the compositor.
    /// </summary>
    public override void Start()
    {
        _spriteBatch = new SpriteBatch(Game.GraphicsDevice);

        _font = Content.Load<SpriteFont>("/Stride.Engine/StrideDefaultFont");

        _camera = SceneSystem.SceneInstance.RootScene.GetCamera();

        _renderer = new DelegateSceneRenderer(Draw);
        SceneSystem.GraphicsCompositor.AddSceneRenderer(_renderer);
    }

    /// <summary>
    /// Takes the renderer out of the compositor and releases the sprite batch, so a removed entity leaves nothing drawing.
    /// </summary>
    public override void Cancel()
    {
        if (_renderer is not null && SceneSystem.GraphicsCompositor.Game is SceneRendererCollection renderers)
        {
            renderers.Children.Remove(_renderer);
        }

        _spriteBatch?.Dispose();
        _spriteBatch = null;
        _renderer = null;
    }

    /// <summary>
    /// Draws the label at the entity's screen position.
    /// </summary>
    /// <param name="drawContext">The rendering context used to issue draw commands.</param>
    private void Draw(RenderDrawContext drawContext)
    {
        if (_spriteBatch is null || _font is null || _camera is null) return;

        // Only while the entity is in front of the camera and on screen
        if (!_camera.TryWorldToScreenPoint(Entity.Transform.Position, GraphicsDevice, out var screen)) return;

        _spriteBatch.Begin(drawContext.GraphicsContext);

        _spriteBatch.DrawString(_font, "Example 3: I am a capsule", FontSize, screen + _offset, Color.Red);

        _spriteBatch.End();
    }
}
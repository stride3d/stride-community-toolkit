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

        SceneSystem.GraphicsCompositor.AddSceneRenderer(new DelegateSceneRenderer(Draw));
    }

    /// <summary>
    /// Draws the label at the entity's screen position.
    /// </summary>
    /// <param name="drawContext">The rendering context used to issue draw commands.</param>
    private void Draw(RenderDrawContext drawContext)
    {
        if (_spriteBatch is null || _font is null || _camera is null) return;

        // ViewProjection transforms a world-space position into clip space (pre-perspective divide)
        var viewProjection = _camera.ViewProjectionMatrix;

        var worldPos = Entity.Transform.Position;

        // Transform to clip space; behind the camera is out
        var clipPosition = Vector4.Transform(new Vector4(worldPos, 1f), viewProjection);

        if (clipPosition.W <= 0f) return;

        // Perspective divide to normalized device coordinates, then the clip test: x and y in [-1,1], z in [0,1]
        var ndc = new Vector3(clipPosition.X, clipPosition.Y, clipPosition.Z) / clipPosition.W;

        if (ndc.Z < 0f || ndc.Z > 1f || MathF.Abs(ndc.X) > 1f || MathF.Abs(ndc.Y) > 1f) return;

        var screen = _camera.WorldToScreenPoint(ref worldPos, GraphicsDevice);

        _spriteBatch.Begin(drawContext.GraphicsContext);

        _spriteBatch.DrawString(_font, "Example 3: I am a capsule", FontSize, screen + _offset, Color.Red);

        _spriteBatch.End();
    }
}
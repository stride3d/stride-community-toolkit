using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Compositing;
using Stride.Rendering.Images;

namespace Stride.CommunityToolkit.Rendering.Compositing;

/// <summary>
/// Adds cameras that draw into textures: a rear-view mirror, a security monitor, a minimap, a
/// picture-in-picture, or a camera with a look of its own.
/// </summary>
public static class RenderTextureCameraExtensions
{
    /// <summary>
    /// Makes an entity's camera draw into a texture of the given size, every frame, alongside the
    /// main view. The entity gets a <see cref="CameraComponent"/> if it has none.
    /// </summary>
    /// <param name="game">The game whose compositor draws the feed.</param>
    /// <param name="cameraEntity">The entity that is the camera: already in the scene, or added to it afterwards.</param>
    /// <param name="width">The texture's width in pixels.</param>
    /// <param name="height">The texture's height in pixels.</param>
    /// <param name="format">
    /// The texture's format. The default is HDR, and it should stay HDR unless the feed tone-maps
    /// itself: the scene is lit in HDR and a feed has no tone map of its own, so an 8-bit texture
    /// saturates to white, while an HDR one is tone-mapped by the main view along with everything
    /// else drawn from it.
    /// </param>
    /// <param name="postEffects">
    /// A look for the feed. Receives a <see cref="PostProcessingEffects"/> with every effect switched
    /// off; enable what the feed should have, or add colour transforms. Left out, the feed draws the
    /// scene as it is.
    /// </param>
    /// <returns>The feed: its texture, its camera, and what was added to the compositor.</returns>
    /// <remarks>
    /// <para>
    /// The feed is a <see cref="SceneCameraRenderer"/> for a camera slot of its own, wrapping a
    /// <see cref="RenderTextureSceneRenderer"/> for the texture, wrapping a second
    /// <see cref="ForwardRenderer"/> over the compositor's existing opaque and transparent stages -
    /// the structure of the engine's own multiple-output test. Sharing the stages is what makes the
    /// feed draw the same meshes, particles and shapes as the main view. It has to be a second
    /// forward renderer: the compositor's own is reference-counted and cannot hang in two places.
    /// </para>
    /// <para>
    /// The feed is appended after the main view, so the texture shows the previous frame. Anything
    /// that submits per frame and is drawn once per view, like a shape batch, appears in the feed as
    /// it appears in the main view.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">The game has no graphics compositor, or its opaque and transparent stages are missing.</exception>
    public static RenderTextureCamera AddRenderTextureCamera(this Game game, Entity cameraEntity, int width, int height,
        PixelFormat format = PixelFormat.R16G16B16A16_Float, Action<PostProcessingEffects>? postEffects = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(cameraEntity);

        var compositor = game.SceneSystem.GraphicsCompositor
            ?? throw new InvalidOperationException("The game has no graphics compositor; call AddGraphicsCompositor or SetupBase3D first.");

        if (!compositor.TryGetRenderStage("Opaque", out var opaque) || !compositor.TryGetRenderStage("Transparent", out var transparent))
        {
            throw new InvalidOperationException("The compositor has no Opaque and Transparent render stages to draw the feed with.");
        }

        var texture = Texture.New2D(game.GraphicsDevice, width, height, format, TextureFlags.ShaderResource | TextureFlags.RenderTarget);

        // A slot of its own: slot zero is the game's camera, and two cameras on one slot is a trap
        var slot = new SceneCameraSlot { Name = $"{cameraEntity.Name} feed" };

        compositor.Cameras.Add(slot);

        var camera = cameraEntity.Get<CameraComponent>();

        if (camera is null)
        {
            camera = new CameraComponent();
            cameraEntity.Add(camera);
        }

        // The texture's shape, not the window's, or the picture is squashed
        camera.Slot = slot.ToSlotId();
        camera.UseCustomAspectRatio = true;
        camera.AspectRatio = width / (float)height;

        var forward = new ForwardRenderer
        {
            Clear = { Color = Color.Black },
            OpaqueRenderStage = opaque,
            TransparentRenderStage = transparent,
        };

        if (postEffects is not null)
        {
            // A bare PostProcessingEffects ships with bloom, ambient occlusion and the rest on
            var effects = new PostProcessingEffects();

            effects.DisableAll();
            postEffects(effects);

            // Adding a transform is asking for it; the group itself is off until something is in it
            if (effects.ColorTransforms.Transforms.Count > 0)
            {
                effects.ColorTransforms.Enabled = true;
            }

            forward.PostEffects = effects;
        }

        var renderer = new SceneCameraRenderer
        {
            Camera = slot,
            Child = new RenderTextureSceneRenderer { RenderTexture = texture, Child = forward },
        };

        compositor.AddSceneRenderer(renderer);

        return new RenderTextureCamera(compositor, texture, cameraEntity, camera, slot, renderer, forward);
    }
}
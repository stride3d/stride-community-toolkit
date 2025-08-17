using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.DebugShapes.Code;

public static class DebugShapeExtensions
{
    /// <summary>
    /// <para>Adds <see cref="ImmediateDebugRenderFeature"/> and <see cref="ImmediateDebugRenderSystem"/> to the game.</para>
    /// <para>Registers the system to the service registry for easy access.</para>
    /// </summary>
    /// <param name="game"></param>
    /// <param name="debugShapeRenderGroup"></param>
    public static void AddDebugShapes(this Game game, RenderGroup debugShapeRenderGroup = RenderGroup.Group1)
    {
        game.SceneSystem.GraphicsCompositor.AddImmediateDebugRenderFeature();

        var debugDraw = new ImmediateDebugRenderSystem(game.Services, debugShapeRenderGroup);
#if DEBUG
        debugDraw.Visible = true;
#endif
        game.Services.AddService(debugDraw);
        game.GameSystems.Add(debugDraw);
    }

    /// <summary>
    /// Adds an immediate debug render feature to the specified <see cref="GraphicsCompositor"/>.
    /// This method ensures the debug render feature is added only once and links it with both the
    /// "Opaque" and "Transparent" render stages.
    /// </summary>
    /// <param name="graphicsCompositor">The <see cref="GraphicsCompositor"/> to modify.</param>
    /// <exception cref="NullReferenceException">
    /// Thrown when the "Opaque" or "Transparent" render stages are not found in the <paramref name="graphicsCompositor"/>.
    /// </exception>
    public static void AddImmediateDebugRenderFeature(this GraphicsCompositor graphicsCompositor)
    {
        var debugRenderFeatures = graphicsCompositor.RenderFeatures.OfType<ImmediateDebugRenderFeature>();

        if (!graphicsCompositor.TryGetRenderStage("Opaque", out var opaqueRenderStage))
        {
            throw new NullReferenceException("Opaque RenderStage not found");
        }

        if (!graphicsCompositor.TryGetRenderStage("Transparent", out var transparentRenderStage))
        {
            throw new NullReferenceException("Transparent RenderStage not found");
        }

        if (!debugRenderFeatures.Any())
        {
            var newDebugRenderFeature = new ImmediateDebugRenderFeature()
            {
                RenderStageSelectors = {
                        new ImmediateDebugRenderStageSelector
                        {
                            OpaqueRenderStage = opaqueRenderStage,
                            TransparentRenderStage = transparentRenderStage
                        }
                    }
            };

            graphicsCompositor.RenderFeatures.Add(newDebugRenderFeature);
        }
    }

    /// <summary>
    /// Attempts to retrieve a render stage from the specified <see cref="GraphicsCompositor"/> based on the provided effect name.
    /// </summary>
    /// <param name="graphicsCompositor">The <see cref="GraphicsCompositor"/> containing the render stages.</param>
    /// <param name="effectName">The name of the render stage to search for.</param>
    /// <param name="renderStage">
    /// When this method returns, contains the <see cref="RenderStage"/> if the render stage was found; otherwise, <c>null</c>.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <c>true</c> if the render stage is found; otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetRenderStage(this GraphicsCompositor graphicsCompositor, string effectName, out RenderStage? renderStage)
    {
        renderStage = null;

        var renderSystem = graphicsCompositor.RenderSystem;

        for (int i = 0; i < renderSystem.RenderStages.Count; ++i)
        {
            var stage = renderSystem.RenderStages[i];

            if (stage.Name == effectName)
            {
                renderStage = stage;

                return true;
            }
        }

        return false;
    }
}
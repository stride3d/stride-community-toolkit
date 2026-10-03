using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// Submits every enabled <see cref="ShapeComponent"/> to its <see cref="ShapeBatch"/> each frame,
/// reading the plane and scale to draw in from the entity's world matrix.
/// </summary>
public sealed class ShapeProcessor : EntityProcessor<ShapeComponent>
{
    private static readonly Logger Log = GlobalLogger.GetLogger(nameof(ShapeProcessor));

    // The batch this processor made for itself, where nothing registered one, and the render
    // system it is registered with; null until needed
    private ShapeBatch? _own;
    private RenderSystem? _ownRenderSystem;

    /// <inheritdoc/>
    public override void Draw(RenderContext context)
    {
        // The game's default batch when there is one, so components share its state with everything
        // else drawn through it; otherwise one of this processor's own, registered on first use.
        // That second path is what draws components in Game Studio's scene editor, which runs the
        // processor but never calls AddShapeBatch, and in any game that did not call it either.
        var fallback = Services.GetService<ShapeBatch>() ?? OwnBatch(context);

        foreach (var kv in ComponentDatas)
        {
            var component = kv.Key;

            if (!component.Enabled || component.Vertices.Count < 1) continue;

            var batch = component.Batch ?? fallback;

            // No render system yet: the first frame, before the compositor has drawn; next frame then
            if (batch is null) continue;

            // The batch's colours, border, fill and glow are current state shared with whoever else draws
            // through it, so put back whatever was there before moving on
            var borderWidth = batch.BorderWidth;
            var fillAlpha = batch.Fill.Alpha;
            var fillColor = batch.Fill.Color;
            var glowWidth = batch.Glow.Width;
            var glowColor = batch.Glow.Color;
            var tag = batch.Tag;

            // Negative means "inherit"; a transparent colour means the same for the colours,
            // because Game Studio cannot edit nullable value types (see ShapeComponent.Inherit)
            batch.BorderWidth = component.BorderWidth < 0f ? borderWidth : component.BorderWidth;
            batch.Fill.Alpha = component.FillAlpha < 0f ? fillAlpha : component.FillAlpha;
            batch.Fill.Color = component.FillColor.A == 0 ? fillColor : component.FillColor;
            batch.Glow.Width = component.GlowWidth < 0f ? glowWidth : component.GlowWidth;
            batch.Glow.Color = component.GlowColor.A == 0 ? glowColor : component.GlowColor;
            // A pickable component is its own tag: the hit hands the component back
            batch.Tag = component.Pickable ? component : null;

            Draw(batch, component);

            batch.BorderWidth = borderWidth;
            batch.Fill.Alpha = fillAlpha;
            batch.Fill.Color = fillColor;
            batch.Glow.Width = glowWidth;
            batch.Glow.Color = glowColor;
            batch.Tag = tag;
        }
    }

    /// <summary>
    /// A depth-tested batch in the transparent stage, registered with the scene and the render system
    /// that draws it - the same wiring <c>AddShapeBatch</c> does, without a <c>Game</c>. Registered
    /// again whenever that render system changes.
    /// </summary>
    private ShapeBatch? OwnBatch(RenderContext context)
    {
        if (EntityManager is not SceneInstance sceneInstance) return null;

        // The render system that draws this scene is its scene system's compositor's. The context's
        // is whichever compositor drew last, which in Game Studio is the editor's own gizmo
        // compositor, and on a game's first frame is nothing at all.
        var renderSystem = SceneRendererRegistration.OwningCompositor(Services, sceneInstance)?.RenderSystem ?? context.RenderSystem;

        if (renderSystem is null) return null;

        if (_own is not null && ReferenceEquals(_ownRenderSystem, renderSystem)) return _own;

        // Game Studio swaps the compositor: a fallback one at start for the project's, and again on
        // every change of the view mode. A batch registered with the old render system draws in
        // nothing, so it is registered afresh with the new one.
        RemoveOwn(sceneInstance);

        _own = new ShapeBatch { DepthTest = true };
        _ownRenderSystem = renderSystem;

        ShapeBatchExtensions.Register(sceneInstance, renderSystem, _own);

        Log.Info("ShapeComponent: no ShapeBatch registered as a service, drawing through the processor's own depth-tested batch.");

        return _own;
    }

    /// <inheritdoc/>
    protected override void OnSystemRemove()
    {
        if (EntityManager is SceneInstance sceneInstance)
        {
            RemoveOwn(sceneInstance);
        }

        base.OnSystemRemove();
    }

    private void RemoveOwn(SceneInstance sceneInstance)
    {
        if (_own is not { } own) return;

        foreach (var visibilityGroup in sceneInstance.VisibilityGroups)
        {
            visibilityGroup.RenderObjects.Remove(own);
        }

        _own = null;
        _ownRenderSystem = null;
    }

    private static void Draw(ShapeBatch batch, ShapeComponent component)
    {
        // The world matrix, so parented entities work too
        ref var world = ref component.Entity.Transform.WorldMatrix;
        var position = world.TranslationVector;
        var vertices = CollectionsMarshal.AsSpan(component.Vertices);

        if (component.Billboard)
        {
            batch.DrawBillboard(vertices, position, component.Color, component.Radius);

            return;
        }

        // Rows 1 and 2 are the entity's own X and Y axes in world space, which is the plane the
        // shape lies in; their length is the scale, taken from X so the shape stays undistorted
        var axisX = new Vector3(world.M11, world.M12, world.M13);
        var axisY = new Vector3(world.M21, world.M22, world.M23);
        var scale = axisX.Length();

        if (scale <= float.Epsilon) return;

        batch.DrawSolidPolygon(vertices, position, axisX, axisY, component.Color, component.Radius, scale);
    }
}
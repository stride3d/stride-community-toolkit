// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Rendering;

namespace Stride.CommunityToolkit.DebugShapes;

/// <summary>
/// Provides functionality for rendering immediate debug primitives such as lines, shapes, and other geometric objects.
/// </summary>
/// <remarks>This class is designed to render debug primitives directly to the screen, bypassing typical rendering
/// pipelines. It supports various geometric shapes, including quads, circles, spheres, cubes, capsules, cylinders,
/// cones, and lines. The rendering process is optimized for debugging purposes, allowing developers to visualize
/// objects in real-time.</remarks>
public class ImmediateDebugRenderFeature : RootRenderFeature
{
    /// <inheritdoc/>
    public override Type SupportedRenderObjectType => typeof(ImmediateDebugRenderObject);

    private DebugPrimitiveRenderer? _primitiveRenderer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmediateDebugRenderFeature"/> class.
    /// </summary>
    /// <remarks>This constructor sets the default sort key to ensure that the feature is rendered last,
    /// preventing background elements from being rendered over objects without depth testing in the opaque
    /// stage.</remarks>
    public ImmediateDebugRenderFeature() => SortKey = 0xFF;

    /// <inheritdoc/>
    protected override void InitializeCore()
    {
        _primitiveRenderer = new DebugPrimitiveRenderer();
        _primitiveRenderer.Initialize(Context.GraphicsDevice, Context.Services);
    }

    /// <inheritdoc/>
    public override void Extract()
    {
        if (_primitiveRenderer is null)
            return;

        var lastOffset = 0;
        var lastLineOffset = 0;

        foreach (RenderObject renderObject in RenderObjects)
        {
            (lastOffset, lastLineOffset) = ((ImmediateDebugRenderObject)renderObject).ExtractFrame(_primitiveRenderer, lastOffset, lastLineOffset);
        }
    }

    /// <summary>The block the profiler shows this feature's GPU time under.</summary>
    public static readonly ProfilingKey ProfilingKey = new("DebugShapes");

    private static readonly Color4 ProfileColor = new(0.3f, 0.8f, 0.4f, 1f);

    /// <inheritdoc/>
    public override void Prepare(RenderDrawContext context)
    {
        _primitiveRenderer?.Prepare(context);
    }

    /// <inheritdoc/>
    public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
    {
        if (_primitiveRenderer is null) return;

        using var _ = context.QueryManager.BeginProfile(ProfileColor, ProfilingKey);

        var commandList = context.CommandList;

        for (var index = startIndex; index < endIndex; index++)
        {
            var renderNodeReference = renderViewStage.SortedRenderNodes[index].RenderNode;
            var debugObject = (ImmediateDebugRenderObject)GetRenderNode(renderNodeReference).RenderObject;
            var objectHasTransparency = debugObject.Stage == DebugRenderStage.Transparent;

            // render with depth test first
            _primitiveRenderer.SetPrimitiveRenderingPipelineState(commandList, depthTest: true, selectedFillMode: debugObject.CurrentFillMode, hasTransparency: objectHasTransparency);
            debugObject.Draw(_primitiveRenderer, context, renderView, depthTest: true, objectHasTransparency);

            // render without depth test second
            _primitiveRenderer.SetPrimitiveRenderingPipelineState(commandList, depthTest: false, selectedFillMode: debugObject.CurrentFillMode, hasTransparency: objectHasTransparency);
            debugObject.Draw(_primitiveRenderer, context, renderView, depthTest: false, objectHasTransparency);
        }
    }

    /// <inheritdoc/>
    public override void Flush(RenderDrawContext context) { }

    /* FIXME: is there a nicer way to handle dispose, some xenko idiom? */

    /// <inheritdoc/>
    public override void Unload()
    {
        base.Unload();
        _primitiveRenderer?.Unload();
        _primitiveRenderer = null;
    }
}
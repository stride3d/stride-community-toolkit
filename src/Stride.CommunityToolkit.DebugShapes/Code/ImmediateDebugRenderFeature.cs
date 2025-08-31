// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Rendering;

namespace Stride.CommunityToolkit.DebugShapes.Code;

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

        int lastOffset = 0;
        int lastLineOffset = 0;

        foreach (RenderObject renderObject in RenderObjects)
        {
            ImmediateDebugRenderObject debugObject = (ImmediateDebugRenderObject)renderObject;

            int primitivesWithDepth = DebugPrimitiveRenderer.SumBasicPrimitives(ref debugObject.totalPrimitives);
            int primitivesWithoutDepth = DebugPrimitiveRenderer.SumBasicPrimitives(ref debugObject.totalPrimitivesNoDepth);
            int totalThingsToDraw = primitivesWithDepth + primitivesWithoutDepth;

            _primitiveRenderer.EnsureCapacity(totalThingsToDraw, debugObject.totalPrimitives.Lines * 2 + debugObject.totalPrimitivesNoDepth.Lines * 2);

            var primitiveOffsets = DebugPrimitiveRenderer.SetupPrimitiveOffsets(ref debugObject.totalPrimitives, lastOffset);
            var primitiveOffsetsNoDepth = DebugPrimitiveRenderer.SetupPrimitiveOffsets(ref debugObject.totalPrimitivesNoDepth, primitiveOffsets.Cones + debugObject.totalPrimitives.Cones);

            primitiveOffsets.Lines = 0 + lastLineOffset;
            primitiveOffsetsNoDepth.Lines = debugObject.totalPrimitives.Lines * 2 + lastLineOffset;

            debugObject.instanceOffsets = primitiveOffsets;
            debugObject.instanceOffsetsNoDepth = primitiveOffsetsNoDepth;

            _primitiveRenderer.ProcessRenderables(debugObject.renderablesWithDepth, ref primitiveOffsets);
            _primitiveRenderer.ProcessRenderables(debugObject.renderablesNoDepth, ref primitiveOffsetsNoDepth);

            debugObject.primitivesToDraw = debugObject.totalPrimitives;
            debugObject.primitivesToDrawNoDepth = debugObject.totalPrimitivesNoDepth;

            lastOffset = debugObject.instanceOffsetsNoDepth.Cones + debugObject.totalPrimitivesNoDepth.Cones;
            lastLineOffset = debugObject.instanceOffsetsNoDepth.Lines + debugObject.totalPrimitivesNoDepth.Lines * 2;

            // Clear per-frame message queues
            debugObject.renderablesWithDepth.Clear();
            debugObject.renderablesNoDepth.Clear();
            debugObject.totalPrimitives.Clear();
            debugObject.totalPrimitivesNoDepth.Clear();
        }
    }

    /// <inheritdoc/>
    public override void Prepare(RenderDrawContext context)
    {
        _primitiveRenderer?.Prepare(context);
    }

    /// <inheritdoc/>
    public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
    {
        if (_primitiveRenderer is null) return;

        var commandList = context.CommandList;

        for (int index = startIndex; index < endIndex; index++)
        {
            var renderNodeReference = renderViewStage.SortedRenderNodes[index].RenderNode;
            var debugObject = (ImmediateDebugRenderObject)GetRenderNode(renderNodeReference).RenderObject;
            bool objectHasTransparency = debugObject.Stage == DebugRenderStage.Transparent;

            // render with depth test first
            _primitiveRenderer.SetPrimitiveRenderingPipelineState(commandList, depthTest: true, selectedFillMode: debugObject.CurrentFillMode, hasTransparency: objectHasTransparency);
            _primitiveRenderer.DrawPrimitives(context, renderView, ref debugObject.instanceOffsets, ref debugObject.primitivesToDraw, depthTest: true, fillMode: debugObject.CurrentFillMode, hasTransparency: objectHasTransparency);

            // render without depth test second
            _primitiveRenderer.SetPrimitiveRenderingPipelineState(commandList, depthTest: false, selectedFillMode: debugObject.CurrentFillMode, hasTransparency: objectHasTransparency);
            _primitiveRenderer.DrawPrimitives(context, renderView, ref debugObject.instanceOffsetsNoDepth, ref debugObject.primitivesToDrawNoDepth, depthTest: false, fillMode: debugObject.CurrentFillMode, hasTransparency: objectHasTransparency);
        }
    }

    /// <inheritdoc/>
    public override void Flush(RenderDrawContext context)
    {
    }

    /* FIXME: is there a nicer way to handle dispose, some xenko idiom? */

    /// <inheritdoc/>
    public override void Unload()
    {
        base.Unload();
        _primitiveRenderer?.Unload();
        _primitiveRenderer = null;
    }
}
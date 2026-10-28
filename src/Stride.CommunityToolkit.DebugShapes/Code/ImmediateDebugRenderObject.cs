// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.CommunityToolkit.DebugShapes;

/// <summary>
/// Represents a render object for immediate debug rendering, allowing the addition of various shapes and lines.
/// </summary>
public class ImmediateDebugRenderObject : RenderObject
{
    // This frame's shapes, one list per depth test
    private readonly List<Renderable> _renderablesWithDepth = [];
    private readonly List<Renderable> _renderablesNoDepth = [];

    // How many of each kind have been queued this frame
    private Primitives _totalPrimitives, _totalPrimitivesNoDepth;

    // Where each kind's instances start in the renderer's buffers, set when the frame is extracted
    private Primitives _instanceOffsets, _instanceOffsetsNoDepth;

    // How many of each kind the render stage draws
    private Primitives _primitivesToDraw, _primitivesToDrawNoDepth;

    /* state set from outside */
    internal FillMode CurrentFillMode { get; set; } = FillMode.Wireframe;

    internal DebugRenderStage Stage { get; set; }

    /// <summary>
    /// Adds a quad to the debug render queue.
    /// </summary>
    /// <param name="position">The position of the quad.</param>
    /// <param name="size">The size of the quad.</param>
    /// <param name="rotation">The rotation of the quad.</param>
    /// <param name="color">The color of the quad.</param>
    /// <param name="depthTest">Whether to use depth testing for rendering.</param>
    public void DrawQuad(in Vector3 position, in Vector2 size, in Quaternion rotation, in Color color, bool depthTest = true)
    {
        var cmd = new Quad { Position = position, Size = size, Rotation = rotation, Color = color };

        Push(new Renderable(in cmd), depthTest);
    }

    /// <summary>
    /// Adds a circle to the debug render queue.
    /// </summary>
    /// <param name="position">The position of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="rotation">The rotation of the circle.</param>
    /// <param name="color">The color of the circle.</param>
    /// <param name="depthTest">Whether to use depth testing for rendering.</param>
    public void DrawCircle(in Vector3 position, float radius, in Quaternion rotation, in Color color, bool depthTest = true)
    {
        var cmd = new Circle { Position = position, Radius = radius, Rotation = rotation, Color = color };

        Push(new Renderable(in cmd), depthTest);
    }

    /// <summary>
    /// Adds a sphere to the debug render queue.
    /// </summary>
    /// <param name="position">The position of the sphere.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <param name="color">The color of the sphere.</param>
    /// <param name="depthTest">Whether to use depth testing for rendering.</param>
    public void DrawSphere(in Vector3 position, float radius, in Color color, bool depthTest = true)
    {
        var cmd = new Sphere { Position = position, Radius = radius, Color = color };

        Push(new Renderable(in cmd), depthTest);
    }

    /// <summary>
    /// Adds a half-sphere to the debug render queue.
    /// </summary>
    /// <param name="position">The position of the half-sphere.</param>
    /// <param name="radius">The radius of the half-sphere.</param>
    /// <param name="rotation">The rotation of the half-sphere.</param>
    /// <param name="color">The color of the half-sphere.</param>
    /// <param name="depthTest">Whether to use depth testing for rendering.</param>
    public void DrawHalfSphere(in Vector3 position, float radius, in Quaternion rotation, in Color color, bool depthTest = true)
    {
        var cmd = new HalfSphere { Position = position, Radius = radius, Rotation = rotation, Color = color };

        Push(new Renderable(in cmd), depthTest);
    }

    /// <summary>
    /// Adds a cube to the debug render queue.
    /// </summary>
    /// <param name="start">The centre of the cube.</param>
    /// <param name="end"><paramref name="start"/> plus the cube's size.</param>
    /// <param name="rotation">The rotation of the cube.</param>
    /// <param name="color">The color of the cube.</param>
    /// <param name="depthTest">Whether to use depth testing for rendering.</param>
    public void DrawCube(in Vector3 start, in Vector3 end, in Quaternion rotation, in Color color, bool depthTest = true)
    {
        var cmd = new Cube { Start = start, End = end, Rotation = rotation, Color = color };

        Push(new Renderable(in cmd), depthTest);
    }

    /// <summary>
    /// Adds a capsule to the debug render queue.
    /// </summary>
    /// <param name="position">The position of the capsule.</param>
    /// <param name="height">The height of the capsule.</param>
    /// <param name="radius">The radius of the capsule.</param>
    /// <param name="rotation">The rotation of the capsule.</param>
    /// <param name="color">The color of the capsule.</param>
    /// <param name="depthTest">Whether to use depth testing for rendering.</param>
    public void DrawCapsule(in Vector3 position, float height, float radius, in Quaternion rotation, in Color color, bool depthTest = true)
    {
        var cmd = new Capsule { Position = position, Height = height, Radius = radius, Rotation = rotation, Color = color };

        Push(new Renderable(in cmd), depthTest);
    }

    /// <summary>
    /// Adds a cylinder to the debug render queue.
    /// </summary>
    /// <param name="position">The position of the cylinder.</param>
    /// <param name="height">The height of the cylinder.</param>
    /// <param name="radius">The radius of the cylinder.</param>
    /// <param name="rotation">The rotation of the cylinder.</param>
    /// <param name="color">The color of the cylinder.</param>
    /// <param name="depthTest">Whether to use depth testing for rendering.</param>
    public void DrawCylinder(in Vector3 position, float height, float radius, in Quaternion rotation, in Color color, bool depthTest = true)
    {
        var cmd = new Cylinder { Position = position, Height = height, Radius = radius, Rotation = rotation, Color = color };

        Push(new Renderable(in cmd), depthTest);
    }

    /// <summary>
    /// Adds a cone to the debug render queue.
    /// </summary>
    /// <param name="position">The position of the cone.</param>
    /// <param name="height">The height of the cone.</param>
    /// <param name="radius">The radius of the cone.</param>
    /// <param name="rotation">The rotation of the cone.</param>
    /// <param name="color">The color of the cone.</param>
    /// <param name="depthTest">Whether to use depth testing for rendering.</param>
    public void DrawCone(in Vector3 position, float height, float radius, in Quaternion rotation, in Color color, bool depthTest = true)
    {
        var cmd = new Cone { Position = position, Height = height, Radius = radius, Rotation = rotation, Color = color };

        Push(new Renderable(in cmd), depthTest);
    }

    /// <summary>
    /// Adds a line to the debug render queue.
    /// </summary>
    /// <param name="start">The starting position of the line.</param>
    /// <param name="end">The ending position of the line.</param>
    /// <param name="color">The color of the line.</param>
    /// <param name="depthTest">Whether to use depth testing for rendering.</param>
    public void DrawLine(in Vector3 start, in Vector3 end, in Color color, bool depthTest = true)
    {
        var cmd = new Line { Start = start, End = end, Color = color };

        Push(new Renderable(in cmd), depthTest);
    }

    /// <summary>Queues one shape in the list its depth test puts it in, and counts it under its kind.</summary>
    private void Push(in Renderable renderable, bool depthTest)
    {
        ref var counts = ref depthTest ? ref _totalPrimitives : ref _totalPrimitivesNoDepth;

        (depthTest ? _renderablesWithDepth : _renderablesNoDepth).Add(renderable);
        counts.Increment(renderable.Type);
    }

    /// <summary>
    /// Draws this frame's shapes through <paramref name="renderer"/>: the depth-tested ones or the others, as
    /// <see cref="ExtractFrame"/> laid them out.
    /// </summary>
    internal void Draw(DebugPrimitiveRenderer renderer, RenderDrawContext context, RenderView renderView, bool depthTest, bool hasTransparency)
    {
        if (depthTest)
        {
            renderer.DrawPrimitives(context, renderView, ref _instanceOffsets, ref _primitivesToDraw, depthTest: true, CurrentFillMode, hasTransparency);
        }
        else
        {
            renderer.DrawPrimitives(context, renderView, ref _instanceOffsetsNoDepth, ref _primitivesToDrawNoDepth, depthTest: false, CurrentFillMode, hasTransparency);
        }
    }

    /// <summary>
    /// Ends the frame for this object: turns everything queued since the last call into draw counts and
    /// instance offsets, hands the instance data to <paramref name="renderer"/>, and clears the queues.
    /// </summary>
    /// <param name="renderer">Destination for the instance data.</param>
    /// <param name="instanceOffset">First free instance slot; objects extracted earlier this frame occupy the slots below it.</param>
    /// <param name="lineOffset">First free line-vertex slot.</param>
    /// <returns>The first free instance and line-vertex slots after this object's data.</returns>
    internal (int InstanceOffset, int LineOffset) ExtractFrame(DebugPrimitiveRenderer renderer, int instanceOffset, int lineOffset)
    {
        renderer.EnsureCapacity(_totalPrimitives.InstanceTotal + _totalPrimitivesNoDepth.InstanceTotal, _totalPrimitives.Lines * 2 + _totalPrimitivesNoDepth.Lines * 2);

        // The depth-tested shapes first, then the rest, each kind in its own run; lines keep a buffer of their own
        var primitiveOffsets = _totalPrimitives.Offsets(instanceOffset);
        var primitiveOffsetsNoDepth = _totalPrimitivesNoDepth.Offsets(primitiveOffsets.Cones + _totalPrimitives.Cones);

        primitiveOffsets.Lines = lineOffset;
        primitiveOffsetsNoDepth.Lines = _totalPrimitives.Lines * 2 + lineOffset;

        _instanceOffsets = primitiveOffsets;
        _instanceOffsetsNoDepth = primitiveOffsetsNoDepth;

        renderer.ProcessRenderables(_renderablesWithDepth, ref primitiveOffsets);
        renderer.ProcessRenderables(_renderablesNoDepth, ref primitiveOffsetsNoDepth);

        _primitivesToDraw = _totalPrimitives;
        _primitivesToDrawNoDepth = _totalPrimitivesNoDepth;

        var next = (_instanceOffsetsNoDepth.Cones + _totalPrimitivesNoDepth.Cones, _instanceOffsetsNoDepth.Lines + _totalPrimitivesNoDepth.Lines * 2);

        // Clear per-frame message queues
        _renderablesWithDepth.Clear();
        _renderablesNoDepth.Clear();
        _totalPrimitives.Clear();
        _totalPrimitivesNoDepth.Clear();

        return next;
    }
}
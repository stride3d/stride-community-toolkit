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
    internal readonly List<Renderable> renderablesWithDepth = [];
    internal readonly List<Renderable> renderablesNoDepth = [];

    // How many of each kind have been queued this frame
    private Primitives totalPrimitives, totalPrimitivesNoDepth;

    // Where each kind's instances start in the renderer's buffers, set when the frame is extracted
    internal Primitives instanceOffsets, instanceOffsetsNoDepth;

    // How many of each kind the render stage draws
    internal Primitives primitivesToDraw, primitivesToDrawNoDepth;

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
    public void DrawQuad(ref Vector3 position, ref Vector2 size, ref Quaternion rotation, ref Color color, bool depthTest = true)
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
    public void DrawCircle(ref Vector3 position, float radius, ref Quaternion rotation, ref Color color, bool depthTest = true)
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
    public void DrawSphere(ref Vector3 position, float radius, ref Color color, bool depthTest = true)
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
    public void DrawHalfSphere(ref Vector3 position, float radius, ref Quaternion rotation, ref Color color, bool depthTest = true)
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
    public void DrawCube(ref Vector3 start, ref Vector3 end, ref Quaternion rotation, ref Color color, bool depthTest = true)
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
    public void DrawCapsule(ref Vector3 position, float height, float radius, ref Quaternion rotation, ref Color color, bool depthTest = true)
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
    public void DrawCylinder(ref Vector3 position, float height, float radius, ref Quaternion rotation, ref Color color, bool depthTest = true)
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
    public void DrawCone(ref Vector3 position, float height, float radius, ref Quaternion rotation, ref Color color, bool depthTest = true)
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
    public void DrawLine(ref Vector3 start, ref Vector3 end, ref Color color, bool depthTest = true)
    {
        var cmd = new Line { Start = start, End = end, Color = color };

        Push(new Renderable(in cmd), depthTest);
    }

    /// <summary>Queues one shape in the list its depth test puts it in, and counts it under its kind.</summary>
    private void Push(in Renderable renderable, bool depthTest)
    {
        ref var counts = ref depthTest ? ref totalPrimitives : ref totalPrimitivesNoDepth;

        (depthTest ? renderablesWithDepth : renderablesNoDepth).Add(renderable);
        counts.Increment(renderable.Type);
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
        renderer.EnsureCapacity(totalPrimitives.InstanceTotal + totalPrimitivesNoDepth.InstanceTotal, totalPrimitives.Lines * 2 + totalPrimitivesNoDepth.Lines * 2);

        // The depth-tested shapes first, then the rest, each kind in its own run; lines keep a buffer of their own
        var primitiveOffsets = totalPrimitives.Offsets(instanceOffset);
        var primitiveOffsetsNoDepth = totalPrimitivesNoDepth.Offsets(primitiveOffsets.Cones + totalPrimitives.Cones);

        primitiveOffsets.Lines = lineOffset;
        primitiveOffsetsNoDepth.Lines = totalPrimitives.Lines * 2 + lineOffset;

        instanceOffsets = primitiveOffsets;
        instanceOffsetsNoDepth = primitiveOffsetsNoDepth;

        renderer.ProcessRenderables(renderablesWithDepth, ref primitiveOffsets);
        renderer.ProcessRenderables(renderablesNoDepth, ref primitiveOffsetsNoDepth);

        primitivesToDraw = totalPrimitives;
        primitivesToDrawNoDepth = totalPrimitivesNoDepth;

        var next = (instanceOffsetsNoDepth.Cones + totalPrimitivesNoDepth.Cones, instanceOffsetsNoDepth.Lines + totalPrimitivesNoDepth.Lines * 2);

        // Clear per-frame message queues
        renderablesWithDepth.Clear();
        renderablesNoDepth.Clear();
        totalPrimitives.Clear();
        totalPrimitivesNoDepth.Clear();

        return next;
    }
}
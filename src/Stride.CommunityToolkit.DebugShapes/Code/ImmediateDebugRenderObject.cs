// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using static Stride.CommunityToolkit.DebugShapes.Code.ImmediateDebugRenderFeature;
using Capsule = Stride.CommunityToolkit.DebugShapes.Code.ImmediateDebugRenderFeature.Capsule;
using Cone = Stride.CommunityToolkit.DebugShapes.Code.ImmediateDebugRenderFeature.Cone;
using Cube = Stride.CommunityToolkit.DebugShapes.Code.ImmediateDebugRenderFeature.Cube;
using Cylinder = Stride.CommunityToolkit.DebugShapes.Code.ImmediateDebugRenderFeature.Cylinder;
using Sphere = Stride.CommunityToolkit.DebugShapes.Code.ImmediateDebugRenderFeature.Sphere;

namespace Stride.CommunityToolkit.DebugShapes.Code;

/// <summary>
/// Represents a render object for immediate debug rendering, allowing the addition of various shapes and lines.
/// </summary>
public class ImmediateDebugRenderObject : RenderObject
{
    /* messages */
    internal readonly List<Renderable> renderablesWithDepth = [];
    internal readonly List<Renderable> renderablesNoDepth = [];

    /* accumulators used when data is being pushed to the system */
    internal Primitives totalPrimitives, totalPrimitivesNoDepth;

    /* used to specify offset into instance data buffers when drawing */
    internal Primitives instanceOffsets, instanceOffsetsNoDepth;

    /* used in render stage to know how many of each instance to draw */
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
        var cmd = new Quad() { Position = position, Size = size, Rotation = rotation, Color = color };
        var msg = new Renderable(ref cmd);
        if (depthTest)
        {
            renderablesWithDepth.Add(msg);
            totalPrimitives.Quads++;
        }
        else
        {
            renderablesNoDepth.Add(msg);
            totalPrimitivesNoDepth.Quads++;
        }
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
        var cmd = new Circle() { Position = position, Radius = radius, Rotation = rotation, Color = color };
        var msg = new Renderable(ref cmd);
        if (depthTest)
        {
            renderablesWithDepth.Add(msg);
            totalPrimitives.Circles++;
        }
        else
        {
            renderablesNoDepth.Add(msg);
            totalPrimitivesNoDepth.Circles++;
        }
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
        var cmd = new Sphere() { Position = position, Radius = radius, Color = color };
        var msg = new Renderable(ref cmd);
        if (depthTest)
        {
            renderablesWithDepth.Add(msg);
            totalPrimitives.Spheres++;
        }
        else
        {
            renderablesNoDepth.Add(msg);
            totalPrimitivesNoDepth.Spheres++;
        }
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
        var cmd = new HalfSphere() { Position = position, Radius = radius, Rotation = rotation, Color = color };
        var msg = new Renderable(ref cmd);
        if (depthTest)
        {
            renderablesWithDepth.Add(msg);
            totalPrimitives.HalfSpheres++;
        }
        else
        {
            renderablesNoDepth.Add(msg);
            totalPrimitivesNoDepth.HalfSpheres++;
        }
    }

    /// <summary>
    /// Adds a cube to the debug render queue.
    /// </summary>
    /// <param name="start">The starting position of the cube.</param>
    /// <param name="end">The ending position of the cube.</param>
    /// <param name="rotation">The rotation of the cube.</param>
    /// <param name="color">The color of the cube.</param>
    /// <param name="depthTest">Whether to use depth testing for rendering.</param>
    public void DrawCube(ref Vector3 start, ref Vector3 end, ref Quaternion rotation, ref Color color, bool depthTest = true)
    {
        var cmd = new Cube() { Start = start, End = end, Rotation = rotation, Color = color };
        var msg = new Renderable(ref cmd);
        if (depthTest)
        {
            renderablesWithDepth.Add(msg);
            totalPrimitives.Cubes++;
        }
        else
        {
            renderablesNoDepth.Add(msg);
            totalPrimitivesNoDepth.Cubes++;
        }
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
        var cmd = new Capsule() { Position = position, Height = height, Radius = radius, Rotation = rotation, Color = color };
        var msg = new Renderable(ref cmd);
        if (depthTest)
        {
            renderablesWithDepth.Add(msg);
            totalPrimitives.Capsules++;
        }
        else
        {
            renderablesNoDepth.Add(msg);
            totalPrimitivesNoDepth.Capsules++;
        }
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
        var cmd = new Cylinder() { Position = position, Height = height, Radius = radius, Rotation = rotation, Color = color };
        var msg = new Renderable(ref cmd);
        if (depthTest)
        {
            renderablesWithDepth.Add(msg);
            totalPrimitives.Cylinders++;
        }
        else
        {
            renderablesNoDepth.Add(msg);
            totalPrimitivesNoDepth.Cylinders++;
        }
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
        var cmd = new Cone() { Position = position, Height = height, Radius = radius, Rotation = rotation, Color = color };
        var msg = new Renderable(ref cmd);
        if (depthTest)
        {
            renderablesWithDepth.Add(msg);
            totalPrimitives.Cones++;
        }
        else
        {
            renderablesNoDepth.Add(msg);
            totalPrimitivesNoDepth.Cones++;
        }
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
        var cmd = new Line() { Start = start, End = end, Color = color };
        var msg = new Renderable(ref cmd);
        if (depthTest)
        {
            renderablesWithDepth.Add(msg);
            totalPrimitives.Lines++;
        }
        else
        {
            renderablesNoDepth.Add(msg);
            totalPrimitivesNoDepth.Lines++;
        }
    }
}
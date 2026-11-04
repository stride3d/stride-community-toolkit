// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.DebugShapes;

/// <summary>
/// Provides a system for immediate debug rendering of various shapes and lines in the game world.
/// </summary>
public class ImmediateDebugRenderSystem : GameSystemBase
{
    internal enum DebugRenderableFlags : byte
    {
        Solid = (1 << 0),
        Wireframe = (1 << 1),
        DepthTest = (1 << 2)
    }

    private readonly List<DebugRenderable> _renderMessages = [];
    private readonly List<DebugRenderable> _renderMessagesWithLifetime = [];

    private ImmediateDebugRenderObject? _solidPrimitiveRenderer;
    private ImmediateDebugRenderObject? _wireframePrimitiveRenderer;
    private ImmediateDebugRenderObject? _transparentSolidPrimitiveRenderer;
    private ImmediateDebugRenderObject? _transparentWireframePrimitiveRenderer;

    /// <summary>
    /// Default color used when a specific color is not provided for a primitive.
    /// </summary>
    public Color PrimitiveColor { get; set; } = Color.LightGreen;

    /// <summary>
    /// Maximum number of one-frame primitives stored and rendered per update; past it, the oldest is dropped for each new one.
    /// </summary>
    public int MaxPrimitives { get; set; } = 100;

    /// <summary>
    /// Maximum number of lifetime-based primitives stored and rendered per update; past it, the oldest is dropped for each new one.
    /// </summary>
    public int MaxPrimitivesWithLifetime { get; set; } = 100;

    /// <summary>
    /// Render group used for the internal debug render objects.
    /// </summary>
    public RenderGroup RenderGroup { get; set; }

    /// <summary>
    /// Creates a new instance of the debug render system.
    /// </summary>
    /// <param name="registry">The service registry.</param>
    /// <param name="renderGroup">The render group to use for the debug render objects.</param>
    public ImmediateDebugRenderSystem(IServiceRegistry registry, RenderGroup renderGroup = RenderGroup.Group31) : base(registry)
    {
        Enabled = true;
        Visible = Platform.IsRunningDebugAssembly;

        RenderGroup = renderGroup;

        DrawOrder = 0xffffff;
        UpdateOrder = -100100; //before script
    }

    // A full queue drops its oldest shape for the new one, so what is drawn is always the latest asked for
    private void PushMessage(in DebugRenderable msg)
    {
        var timed = msg.Lifetime > 0f;
        var queue = timed ? _renderMessagesWithLifetime : _renderMessages;

        if (queue.Count >= (timed ? MaxPrimitivesWithLifetime : MaxPrimitives)) queue.RemoveAt(0);

        queue.Add(msg);
    }

    /// <summary>The colour asked for, or <see cref="PrimitiveColor"/> for the default.</summary>
    private Color Resolve(Color color) => color == default ? PrimitiveColor : color;

    /// <summary>The rotation asked for, or identity for the default.</summary>
    private static Quaternion Resolve(Quaternion rotation) => rotation == default ? Quaternion.Identity : rotation;

    /// <summary>The flags a shape carries: its depth test, and solid or wireframe.</summary>
    private static DebugRenderableFlags Flags(bool depthTest, bool solid)
        => (depthTest ? DebugRenderableFlags.DepthTest : 0) | (solid ? DebugRenderableFlags.Solid : DebugRenderableFlags.Wireframe);

    /// <summary>
    /// Draws a line between two points.
    /// </summary>
    /// <param name="start">Start position.</param>
    /// <param name="end">End position.</param>
    /// <param name="color">Line color. Uses <see cref="PrimitiveColor"/> when default.</param>
    /// <param name="duration">Duration in seconds the line remains visible. 0 draws for a single frame.</param>
    /// <param name="depthTest">Whether depth testing is enabled.</param>
    public void DrawLine(Vector3 start, Vector3 end, Color color = default, float duration = 0.0f, bool depthTest = true)
    {
        var cmd = new Line { Start = start, End = end, Color = Resolve(color) };

        PushMessage(new DebugRenderable(in cmd, depthTest ? DebugRenderableFlags.DepthTest : 0, duration));
    }

    /// <summary>
    /// Draws multiple independent line segments from a vertex list (pairs form segments).
    /// </summary>
    /// <param name="vertices">Array of vertices; each consecutive pair defines a line.</param>
    /// <param name="color">Color for all lines. Uses <see cref="PrimitiveColor"/> when null.</param>
    /// <param name="duration">Duration in seconds the lines remain visible. 0 draws for a single frame.</param>
    /// <param name="depthTest">Whether depth testing is enabled.</param>
    public void DrawLines(Vector3[] vertices, Color? color = null, float duration = 0.0f, bool depthTest = true)
    {
        for (var i = 0; i + 1 < vertices.Length; i += 2)
        {
            DrawLine(vertices[i], vertices[i + 1], color ?? PrimitiveColor, duration, depthTest);
        }
    }

    /// <summary>
    /// Draws a ray from a starting point along a direction.
    /// </summary>
    /// <param name="start">Start position.</param>
    /// <param name="dir">Direction vector.</param>
    /// <param name="color">Ray color. Uses <see cref="PrimitiveColor"/> when default.</param>
    /// <param name="duration">Duration in seconds the ray remains visible. 0 draws for a single frame.</param>
    /// <param name="depthTest">Whether depth testing is enabled.</param>
    public void DrawRay(Vector3 start, Vector3 dir, Color color = default, float duration = 0.0f, bool depthTest = true)
    {
        DrawLine(start, start + dir, Resolve(color), duration, depthTest);
    }

    /// <summary>
    /// Draws an arrow composed of a line and a cone tip.
    /// </summary>
    /// <param name="from">Arrow start position.</param>
    /// <param name="to">Arrow end position (tip base).</param>
    /// <param name="coneHeight">Height of the cone tip.</param>
    /// <param name="coneRadius">Radius of the cone tip.</param>
    /// <param name="color">Arrow color. Uses <see cref="PrimitiveColor"/> when default.</param>
    /// <param name="duration">Duration in seconds the arrow remains visible. 0 draws for a single frame.</param>
    /// <param name="depthTest">Whether depth testing is enabled.</param>
    /// <param name="solid">Whether the cone tip is rendered solid or wireframe.</param>
    public void DrawArrow(Vector3 from, Vector3 to, float coneHeight = 0.25f, float coneRadius = 0.125f, Color color = default, float duration = 0.0f, bool depthTest = true, bool solid = false)
    {
        DrawLine(from, to, color, duration, depthTest);
        DrawCone(to, coneHeight, coneRadius, Quaternion.BetweenDirections(Vector3.UnitY, to - from), Resolve(color), duration, depthTest, solid);
    }

    /// <summary>
    /// Draws a sphere.
    /// </summary>
    /// <param name="position">Sphere center position.</param>
    /// <param name="radius">Sphere radius.</param>
    /// <param name="color">Color. Uses <see cref="PrimitiveColor"/> when default.</param>
    /// <param name="duration">Duration in seconds the sphere remains visible. 0 draws for a single frame.</param>
    /// <param name="depthTest">Whether depth testing is enabled.</param>
    /// <param name="solid">Whether rendered solid or wireframe.</param>
    public void DrawSphere(Vector3 position, float radius, Color color = default, float duration = 0.0f, bool depthTest = true, bool solid = false)
    {
        var cmd = new Sphere { Position = position, Radius = radius, Color = Resolve(color) };

        PushMessage(new DebugRenderable(in cmd, Flags(depthTest, solid), duration));
    }

    /// <summary>
    /// Draws a half-sphere.
    /// </summary>
    /// <param name="position">Half-sphere center position.</param>
    /// <param name="radius">Half-sphere radius.</param>
    /// <param name="color">Color. Uses <see cref="PrimitiveColor"/> when default.</param>
    /// <param name="rotation">Rotation applied to the half-sphere.</param>
    /// <param name="duration">Duration in seconds the half-sphere remains visible. 0 draws for a single frame.</param>
    /// <param name="depthTest">Whether depth testing is enabled.</param>
    /// <param name="solid">Whether rendered solid or wireframe.</param>
    public void DrawHalfSphere(Vector3 position, float radius, Color color = default, Quaternion rotation = default, float duration = 0.0f, bool depthTest = true, bool solid = false)
    {
        var cmd = new HalfSphere { Position = position, Radius = radius, Rotation = Resolve(rotation), Color = Resolve(color) };

        PushMessage(new DebugRenderable(in cmd, Flags(depthTest, solid), duration));
    }

    /// <summary>
    /// Draws the box spanning two opposite corners, rotated about its centre.
    /// </summary>
    /// <param name="start">One corner of the box.</param>
    /// <param name="end">The opposite corner.</param>
    /// <param name="rotation">Rotation applied to the box.</param>
    /// <param name="color">Color. Uses <see cref="PrimitiveColor"/> when default.</param>
    /// <param name="duration">Duration in seconds the bounds remain visible. 0 draws for a single frame.</param>
    /// <param name="depthTest">Whether depth testing is enabled.</param>
    /// <param name="solid">Whether rendered solid or wireframe.</param>
    public void DrawBounds(Vector3 start, Vector3 end, Quaternion rotation = default, Color color = default, float duration = 0.0f, bool depthTest = true, bool solid = false)
    {
        var cmd = new Cube { Start = start + ((end - start) / 2), End = end + ((end - start) / 2), Rotation = Resolve(rotation), Color = Resolve(color) };

        PushMessage(new DebugRenderable(in cmd, Flags(depthTest, solid), duration));
    }

    /// <summary>
    /// Draws a cube in the scene with the specified position, size, rotation, color, and rendering options.
    /// </summary>
    /// <remarks>This method allows for flexible rendering of cubes in the scene, with options for wireframe
    /// or solid rendering, depth testing, and custom colors. Use this method to visualize objects, debug spatial
    /// layouts, or create temporary visual markers.</remarks>
    /// <param name="start">The starting position of the cube, representing its center.</param>
    /// <param name="size">The dimensions of the cube, specified as a <see cref="Vector3"/>.</param>
    /// <param name="rotation">The rotation of the cube, specified as a <see cref="Quaternion"/>. Defaults to no rotation.</param>
    /// <param name="color">The color of the cube, specified as a <see cref="Color"/>. Defaults to the primitive color.</param>
    /// <param name="duration">The duration, in seconds, for which the cube will remain visible. Defaults to 0.0, meaning the cube is rendered
    /// for a single frame.</param>
    /// <param name="depthTest">A value indicating whether the cube should be rendered with depth testing. Set to <see langword="true"/> to
    /// enable depth testing; otherwise, <see langword="false"/>. Defaults to <see langword="true"/>.</param>
    /// <param name="solid">A value indicating whether the cube should be rendered as a solid shape. Set to <see langword="true"/> for solid
    /// rendering; otherwise, <see langword="false"/> for wireframe rendering. Defaults to <see langword="false"/>.</param>
    public void DrawCube(Vector3 start, Vector3 size, Quaternion rotation = default, Color color = default, float duration = 0.0f, bool depthTest = true, bool solid = false)
    {
        var cmd = new Cube { Start = start, End = start + size, Rotation = Resolve(rotation), Color = Resolve(color) };

        PushMessage(new DebugRenderable(in cmd, Flags(depthTest, solid), duration));
    }

    /// <summary>
    /// Draws a capsule.
    /// </summary>
    /// <param name="position">Capsule center position.</param>
    /// <param name="height">The capsule's whole height, the two caps included, the way <c>DrawCylinder</c> takes its height.</param>
    /// <param name="radius">Capsule radius.</param>
    /// <param name="rotation">Rotation applied to the capsule.</param>
    /// <param name="color">Color. Uses <see cref="PrimitiveColor"/> when default.</param>
    /// <param name="duration">Duration in seconds the capsule remains visible. 0 draws for a single frame.</param>
    /// <param name="depthTest">Whether depth testing is enabled.</param>
    /// <param name="solid">Whether rendered solid or wireframe.</param>
    public void DrawCapsule(Vector3 position, float height, float radius, Quaternion rotation = default, Color color = default, float duration = 0.0f, bool depthTest = true, bool solid = false)
    {
        // The unit capsule mesh is two units tall, a length of one plus a cap of half a unit at each end, and is scaled by
        // Height, so the whole height asked for is halved to scale it
        var cmd = new Capsule { Position = position, Height = height / 2.0f, Radius = radius, Rotation = Resolve(rotation), Color = Resolve(color) };

        PushMessage(new DebugRenderable(in cmd, Flags(depthTest, solid), duration));
    }

    /// <summary>
    /// Draws a cylinder.
    /// </summary>
    /// <param name="position">Cylinder center position.</param>
    /// <param name="height">Cylinder height.</param>
    /// <param name="radius">Cylinder radius.</param>
    /// <param name="rotation">Rotation applied to the cylinder.</param>
    /// <param name="color">Color. Uses <see cref="PrimitiveColor"/> when default.</param>
    /// <param name="duration">Duration in seconds the cylinder remains visible. 0 draws for a single frame.</param>
    /// <param name="depthTest">Whether depth testing is enabled.</param>
    /// <param name="solid">Whether rendered solid or wireframe.</param>
    public void DrawCylinder(Vector3 position, float height, float radius, Quaternion rotation = default, Color color = default, float duration = 0.0f, bool depthTest = true, bool solid = false)
    {
        var cmd = new Cylinder { Position = position, Height = height, Radius = radius, Rotation = Resolve(rotation), Color = Resolve(color) };

        PushMessage(new DebugRenderable(in cmd, Flags(depthTest, solid), duration));
    }

    /// <summary>
    /// Draws a cone.
    /// </summary>
    /// <param name="position">Cone center position.</param>
    /// <param name="height">Cone height.</param>
    /// <param name="radius">Cone base radius.</param>
    /// <param name="rotation">Rotation applied to the cone.</param>
    /// <param name="color">Color. Uses <see cref="PrimitiveColor"/> when default.</param>
    /// <param name="duration">Duration in seconds the cone remains visible. 0 draws for a single frame.</param>
    /// <param name="depthTest">Whether depth testing is enabled.</param>
    /// <param name="solid">Whether rendered solid or wireframe.</param>
    public void DrawCone(Vector3 position, float height, float radius, Quaternion rotation = default, Color color = default, float duration = 0.0f, bool depthTest = true, bool solid = false)
    {
        var cmd = new Cone { Position = position, Height = height, Radius = radius, Rotation = Resolve(rotation), Color = Resolve(color) };

        PushMessage(new DebugRenderable(in cmd, Flags(depthTest, solid), duration));
    }

    /// <summary>
    /// Draws a quad.
    /// </summary>
    /// <param name="position">Quad center position.</param>
    /// <param name="size">Quad dimensions (X = width, Y = height).</param>
    /// <param name="rotation">Rotation applied to the quad.</param>
    /// <param name="color">Color. Uses <see cref="PrimitiveColor"/> when default.</param>
    /// <param name="duration">Duration in seconds the quad remains visible. 0 draws for a single frame.</param>
    /// <param name="depthTest">Whether depth testing is enabled.</param>
    /// <param name="solid">Whether rendered solid or wireframe.</param>
    public void DrawQuad(Vector3 position, Vector2 size, Quaternion rotation = default, Color color = default, float duration = 0.0f, bool depthTest = true, bool solid = false)
    {
        var cmd = new Quad { Position = position, Size = size, Rotation = Resolve(rotation), Color = Resolve(color) };

        PushMessage(new DebugRenderable(in cmd, Flags(depthTest, solid), duration));
    }

    /// <summary>
    /// Draws a circle.
    /// </summary>
    /// <param name="position">Circle center position.</param>
    /// <param name="radius">Circle radius.</param>
    /// <param name="rotation">Rotation applied to the circle plane.</param>
    /// <param name="color">Color. Uses <see cref="PrimitiveColor"/> when default.</param>
    /// <param name="duration">Duration in seconds the circle remains visible. 0 draws for a single frame.</param>
    /// <param name="depthTest">Whether depth testing is enabled.</param>
    /// <param name="solid">Whether rendered solid or wireframe.</param>
    public void DrawCircle(Vector3 position, float radius, Quaternion rotation = default, Color color = default, float duration = 0.0f, bool depthTest = true, bool solid = false)
    {
        var cmd = new Circle { Position = position, Radius = radius, Rotation = Resolve(rotation), Color = Resolve(color) };

        PushMessage(new DebugRenderable(in cmd, Flags(depthTest, solid), duration));
    }

    private bool CreateDebugRenderObjects()
    {
        // TODO: is this sane at all? it still seems a bit off.. what happens if the VisibilityGroups stuff gets changed/updated for instance?
        //  or will that never happen? ask xen2 about this and visibilitygroups again specifically.....
        var visibilityGroup = RenderContext.GetShared(Services)?.VisibilityGroup;

        if (visibilityGroup is null) return false;

        // One render object per pipeline configuration, so each draws its batch with a single state
        _solidPrimitiveRenderer = Add(FillMode.Solid, DebugRenderStage.Opaque);
        _wireframePrimitiveRenderer = Add(FillMode.Wireframe, DebugRenderStage.Opaque);
        _transparentSolidPrimitiveRenderer = Add(FillMode.Solid, DebugRenderStage.Transparent);
        _transparentWireframePrimitiveRenderer = Add(FillMode.Wireframe, DebugRenderStage.Transparent);

        return true;

        ImmediateDebugRenderObject Add(FillMode fillMode, DebugRenderStage stage)
        {
            var renderObject = new ImmediateDebugRenderObject { CurrentFillMode = fillMode, Stage = stage };

            visibilityGroup.RenderObjects.Add(renderObject);

            return renderObject;
        }
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (!Enabled || !Visible) return;

        if (_wireframePrimitiveRenderer is null && !CreateDebugRenderObjects()) return;

        // TODO: check if i'm doing this correctly..
        _solidPrimitiveRenderer!.RenderGroup = RenderGroup;
        _wireframePrimitiveRenderer!.RenderGroup = RenderGroup;
        _transparentSolidPrimitiveRenderer!.RenderGroup = RenderGroup;
        _transparentWireframePrimitiveRenderer!.RenderGroup = RenderGroup;

        HandlePrimitives(gameTime, _renderMessages);
        HandlePrimitives(gameTime, _renderMessagesWithLifetime);

        var delta = (float)gameTime.Elapsed.TotalSeconds;

        /* clear out any messages with no lifetime left */
        var lifetimeSpan = CollectionsMarshal.AsSpan(_renderMessagesWithLifetime);
        for (var i = 0; i < lifetimeSpan.Length; ++i)
        {
            lifetimeSpan[i].Lifetime -= delta;
        }

        _renderMessagesWithLifetime.RemoveAll((msg) => msg.Lifetime <= 0.0f);

        /* just clear our per-frame array */
        _renderMessages.Clear();
    }

    /// <summary>
    /// Hands one frame's queued shapes to the render objects: each message carries its own shape kind
    /// in a tagged union, so the loop reads the tag and forwards the matching payload.
    /// </summary>
    private void HandlePrimitives(GameTime gameTime, List<DebugRenderable> messages)
    {
        // Four render objects exist so that one pipeline state covers a whole batch: solid or wireframe,
        // opaque or transparent. A shape picks its own by its fill flag and its colour's alpha.
        ImmediateDebugRenderObject ChooseRenderer(DebugRenderableFlags flags, byte alpha)
        {
            var solid = (flags & DebugRenderableFlags.Solid) != 0;

            return alpha < 255
                ? (solid ? _transparentSolidPrimitiveRenderer! : _transparentWireframePrimitiveRenderer!)
                : (solid ? _solidPrimitiveRenderer! : _wireframePrimitiveRenderer!);
        }

        if (messages.Count == 0) return;

        // Walked as a span, by reference: a DebugRenderable is a wide union and copying one per shape
        // would dominate the cost of drawing it.
        var span = CollectionsMarshal.AsSpan(messages);
        for (var i = 0; i < span.Length; ++i)
        {
            ref var msg = ref span[i];
            var useDepthTest = (msg.Flags & DebugRenderableFlags.DepthTest) != 0;

            switch (msg.Type)
            {
                case DebugPrimitiveType.Quad:
                    ChooseRenderer(msg.Flags, msg.QuadData.Color.A).DrawQuad(in msg.QuadData.Position, in msg.QuadData.Size, in msg.QuadData.Rotation, in msg.QuadData.Color, depthTest: useDepthTest);
                    break;
                case DebugPrimitiveType.Circle:
                    ChooseRenderer(msg.Flags, msg.CircleData.Color.A).DrawCircle(in msg.CircleData.Position, msg.CircleData.Radius, in msg.CircleData.Rotation, in msg.CircleData.Color, depthTest: useDepthTest);
                    break;
                case DebugPrimitiveType.Line:
                    ChooseRenderer(msg.Flags, msg.LineData.Color.A).DrawLine(in msg.LineData.Start, in msg.LineData.End, in msg.LineData.Color, depthTest: useDepthTest);
                    break;
                case DebugPrimitiveType.Cube:
                    ChooseRenderer(msg.Flags, msg.CubeData.Color.A).DrawCube(in msg.CubeData.Start, in msg.CubeData.End, in msg.CubeData.Rotation, in msg.CubeData.Color, depthTest: useDepthTest);
                    break;
                case DebugPrimitiveType.Sphere:
                    ChooseRenderer(msg.Flags, msg.SphereData.Color.A).DrawSphere(in msg.SphereData.Position, msg.SphereData.Radius, in msg.SphereData.Color, depthTest: useDepthTest);
                    break;
                case DebugPrimitiveType.HalfSphere:
                    ChooseRenderer(msg.Flags, msg.HalfSphereData.Color.A).DrawHalfSphere(in msg.HalfSphereData.Position, msg.HalfSphereData.Radius, in msg.HalfSphereData.Rotation, in msg.HalfSphereData.Color, depthTest: useDepthTest);
                    break;
                case DebugPrimitiveType.Capsule:
                    ChooseRenderer(msg.Flags, msg.CapsuleData.Color.A).DrawCapsule(in msg.CapsuleData.Position, msg.CapsuleData.Height, msg.CapsuleData.Radius, in msg.CapsuleData.Rotation, in msg.CapsuleData.Color, depthTest: useDepthTest);
                    break;
                case DebugPrimitiveType.Cylinder:
                    ChooseRenderer(msg.Flags, msg.CylinderData.Color.A).DrawCylinder(in msg.CylinderData.Position, msg.CylinderData.Height, msg.CylinderData.Radius, in msg.CylinderData.Rotation, in msg.CylinderData.Color, depthTest: useDepthTest);
                    break;
                case DebugPrimitiveType.Cone:
                    ChooseRenderer(msg.Flags, msg.ConeData.Color.A).DrawCone(in msg.ConeData.Position, msg.ConeData.Height, msg.ConeData.Radius, in msg.ConeData.Rotation, in msg.ConeData.Color, depthTest: useDepthTest);
                    break;
            }
        }
    }
}
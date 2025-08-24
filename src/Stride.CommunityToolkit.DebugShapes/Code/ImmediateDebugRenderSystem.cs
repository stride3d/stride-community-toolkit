// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.DebugShapes.Code;

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
    /// Maximum number of one-frame primitives stored and rendered per update.
    /// </summary>
    public int MaxPrimitives { get; set; } = 100;

    /// <summary>
    /// Maximum number of lifetime-based primitives stored and rendered per update.
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

    private void PushMessage(ref DebugRenderable msg)
    {
        if (msg.Lifetime > 0.0f)
        {
            _renderMessagesWithLifetime.Add(msg);
            // drop one old message if the tail size has been reached
            if (_renderMessagesWithLifetime.Count > MaxPrimitivesWithLifetime)
            {
                _renderMessagesWithLifetime.RemoveAt(_renderMessagesWithLifetime.Count - 1);
            }
        }
        else
        {
            _renderMessages.Add(msg);
            // drop one old message if the tail size has been reached
            if (_renderMessages.Count > MaxPrimitives)
            {
                _renderMessages.RemoveAt(_renderMessages.Count - 1);
            }
        }
    }

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
        var cmd = new Line { Start = start, End = end, Color = color == default ? PrimitiveColor : color };
        var msg = new DebugRenderable(ref cmd, depthTest ? DebugRenderableFlags.DepthTest : 0) { Lifetime = duration };
        PushMessage(ref msg);
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
        var totalVertexPairs = vertices.Length - (vertices.Length % 2);
        for (int i = 0; i < totalVertexPairs; i += 2)
        {
            ref var v1 = ref vertices[i];
            ref var v2 = ref vertices[i + 1];
            DrawLine(v1, v2, color ?? PrimitiveColor, duration, depthTest);
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
        DrawLine(start, start + dir, color == default ? PrimitiveColor : color, duration, depthTest);
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
        DrawCone(to, coneHeight, coneRadius, Quaternion.BetweenDirections(new Vector3(0.0f, 1.0f, 0.0f), to - from), color == default ? PrimitiveColor : color, duration, depthTest, solid);
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
        var cmd = new Sphere { Position = position, Radius = radius, Color = color == default ? PrimitiveColor : color };
        var renderFlags = (depthTest ? DebugRenderableFlags.DepthTest : 0) | (solid ? DebugRenderableFlags.Solid : DebugRenderableFlags.Wireframe);
        var msg = new DebugRenderable(ref cmd, renderFlags) { Lifetime = duration };
        PushMessage(ref msg);
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
        var cmd = new HalfSphere { Position = position, Radius = radius, Rotation = rotation == default ? Quaternion.Identity : rotation, Color = color == default ? PrimitiveColor : color };
        var renderFlags = (depthTest ? DebugRenderableFlags.DepthTest : 0) | (solid ? DebugRenderableFlags.Solid : DebugRenderableFlags.Wireframe);
        var msg = new DebugRenderable(ref cmd, renderFlags) { Lifetime = duration };
        PushMessage(ref msg);
    }

    /// <summary>
    /// Draws axis-aligned bounds represented by a cube.
    /// </summary>
    /// <param name="start">Start position of the bounding box.</param>
    /// <param name="end">End position of the bounding box.</param>
    /// <param name="rotation">Rotation applied to the box.</param>
    /// <param name="color">Color. Uses <see cref="PrimitiveColor"/> when default.</param>
    /// <param name="duration">Duration in seconds the bounds remain visible. 0 draws for a single frame.</param>
    /// <param name="depthTest">Whether depth testing is enabled.</param>
    /// <param name="solid">Whether rendered solid or wireframe.</param>
    public void DrawBounds(Vector3 start, Vector3 end, Quaternion rotation = default, Color color = default, float duration = 0.0f, bool depthTest = true, bool solid = false)
    {
        var cmd = new Cube { Start = start + ((end - start) / 2), End = end + ((end - start) / 2), Rotation = rotation == default ? Quaternion.Identity : rotation, Color = color == default ? PrimitiveColor : color };
        var renderFlags = (depthTest ? DebugRenderableFlags.DepthTest : 0) | (solid ? DebugRenderableFlags.Solid : DebugRenderableFlags.Wireframe);
        var msg = new DebugRenderable(ref cmd, renderFlags) { Lifetime = duration };
        PushMessage(ref msg);
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
        var cmd = new Cube { Start = start, End = start + size, Rotation = rotation == default ? Quaternion.Identity : rotation, Color = color == default ? PrimitiveColor : color };
        var renderFlags = (depthTest ? DebugRenderableFlags.DepthTest : 0) | (solid ? DebugRenderableFlags.Solid : DebugRenderableFlags.Wireframe);
        var msg = new DebugRenderable(ref cmd, renderFlags) { Lifetime = duration };

        PushMessage(ref msg);
    }

    /// <summary>
    /// Draws a capsule.
    /// </summary>
    /// <param name="position">Capsule center position.</param>
    /// <param name="height">Capsule height.</param>
    /// <param name="radius">Capsule radius.</param>
    /// <param name="rotation">Rotation applied to the capsule.</param>
    /// <param name="color">Color. Uses <see cref="PrimitiveColor"/> when default.</param>
    /// <param name="duration">Duration in seconds the capsule remains visible. 0 draws for a single frame.</param>
    /// <param name="depthTest">Whether depth testing is enabled.</param>
    /// <param name="solid">Whether rendered solid or wireframe.</param>
    public void DrawCapsule(Vector3 position, float height, float radius, Quaternion rotation = default, Color color = default, float duration = 0.0f, bool depthTest = true, bool solid = false)
    {
        // FIXME: height is divided by two here but can probably be solved more elegantly elsewhere yes
        var cmd = new Capsule { Position = position, Height = height / 2.0f, Radius = radius, Rotation = rotation == default ? Quaternion.Identity : rotation, Color = color == default ? PrimitiveColor : color };
        var renderFlags = (depthTest ? DebugRenderableFlags.DepthTest : 0) | (solid ? DebugRenderableFlags.Solid : DebugRenderableFlags.Wireframe);
        var msg = new DebugRenderable(ref cmd, renderFlags) { Lifetime = duration };
        PushMessage(ref msg);
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
        var cmd = new Cylinder { Position = position, Height = height, Radius = radius, Rotation = rotation == default ? Quaternion.Identity : rotation, Color = color == default ? PrimitiveColor : color };
        var renderFlags = (depthTest ? DebugRenderableFlags.DepthTest : 0) | (solid ? DebugRenderableFlags.Solid : DebugRenderableFlags.Wireframe);
        var msg = new DebugRenderable(ref cmd, renderFlags) { Lifetime = duration };
        PushMessage(ref msg);
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
        var cmd = new Cone { Position = position, Height = height, Radius = radius, Rotation = rotation == default ? Quaternion.Identity : rotation, Color = color == default ? PrimitiveColor : color };
        var renderFlags = (depthTest ? DebugRenderableFlags.DepthTest : 0) | (solid ? DebugRenderableFlags.Solid : DebugRenderableFlags.Wireframe);
        var msg = new DebugRenderable(ref cmd, renderFlags) { Lifetime = duration };
        PushMessage(ref msg);
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
        var cmd = new Quad { Position = position, Size = size, Rotation = rotation == default ? Quaternion.Identity : rotation, Color = color == default ? PrimitiveColor : color };
        var renderFlags = (depthTest ? DebugRenderableFlags.DepthTest : 0) | (solid ? DebugRenderableFlags.Solid : DebugRenderableFlags.Wireframe);
        var msg = new DebugRenderable(ref cmd, renderFlags) { Lifetime = duration };
        PushMessage(ref msg);
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
        var cmd = new Circle { Position = position, Radius = radius, Rotation = rotation == default ? Quaternion.Identity : rotation, Color = color == default ? PrimitiveColor : color };
        var renderFlags = (depthTest ? DebugRenderableFlags.DepthTest : 0) | (solid ? DebugRenderableFlags.Solid : DebugRenderableFlags.Wireframe);
        var msg = new DebugRenderable(ref cmd, renderFlags) { Lifetime = duration };
        PushMessage(ref msg);
    }

    private bool CreateDebugRenderObjects()
    {
        // TODO: is this sane at all? it still seems a bit off.. what happens if the VisibilityGroups stuff gets changed/updated for instance?
        //  or will that never happen? ask xen2 about this and visibilitygroups again specifically.....
        var renderContext = RenderContext.GetShared(Services);
        if (renderContext == null)
            return false;

        var visibilityGroup = renderContext.VisibilityGroup;
        if (visibilityGroup == null)
            return false;

        var newSolidRenderObject = new ImmediateDebugRenderObject
        {
            CurrentFillMode = FillMode.Solid,
            Stage = DebugRenderStage.Opaque
        };
        visibilityGroup.RenderObjects.Add(newSolidRenderObject);
        _solidPrimitiveRenderer = newSolidRenderObject;

        var newWireframeRenderObject = new ImmediateDebugRenderObject
        {
            CurrentFillMode = FillMode.Wireframe,
            Stage = DebugRenderStage.Opaque
        };
        visibilityGroup.RenderObjects.Add(newWireframeRenderObject);
        _wireframePrimitiveRenderer = newWireframeRenderObject;

        var newTransparentSolidRenderObject = new ImmediateDebugRenderObject
        {
            CurrentFillMode = FillMode.Solid,
            Stage = DebugRenderStage.Transparent
        };
        visibilityGroup.RenderObjects.Add(newTransparentSolidRenderObject);
        _transparentSolidPrimitiveRenderer = newTransparentSolidRenderObject;

        var newTransparentWireframeRenderObject = new ImmediateDebugRenderObject
        {
            CurrentFillMode = FillMode.Wireframe,
            Stage = DebugRenderStage.Transparent
        };
        visibilityGroup.RenderObjects.Add(newTransparentWireframeRenderObject);
        _transparentWireframePrimitiveRenderer = newTransparentWireframeRenderObject;

        return true;
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (!Enabled || !Visible) return;

        if (_wireframePrimitiveRenderer is null)
        {
            bool created = CreateDebugRenderObjects();

            if (!created) return;
        }

        // TODO: check if i'm doing this correctly..
        _solidPrimitiveRenderer!.RenderGroup = RenderGroup;
        _wireframePrimitiveRenderer!.RenderGroup = RenderGroup;
        _transparentSolidPrimitiveRenderer!.RenderGroup = RenderGroup;
        _transparentWireframePrimitiveRenderer!.RenderGroup = RenderGroup;

        HandlePrimitives(gameTime, _renderMessages);
        HandlePrimitives(gameTime, _renderMessagesWithLifetime);

        float delta = (float)gameTime.Elapsed.TotalSeconds;

        /* clear out any messages with no lifetime left */
        var lifetimeSpan = CollectionsMarshal.AsSpan(_renderMessagesWithLifetime);
        for (int i = 0; i < lifetimeSpan.Length; ++i)
        {
            lifetimeSpan[i].Lifetime -= delta;
        }

        _renderMessagesWithLifetime.RemoveAll((msg) => msg.Lifetime <= 0.0f);

        /* just clear our per-frame array */
        _renderMessages.Clear();
    }

    private void HandlePrimitives(GameTime gameTime, List<DebugRenderable> messages)
    {
        ImmediateDebugRenderObject ChooseRenderer(DebugRenderableFlags flags, byte alpha)
        {
            if (alpha < 255)
            {
                return ((flags & DebugRenderableFlags.Solid) != 0) ? _transparentSolidPrimitiveRenderer! : _transparentWireframePrimitiveRenderer!;
            }
            else
            {
                return ((flags & DebugRenderableFlags.Solid) != 0) ? _solidPrimitiveRenderer! : _wireframePrimitiveRenderer!;
            }
        }

        if (messages.Count == 0) return;

        var span = CollectionsMarshal.AsSpan(messages);
        for (int i = 0; i < span.Length; ++i)
        {
            ref var msg = ref span[i];
            var useDepthTest = (msg.Flags & DebugRenderableFlags.DepthTest) != 0;

            switch (msg.Type)
            {
                case DebugPrimitiveType.Quad:
                    ChooseRenderer(msg.Flags, msg.QuadData.Color.A).DrawQuad(ref msg.QuadData.Position, ref msg.QuadData.Size, ref msg.QuadData.Rotation, ref msg.QuadData.Color, depthTest: useDepthTest);
                    break;
                case DebugPrimitiveType.Circle:
                    ChooseRenderer(msg.Flags, msg.CircleData.Color.A).DrawCircle(ref msg.CircleData.Position, msg.CircleData.Radius, ref msg.CircleData.Rotation, ref msg.CircleData.Color, depthTest: useDepthTest);
                    break;
                case DebugPrimitiveType.Line:
                    ChooseRenderer(msg.Flags, msg.LineData.Color.A).DrawLine(ref msg.LineData.Start, ref msg.LineData.End, ref msg.LineData.Color, depthTest: useDepthTest);
                    break;
                case DebugPrimitiveType.Cube:
                    ChooseRenderer(msg.Flags, msg.CubeData.Color.A).DrawCube(ref msg.CubeData.Start, ref msg.CubeData.End, ref msg.CubeData.Rotation, ref msg.CubeData.Color, depthTest: useDepthTest);
                    break;
                case DebugPrimitiveType.Sphere:
                    ChooseRenderer(msg.Flags, msg.SphereData.Color.A).DrawSphere(ref msg.SphereData.Position, msg.SphereData.Radius, ref msg.SphereData.Color, depthTest: useDepthTest);
                    break;
                case DebugPrimitiveType.HalfSphere:
                    ChooseRenderer(msg.Flags, msg.HalfSphereData.Color.A).DrawHalfSphere(ref msg.HalfSphereData.Position, msg.HalfSphereData.Radius, ref msg.HalfSphereData.Rotation, ref msg.HalfSphereData.Color, depthTest: useDepthTest);
                    break;
                case DebugPrimitiveType.Capsule:
                    ChooseRenderer(msg.Flags, msg.CapsuleData.Color.A).DrawCapsule(ref msg.CapsuleData.Position, msg.CapsuleData.Height, msg.CapsuleData.Radius, ref msg.CapsuleData.Rotation, ref msg.CapsuleData.Color, depthTest: useDepthTest);
                    break;
                case DebugPrimitiveType.Cylinder:
                    ChooseRenderer(msg.Flags, msg.CylinderData.Color.A).DrawCylinder(ref msg.CylinderData.Position, msg.CylinderData.Height, msg.CylinderData.Radius, ref msg.CylinderData.Rotation, ref msg.CylinderData.Color, depthTest: useDepthTest);
                    break;
                case DebugPrimitiveType.Cone:
                    ChooseRenderer(msg.Flags, msg.ConeData.Color.A).DrawCone(ref msg.ConeData.Position, msg.ConeData.Height, msg.ConeData.Radius, ref msg.ConeData.Rotation, ref msg.ConeData.Color, depthTest: useDepthTest);
                    break;
            }
        }
    }
}
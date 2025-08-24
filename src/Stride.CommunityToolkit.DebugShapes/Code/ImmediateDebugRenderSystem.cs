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

    [StructLayout(LayoutKind.Explicit)]
    internal struct DebugRenderable
    {
        public DebugRenderable(ref Quad q, DebugRenderableFlags renderFlags) : this()
        {
            Type = DebugPrimitiveType.Quad;
            Flags = renderFlags;
            QuadData = q;
        }

        public DebugRenderable(ref Circle c, DebugRenderableFlags renderFlags) : this()
        {
            Type = DebugPrimitiveType.Circle;
            Flags = renderFlags;
            CircleData = c;
        }

        public DebugRenderable(ref Line l, DebugRenderableFlags renderFlags) : this()
        {
            Type = DebugPrimitiveType.Line;
            Flags = renderFlags;
            LineData = l;
        }

        public DebugRenderable(ref Cube b, DebugRenderableFlags renderFlags) : this()
        {
            Type = DebugPrimitiveType.Cube;
            Flags = renderFlags;
            CubeData = b;
        }

        public DebugRenderable(ref Sphere s, DebugRenderableFlags renderFlags) : this()
        {
            Type = DebugPrimitiveType.Sphere;
            Flags = renderFlags;
            SphereData = s;
        }

        public DebugRenderable(ref HalfSphere h, DebugRenderableFlags renderFlags) : this()
        {
            Type = DebugPrimitiveType.HalfSphere;
            Flags = renderFlags;
            HalfSphereData = h;
        }

        public DebugRenderable(ref Capsule c, DebugRenderableFlags renderFlags) : this()
        {
            Type = DebugPrimitiveType.Capsule;
            Flags = renderFlags;
            CapsuleData = c;
        }

        public DebugRenderable(ref Cylinder c, DebugRenderableFlags renderFlags) : this()
        {
            Type = DebugPrimitiveType.Cylinder;
            Flags = renderFlags;
            CylinderData = c;
        }

        public DebugRenderable(ref Cone c, DebugRenderableFlags renderFlags) : this()
        {
            Type = DebugPrimitiveType.Cone;
            Flags = renderFlags;
            ConeData = c;
        }

        [FieldOffset(0)]
        public DebugPrimitiveType Type;

        [FieldOffset(sizeof(byte))]
        public DebugRenderableFlags Flags;

        [FieldOffset(sizeof(byte) * 2)]
        public float Lifetime;

        [FieldOffset((sizeof(byte) * 2) + sizeof(float))]
        public Quad QuadData;

        [FieldOffset((sizeof(byte) * 2) + sizeof(float))]
        public Circle CircleData;

        [FieldOffset((sizeof(byte) * 2) + sizeof(float))]
        public Line LineData;

        [FieldOffset((sizeof(byte) * 2) + sizeof(float))]
        public Cube CubeData;

        [FieldOffset((sizeof(byte) * 2) + sizeof(float))]
        public Sphere SphereData;

        [FieldOffset((sizeof(byte) * 2) + sizeof(float))]
        public HalfSphere HalfSphereData;

        [FieldOffset((sizeof(byte) * 2) + sizeof(float))]
        public Capsule CapsuleData;

        [FieldOffset((sizeof(byte) * 2) + sizeof(float))]
        public Cylinder CylinderData;

        [FieldOffset((sizeof(byte) * 2) + sizeof(float))]
        public Cone ConeData;
    }

    private readonly List<DebugRenderable> renderMessages = new List<DebugRenderable>();
    private readonly List<DebugRenderable> renderMessagesWithLifetime = new List<DebugRenderable>();

    private ImmediateDebugRenderObject solidPrimitiveRenderer;
    private ImmediateDebugRenderObject wireframePrimitiveRenderer;
    private ImmediateDebugRenderObject transparentSolidPrimitiveRenderer;
    private ImmediateDebugRenderObject transparentWireframePrimitiveRenderer;

    public Color PrimitiveColor { get; set; } = Color.LightGreen;

    public int MaxPrimitives { get; set; } = 100;
    public int MaxPrimitivesWithLifetime { get; set; } = 100;

    public RenderGroup RenderGroup { get; set; }

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
            renderMessagesWithLifetime.Add(msg);
            // drop one old message if the tail size has been reached
            if (renderMessagesWithLifetime.Count > MaxPrimitivesWithLifetime)
            {
                renderMessagesWithLifetime.RemoveAt(renderMessagesWithLifetime.Count - 1);
            }
        }
        else
        {
            renderMessages.Add(msg);
            // drop one old message if the tail size has been reached
            if (renderMessages.Count > MaxPrimitives)
            {
                renderMessages.RemoveAt(renderMessages.Count - 1);
            }
        }
    }

    public void DrawLine(Vector3 start, Vector3 end, Color color = default, float duration = 0.0f, bool depthTest = true)
    {
        var cmd = new Line { Start = start, End = end, Color = color == default ? PrimitiveColor : color };
        var msg = new DebugRenderable(ref cmd, depthTest ? DebugRenderableFlags.DepthTest : 0) { Lifetime = duration };
        PushMessage(ref msg);
    }

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

    public void DrawRay(Vector3 start, Vector3 dir, Color color = default, float duration = 0.0f, bool depthTest = true)
    {
        DrawLine(start, start + dir, color == default ? PrimitiveColor : color, duration, depthTest);
    }

    public void DrawArrow(Vector3 from, Vector3 to, float coneHeight = 0.25f, float coneRadius = 0.125f, Color color = default, float duration = 0.0f, bool depthTest = true, bool solid = false)
    {
        DrawLine(from, to, color, duration, depthTest);
        DrawCone(to, coneHeight, coneRadius, Quaternion.BetweenDirections(new Vector3(0.0f, 1.0f, 0.0f), to - from), color == default ? PrimitiveColor : color, duration, depthTest, solid);
    }

    public void DrawSphere(Vector3 position, float radius, Color color = default, float duration = 0.0f, bool depthTest = true, bool solid = false)
    {
        var cmd = new Sphere { Position = position, Radius = radius, Color = color == default ? PrimitiveColor : color };
        var renderFlags = (depthTest ? DebugRenderableFlags.DepthTest : 0) | (solid ? DebugRenderableFlags.Solid : DebugRenderableFlags.Wireframe);
        var msg = new DebugRenderable(ref cmd, renderFlags) { Lifetime = duration };
        PushMessage(ref msg);
    }

    public void DrawHalfSphere(Vector3 position, float radius, Color color = default, Quaternion rotation = default, float duration = 0.0f, bool depthTest = true, bool solid = false)
    {
        var cmd = new HalfSphere { Position = position, Radius = radius, Rotation = rotation == default ? Quaternion.Identity : rotation, Color = color == default ? PrimitiveColor : color };
        var renderFlags = (depthTest ? DebugRenderableFlags.DepthTest : 0) | (solid ? DebugRenderableFlags.Solid : DebugRenderableFlags.Wireframe);
        var msg = new DebugRenderable(ref cmd, renderFlags) { Lifetime = duration };
        PushMessage(ref msg);
    }

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

    public void DrawCapsule(Vector3 position, float height, float radius, Quaternion rotation = default, Color color = default, float duration = 0.0f, bool depthTest = true, bool solid = false)
    {
        // FIXME: height is divided by two here but can probably be solved more elegantly elsewhere yes
        var cmd = new Capsule { Position = position, Height = height / 2.0f, Radius = radius, Rotation = rotation == default ? Quaternion.Identity : rotation, Color = color == default ? PrimitiveColor : color };
        var renderFlags = (depthTest ? DebugRenderableFlags.DepthTest : 0) | (solid ? DebugRenderableFlags.Solid : DebugRenderableFlags.Wireframe);
        var msg = new DebugRenderable(ref cmd, renderFlags) { Lifetime = duration };
        PushMessage(ref msg);
    }

    public void DrawCylinder(Vector3 position, float height, float radius, Quaternion rotation = default, Color color = default, float duration = 0.0f, bool depthTest = true, bool solid = false)
    {
        var cmd = new Cylinder { Position = position, Height = height, Radius = radius, Rotation = rotation == default ? Quaternion.Identity : rotation, Color = color == default ? PrimitiveColor : color };
        var renderFlags = (depthTest ? DebugRenderableFlags.DepthTest : 0) | (solid ? DebugRenderableFlags.Solid : DebugRenderableFlags.Wireframe);
        var msg = new DebugRenderable(ref cmd, renderFlags) { Lifetime = duration };
        PushMessage(ref msg);
    }

    public void DrawCone(Vector3 position, float height, float radius, Quaternion rotation = default, Color color = default, float duration = 0.0f, bool depthTest = true, bool solid = false)
    {
        var cmd = new Cone { Position = position, Height = height, Radius = radius, Rotation = rotation == default ? Quaternion.Identity : rotation, Color = color == default ? PrimitiveColor : color };
        var renderFlags = (depthTest ? DebugRenderableFlags.DepthTest : 0) | (solid ? DebugRenderableFlags.Solid : DebugRenderableFlags.Wireframe);
        var msg = new DebugRenderable(ref cmd, renderFlags) { Lifetime = duration };
        PushMessage(ref msg);
    }

    public void DrawQuad(Vector3 position, Vector2 size, Quaternion rotation = default, Color color = default, float duration = 0.0f, bool depthTest = true, bool solid = false)
    {
        var cmd = new Quad { Position = position, Size = size, Rotation = rotation == default ? Quaternion.Identity : rotation, Color = color == default ? PrimitiveColor : color };
        var renderFlags = (depthTest ? DebugRenderableFlags.DepthTest : 0) | (solid ? DebugRenderableFlags.Solid : DebugRenderableFlags.Wireframe);
        var msg = new DebugRenderable(ref cmd, renderFlags) { Lifetime = duration };
        PushMessage(ref msg);
    }

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
        solidPrimitiveRenderer = newSolidRenderObject;

        var newWireframeRenderObject = new ImmediateDebugRenderObject
        {
            CurrentFillMode = FillMode.Wireframe,
            Stage = DebugRenderStage.Opaque
        };
        visibilityGroup.RenderObjects.Add(newWireframeRenderObject);
        wireframePrimitiveRenderer = newWireframeRenderObject;

        var newTransparentSolidRenderObject = new ImmediateDebugRenderObject
        {
            CurrentFillMode = FillMode.Solid,
            Stage = DebugRenderStage.Transparent
        };
        visibilityGroup.RenderObjects.Add(newTransparentSolidRenderObject);
        transparentSolidPrimitiveRenderer = newTransparentSolidRenderObject;

        var newTransparentWireframeRenderObject = new ImmediateDebugRenderObject
        {
            CurrentFillMode = FillMode.Wireframe,
            Stage = DebugRenderStage.Transparent
        };
        visibilityGroup.RenderObjects.Add(newTransparentWireframeRenderObject);
        transparentWireframePrimitiveRenderer = newTransparentWireframeRenderObject;

        return true;
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (!Enabled || !Visible) return;

        if (wireframePrimitiveRenderer == null)
        {
            bool created = CreateDebugRenderObjects();

            if (!created) return;
        }

        // TODO: check if i'm doing this correctly..
        solidPrimitiveRenderer.RenderGroup = RenderGroup;
        wireframePrimitiveRenderer.RenderGroup = RenderGroup;
        transparentSolidPrimitiveRenderer.RenderGroup = RenderGroup;
        transparentWireframePrimitiveRenderer.RenderGroup = RenderGroup;

        HandlePrimitives(gameTime, renderMessages);
        HandlePrimitives(gameTime, renderMessagesWithLifetime);

        float delta = (float)gameTime.Elapsed.TotalSeconds;

        /* clear out any messages with no lifetime left */
        var lifetimeSpan = CollectionsMarshal.AsSpan(renderMessagesWithLifetime);
        for (int i = 0; i < lifetimeSpan.Length; ++i)
        {
            lifetimeSpan[i].Lifetime -= delta;
        }

        renderMessagesWithLifetime.RemoveAll((msg) => msg.Lifetime <= 0.0f);

        /* just clear our per-frame array */
        renderMessages.Clear();
    }

    private void HandlePrimitives(GameTime gameTime, List<DebugRenderable> messages)
    {
        ImmediateDebugRenderObject ChooseRenderer(DebugRenderableFlags flags, byte alpha)
        {
            if (alpha < 255)
            {
                return ((flags & DebugRenderableFlags.Solid) != 0) ? transparentSolidPrimitiveRenderer : transparentWireframePrimitiveRenderer;
            }
            else
            {
                return ((flags & DebugRenderableFlags.Solid) != 0) ? solidPrimitiveRenderer : wireframePrimitiveRenderer;
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
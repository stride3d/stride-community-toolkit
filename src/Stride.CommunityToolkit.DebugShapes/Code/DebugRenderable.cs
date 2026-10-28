// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using static Stride.CommunityToolkit.DebugShapes.ImmediateDebugRenderSystem;

namespace Stride.CommunityToolkit.DebugShapes;

/// <summary>
/// One queued debug shape: a tagged union of every payload, with the flags and lifetime it was asked
/// for. The payload shares one offset, so the struct is as wide as the widest shape and no wider.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal struct DebugRenderable
{
    // The payload follows the type, the flags and the lifetime
    private const int PayloadOffset = sizeof(byte) * 2 + sizeof(float);

    private DebugRenderable(DebugPrimitiveType type, DebugRenderableFlags flags, float lifetime)
    {
        Type = type;
        Flags = flags;
        Lifetime = lifetime;
    }

    internal DebugRenderable(in Quad q, DebugRenderableFlags renderFlags, float lifetime = 0f) : this(DebugPrimitiveType.Quad, renderFlags, lifetime) => QuadData = q;

    internal DebugRenderable(in Circle c, DebugRenderableFlags renderFlags, float lifetime = 0f) : this(DebugPrimitiveType.Circle, renderFlags, lifetime) => CircleData = c;

    internal DebugRenderable(in Line l, DebugRenderableFlags renderFlags, float lifetime = 0f) : this(DebugPrimitiveType.Line, renderFlags, lifetime) => LineData = l;

    internal DebugRenderable(in Cube b, DebugRenderableFlags renderFlags, float lifetime = 0f) : this(DebugPrimitiveType.Cube, renderFlags, lifetime) => CubeData = b;

    internal DebugRenderable(in Sphere s, DebugRenderableFlags renderFlags, float lifetime = 0f) : this(DebugPrimitiveType.Sphere, renderFlags, lifetime) => SphereData = s;

    internal DebugRenderable(in HalfSphere h, DebugRenderableFlags renderFlags, float lifetime = 0f) : this(DebugPrimitiveType.HalfSphere, renderFlags, lifetime) => HalfSphereData = h;

    internal DebugRenderable(in Capsule c, DebugRenderableFlags renderFlags, float lifetime = 0f) : this(DebugPrimitiveType.Capsule, renderFlags, lifetime) => CapsuleData = c;

    internal DebugRenderable(in Cylinder c, DebugRenderableFlags renderFlags, float lifetime = 0f) : this(DebugPrimitiveType.Cylinder, renderFlags, lifetime) => CylinderData = c;

    internal DebugRenderable(in Cone c, DebugRenderableFlags renderFlags, float lifetime = 0f) : this(DebugPrimitiveType.Cone, renderFlags, lifetime) => ConeData = c;

    [FieldOffset(0)]
    public DebugPrimitiveType Type;

    [FieldOffset(sizeof(byte))]
    public DebugRenderableFlags Flags;

    [FieldOffset(sizeof(byte) * 2)]
    public float Lifetime;

    [FieldOffset(PayloadOffset)]
    public Quad QuadData;

    [FieldOffset(PayloadOffset)]
    public Circle CircleData;

    [FieldOffset(PayloadOffset)]
    public Line LineData;

    [FieldOffset(PayloadOffset)]
    public Cube CubeData;

    [FieldOffset(PayloadOffset)]
    public Sphere SphereData;

    [FieldOffset(PayloadOffset)]
    public HalfSphere HalfSphereData;

    [FieldOffset(PayloadOffset)]
    public Capsule CapsuleData;

    [FieldOffset(PayloadOffset)]
    public Cylinder CylinderData;

    [FieldOffset(PayloadOffset)]
    public Cone ConeData;
}
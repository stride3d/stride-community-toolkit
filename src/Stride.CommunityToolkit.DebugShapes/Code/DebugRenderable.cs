// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using static Stride.CommunityToolkit.DebugShapes.ImmediateDebugRenderSystem;

namespace Stride.CommunityToolkit.DebugShapes;

[StructLayout(LayoutKind.Explicit)]
internal struct DebugRenderable
{
    internal DebugRenderable(ref Quad q, DebugRenderableFlags renderFlags, float lifetime = 0f) : this()
    {
        Type = DebugPrimitiveType.Quad;
        Flags = renderFlags;
        Lifetime = lifetime;
        QuadData = q;
    }

    internal DebugRenderable(ref Circle c, DebugRenderableFlags renderFlags, float lifetime = 0f) : this()
    {
        Type = DebugPrimitiveType.Circle;
        Flags = renderFlags;
        Lifetime = lifetime;
        CircleData = c;
    }

    internal DebugRenderable(ref Line l, DebugRenderableFlags renderFlags, float lifetime = 0f) : this()
    {
        Type = DebugPrimitiveType.Line;
        Flags = renderFlags;
        Lifetime = lifetime;
        LineData = l;
    }

    internal DebugRenderable(ref Cube b, DebugRenderableFlags renderFlags, float lifetime = 0f) : this()
    {
        Type = DebugPrimitiveType.Cube;
        Flags = renderFlags;
        Lifetime = lifetime;
        CubeData = b;
    }

    internal DebugRenderable(ref Sphere s, DebugRenderableFlags renderFlags, float lifetime = 0f) : this()
    {
        Type = DebugPrimitiveType.Sphere;
        Flags = renderFlags;
        Lifetime = lifetime;
        SphereData = s;
    }

    internal DebugRenderable(ref HalfSphere h, DebugRenderableFlags renderFlags, float lifetime = 0f) : this()
    {
        Type = DebugPrimitiveType.HalfSphere;
        Flags = renderFlags;
        Lifetime = lifetime;
        HalfSphereData = h;
    }

    internal DebugRenderable(ref Capsule c, DebugRenderableFlags renderFlags, float lifetime = 0f) : this()
    {
        Type = DebugPrimitiveType.Capsule;
        Flags = renderFlags;
        Lifetime = lifetime;
        CapsuleData = c;
    }

    internal DebugRenderable(ref Cylinder c, DebugRenderableFlags renderFlags, float lifetime = 0f) : this()
    {
        Type = DebugPrimitiveType.Cylinder;
        Flags = renderFlags;
        Lifetime = lifetime;
        CylinderData = c;
    }

    internal DebugRenderable(ref Cone c, DebugRenderableFlags renderFlags, float lifetime = 0f) : this()
    {
        Type = DebugPrimitiveType.Cone;
        Flags = renderFlags;
        Lifetime = lifetime;
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
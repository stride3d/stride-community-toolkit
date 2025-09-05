// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using static Stride.CommunityToolkit.DebugShapes.Code.ImmediateDebugRenderSystem;

namespace Stride.CommunityToolkit.DebugShapes.Code;

[StructLayout(LayoutKind.Explicit)]
internal struct DebugRenderable
{
    internal DebugRenderable(ref Quad q, DebugRenderableFlags renderFlags) : this()
    {
        Type = DebugPrimitiveType.Quad;
        Flags = renderFlags;
        QuadData = q;
    }

    internal DebugRenderable(ref Circle c, DebugRenderableFlags renderFlags) : this()
    {
        Type = DebugPrimitiveType.Circle;
        Flags = renderFlags;
        CircleData = c;
    }

    internal DebugRenderable(ref Line l, DebugRenderableFlags renderFlags) : this()
    {
        Type = DebugPrimitiveType.Line;
        Flags = renderFlags;
        LineData = l;
    }

    internal DebugRenderable(ref Cube b, DebugRenderableFlags renderFlags) : this()
    {
        Type = DebugPrimitiveType.Cube;
        Flags = renderFlags;
        CubeData = b;
    }

    internal DebugRenderable(ref Sphere s, DebugRenderableFlags renderFlags) : this()
    {
        Type = DebugPrimitiveType.Sphere;
        Flags = renderFlags;
        SphereData = s;
    }

    internal DebugRenderable(ref HalfSphere h, DebugRenderableFlags renderFlags) : this()
    {
        Type = DebugPrimitiveType.HalfSphere;
        Flags = renderFlags;
        HalfSphereData = h;
    }

    internal DebugRenderable(ref Capsule c, DebugRenderableFlags renderFlags) : this()
    {
        Type = DebugPrimitiveType.Capsule;
        Flags = renderFlags;
        CapsuleData = c;
    }

    internal DebugRenderable(ref Cylinder c, DebugRenderableFlags renderFlags) : this()
    {
        Type = DebugPrimitiveType.Cylinder;
        Flags = renderFlags;
        CylinderData = c;
    }

    internal DebugRenderable(ref Cone c, DebugRenderableFlags renderFlags) : this()
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
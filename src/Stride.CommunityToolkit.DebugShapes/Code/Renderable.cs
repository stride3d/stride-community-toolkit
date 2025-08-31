// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.DebugShapes.Code;

[StructLayout(LayoutKind.Explicit)]
internal struct Renderable
{
    internal Renderable(ref Quad q) : this()
    {
        Type = DebugPrimitiveType.Quad;
        QuadData = q;
    }

    internal Renderable(ref Circle c) : this()
    {
        Type = DebugPrimitiveType.Circle;
        CircleData = c;
    }

    internal Renderable(ref Sphere s) : this()
    {
        Type = DebugPrimitiveType.Sphere;
        SphereData = s;
    }

    internal Renderable(ref HalfSphere h) : this()
    {
        Type = DebugPrimitiveType.HalfSphere;
        HalfSphereData = h;
    }

    internal Renderable(ref Cube c) : this()
    {
        Type = DebugPrimitiveType.Cube;
        CubeData = c;
    }

    internal Renderable(ref Capsule c) : this()
    {
        Type = DebugPrimitiveType.Capsule;
        CapsuleData = c;
    }

    internal Renderable(ref Cylinder c) : this()
    {
        Type = DebugPrimitiveType.Cylinder;
        CylinderData = c;
    }

    internal Renderable(ref Cone c) : this()
    {
        Type = DebugPrimitiveType.Cone;
        ConeData = c;
    }

    internal Renderable(ref Line l) : this()
    {
        Type = DebugPrimitiveType.Line;
        LineData = l;
    }

    [FieldOffset(0)]
    public DebugPrimitiveType Type;

    [FieldOffset(1)]
    public Quad QuadData;

    [FieldOffset(1)]
    public Circle CircleData;

    [FieldOffset(1)]
    public Sphere SphereData;

    [FieldOffset(1)]
    public HalfSphere HalfSphereData;

    [FieldOffset(1)]
    public Cube CubeData;

    [FieldOffset(1)]
    public Capsule CapsuleData;

    [FieldOffset(1)]
    public Cylinder CylinderData;

    [FieldOffset(1)]
    public Cone ConeData;

    [FieldOffset(1)]
    public Line LineData;
}
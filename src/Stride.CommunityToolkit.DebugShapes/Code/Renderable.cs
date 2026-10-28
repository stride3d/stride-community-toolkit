// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.DebugShapes;

/// <summary>
/// One shape handed to a render object: a tagged union of every payload. The payload shares one
/// offset, so the struct is as wide as the widest shape and no wider.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal struct Renderable
{
    private Renderable(DebugPrimitiveType type) => Type = type;

    internal Renderable(in Quad q) : this(DebugPrimitiveType.Quad) => QuadData = q;

    internal Renderable(in Circle c) : this(DebugPrimitiveType.Circle) => CircleData = c;

    internal Renderable(in Sphere s) : this(DebugPrimitiveType.Sphere) => SphereData = s;

    internal Renderable(in HalfSphere h) : this(DebugPrimitiveType.HalfSphere) => HalfSphereData = h;

    internal Renderable(in Cube c) : this(DebugPrimitiveType.Cube) => CubeData = c;

    internal Renderable(in Capsule c) : this(DebugPrimitiveType.Capsule) => CapsuleData = c;

    internal Renderable(in Cylinder c) : this(DebugPrimitiveType.Cylinder) => CylinderData = c;

    internal Renderable(in Cone c) : this(DebugPrimitiveType.Cone) => ConeData = c;

    internal Renderable(in Line l) : this(DebugPrimitiveType.Line) => LineData = l;

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
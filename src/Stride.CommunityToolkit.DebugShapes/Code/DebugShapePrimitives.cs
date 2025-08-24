// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.DebugShapes.Code;

/// <summary>
/// Shared debug shape primitive payload structs used by ImmediateDebugRenderSystem and ImmediateDebugRenderFeature.
/// </summary>
internal static class DebugShapePrimitives
{
}

[StructLayout(LayoutKind.Sequential)]
internal struct Quad
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector2 Size;
    public Color Color;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Circle
{
    public Vector3 Position;
    public Quaternion Rotation;
    public float Radius;
    public Color Color;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Sphere
{
    public Vector3 Position;
    public float Radius;
    public Color Color;
}

[StructLayout(LayoutKind.Sequential)]
internal struct HalfSphere
{
    public Vector3 Position;
    public float Radius;
    public Quaternion Rotation;
    public Color Color;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Cube
{
    public Vector3 Start;
    public Vector3 End;
    public Quaternion Rotation;
    public Color Color;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Capsule
{
    public Vector3 Position;
    public float Height;
    public float Radius;
    public Quaternion Rotation;
    public Color Color;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Cylinder
{
    public Vector3 Position;
    public float Height;
    public float Radius;
    public Quaternion Rotation;
    public Color Color;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Cone
{
    public Vector3 Position;
    public float Height;
    public float Radius;
    public Quaternion Rotation;
    public Color Color;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Line
{
    public Vector3 Start;
    public Vector3 End;
    public Color Color;
}
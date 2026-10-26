// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.CommunityToolkit.DebugShapes;

/// <summary>
/// One count per primitive kind: how many of each a frame draws, or where each kind's run starts in a
/// packed buffer.
/// </summary>
internal struct Primitives
{
    public int Quads;
    public int Circles;
    public int Spheres;
    public int HalfSpheres;
    public int Cubes;
    public int Capsules;
    public int Cylinders;
    public int Cones;
    public int Lines;

    /// <summary>Every kind but lines, which have a buffer of their own: the instance slots a frame needs.</summary>
    internal int InstanceTotal => Quads + Circles + Spheres + HalfSpheres + Cubes + Capsules + Cylinders + Cones;

    /// <summary>
    /// Where each kind's run starts when the counts are packed one after another from <paramref name="first"/>.
    /// Lines are left at zero for the caller, since they live in their own buffer.
    /// </summary>
    internal Primitives Offsets(int first)
    {
        var offsets = new Primitives { Quads = first };

        offsets.Circles = offsets.Quads + Quads;
        offsets.Spheres = offsets.Circles + Circles;
        offsets.HalfSpheres = offsets.Spheres + Spheres;
        offsets.Cubes = offsets.HalfSpheres + HalfSpheres;
        offsets.Capsules = offsets.Cubes + Cubes;
        offsets.Cylinders = offsets.Capsules + Capsules;
        offsets.Cones = offsets.Cylinders + Cylinders;

        return offsets;
    }

    /// <summary>Adds one to the count of <paramref name="type"/>.</summary>
    internal void Increment(DebugPrimitiveType type)
    {
        switch (type)
        {
            case DebugPrimitiveType.Quad: Quads++; break;
            case DebugPrimitiveType.Circle: Circles++; break;
            case DebugPrimitiveType.Line: Lines++; break;
            case DebugPrimitiveType.Cube: Cubes++; break;
            case DebugPrimitiveType.Sphere: Spheres++; break;
            case DebugPrimitiveType.HalfSphere: HalfSpheres++; break;
            case DebugPrimitiveType.Capsule: Capsules++; break;
            case DebugPrimitiveType.Cylinder: Cylinders++; break;
            case DebugPrimitiveType.Cone: Cones++; break;
        }
    }

    /// <summary>Resets every count to zero.</summary>
    internal void Clear() => this = default;
}
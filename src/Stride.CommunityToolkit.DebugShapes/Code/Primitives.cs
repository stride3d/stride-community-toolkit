// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.CommunityToolkit.DebugShapes.Code;

/// <summary>
/// Counts of different primitive types.
/// </summary>
public struct Primitives
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

    public void Clear()
    {
        Quads = 0;
        Circles = 0;
        Spheres = 0;
        HalfSpheres = 0;
        Cubes = 0;
        Capsules = 0;
        Cylinders = 0;
        Cones = 0;
        Lines = 0;
    }
}
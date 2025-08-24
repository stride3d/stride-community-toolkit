// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.CommunityToolkit.DebugShapes.Code;

/// <summary>
/// Represents the type of debug primitive that can be rendered.
/// </summary>
public enum DebugPrimitiveType : byte
{
    /// <summary>Quad primitive.</summary>
    Quad,
    /// <summary>Circle primitive.</summary>
    Circle,
    /// <summary>Line primitive.</summary>
    Line,
    /// <summary>Cube primitive.</summary>
    Cube,
    /// <summary>Sphere primitive.</summary>
    Sphere,
    /// <summary>Half-sphere primitive.</summary>
    HalfSphere,
    /// <summary>Capsule primitive.</summary>
    Capsule,
    /// <summary>Cylinder primitive.</summary>
    Cylinder,
    /// <summary>Cone primitive.</summary>
    Cone
}
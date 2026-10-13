// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.DebugShapes;

/// <summary>
/// The payload of a debug quad: what <see cref="ImmediateDebugRenderSystem"/> records when one is
/// requested, and what the renderer reads back when it builds the instance data.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct Quad
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector2 Size;
    public Color Color;
}
// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Graphics;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.DebugShapes;

/// <summary>The vertex of a debug line: a position and a colour, as the line shader reads them.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct LineVertex(Vector3 position, Color color)
{
    public static readonly VertexDeclaration Layout = new(VertexElement.Position<Vector3>(), VertexElement.Color<Color>());

    public Vector3 Position = position;
    public Color Color = color;
}
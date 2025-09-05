// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Graphics;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.DebugShapes.Code;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct LineVertex
{
    public static readonly VertexDeclaration Layout = new VertexDeclaration(VertexElement.Position<Vector3>(), VertexElement.Color<Color>());

    public Vector3 Position;
    public Color Color;
}
// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// -----------------------------------------------------------------------------
// The following code is a port of DirectXTk http://directxtk.codeplex.com
// -----------------------------------------------------------------------------
// Microsoft Public License (Ms-PL)
//
// This license governs use of the accompanying software. If you use the
// software, you accept this license. If you do not accept the license, do not
// use the software.
//
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and
// "distribution" have the same meaning here as under U.S. copyright law.
// A "contribution" is the original software, or any additions or changes to
// the software.
// A "contributor" is any person that distributes its contribution under this
// license.
// "Licensed patents" are a contributor's patent claims that read directly on
// its contribution.
//
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the
// license conditions and limitations in section 3, each contributor grants
// you a non-exclusive, worldwide, royalty-free copyright license to reproduce
// its contribution, prepare derivative works of its contribution, and
// distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license
// conditions and limitations in section 3, each contributor grants you a
// non-exclusive, worldwide, royalty-free license under its licensed patents to
// make, have made, use, sell, offer for sale, import, and/or otherwise dispose
// of its contribution in the software or derivative works of the contribution
// in the software.
//
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any
// contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that
// you claim are infringed by the software, your patent license from such
// contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all
// copyright, patent, trademark, and attribution notices that are present in the
// software.
// (D) If you distribute any portion of the software in source code form, you
// may do so only under this license by including a complete copy of this
// license with your distribution. If you distribute any portion of the software
// in compiled or object code form, you may only do so under a license that
// complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The
// contributors give no express warranties, guarantees or conditions. You may
// have additional consumer rights under your local laws which this license
// cannot change. To the extent permitted under your local laws, the
// contributors exclude the implied warranties of merchantability, fitness for a
// particular purpose and non-infringement.

using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;

namespace Stride.CommunityToolkit.DebugShapes;

/// <summary>
/// Provides methods for generating geometric primitives for debug visualization.
/// </summary>
/// <remarks>
/// This type is the public entry point; the heavier mesh generation lives in
/// <see cref="CircularDebugPrimitives"/> (circle, cylinder, cone) and
/// <see cref="SphericalDebugPrimitives"/> (sphere, capsule), with the shared wireframe
/// uv constants in <see cref="DebugPrimitiveUv"/>.
/// </remarks>
public static class ImmediateDebugPrimitives
{
    /// <summary>
    /// Calculates a vector on the circumference of a circle in the XZ plane.
    /// </summary>
    /// <param name="i">The index of the segment.</param>
    /// <param name="tessellation">The total number of segments in the circle.</param>
    /// <returns>A <see cref="Vector3"/> representing the position on the circle.</returns>
    public static Vector3 GetCircleVector(int i, int tessellation)
    {
        var angle = (float)(i * 2.0 * Math.PI / tessellation);
        var dx = (float)Math.Sin(angle);
        var dz = (float)Math.Cos(angle);

        return new Vector3(dx, 0, dz);
    }

    /// <summary>
    /// Copies vertex positions and texture coordinates from a geometric primitive to arrays for rendering.
    /// </summary>
    /// <param name="primitiveData">The source geometric mesh data.</param>
    /// <param name="vertices">The destination vertex array.</param>
    /// <param name="indices">The destination index array.</param>
    public static void CopyFromGeometricPrimitive(GeometricMeshData<VertexPositionNormalTexture> primitiveData, ref VertexPositionTexture[] vertices, ref int[] indices)
    {
        for (int i = 0; i < vertices.Length; ++i)
        {
            vertices[i].Position = primitiveData.Vertices[i].Position;
            vertices[i].TextureCoordinate = primitiveData.Vertices[i].TextureCoordinate;
        }

        for (int i = 0; i < indices.Length; ++i)
        {
            indices[i] = primitiveData.Indices[i];
        }
    }

    /// <summary>
    /// Generates a quad (rectangle) mesh with the specified width and height.
    /// </summary>
    /// <param name="width">The width of the quad.</param>
    /// <param name="height">The height of the quad.</param>
    /// <returns>Arrays of vertices and indices representing the quad.</returns>
    public static (VertexPositionTexture[] Vertices, int[] Indices) GenerateQuad(float width, float height)
    {
        var quadMeshData = GeometricPrimitive.Plane.New(width, height);
        VertexPositionTexture[] vertices = new VertexPositionTexture[quadMeshData.Vertices.Length];
        int[] indices = new int[quadMeshData.Indices.Length];

        CopyFromGeometricPrimitive(quadMeshData, ref vertices, ref indices);

        // transform it because in its default orientation it isn't flat to the normal up
        Quaternion rotation = Quaternion.BetweenDirections(Vector3.UnitZ, Vector3.UnitY);
        for (int i = 0; i < vertices.Length; ++i)
        {
            vertices[i].Position = Vector3.Transform(vertices[i].Position, rotation);
        }

        return (vertices, indices);
    }

    /// <summary>
    /// Generates a circle mesh with optional UV splits and offset.
    /// </summary>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="tessellations">The number of segments used to approximate the circle.</param>
    /// <param name="uvSplits">The number of UV splits for wireframe rendering.</param>
    /// <param name="yOffset">Vertical offset for the circle.</param>
    /// <param name="isFlipped">Whether to flip the winding order.</param>
    /// <param name="uvOffset">Offset for UV splits.</param>
    /// <returns>Arrays of vertices and indices representing the circle.</returns>
    public static (VertexPositionTexture[] Vertices, int[] Indices) GenerateCircle(float radius = 0.5f, int tessellations = 16, int uvSplits = 0, float yOffset = 0.0f, bool isFlipped = false, int uvOffset = 0)
        => CircularDebugPrimitives.GenerateCircle(radius, tessellations, uvSplits, yOffset, isFlipped, uvOffset);

    /// <summary>
    /// Generates a cube mesh with the specified size.
    /// </summary>
    /// <param name="size">The size of the cube.</param>
    /// <returns>Arrays of vertices and indices representing the cube.</returns>
    public static (VertexPositionTexture[] Vertices, int[] Indices) GenerateCube(float size = 1.0f)
    {
        var cubeMeshData = GeometricPrimitive.Cube.New(size);
        VertexPositionTexture[] vertices = new VertexPositionTexture[cubeMeshData.Vertices.Length];
        int[] indices = new int[cubeMeshData.Indices.Length];

        CopyFromGeometricPrimitive(cubeMeshData, ref vertices, ref indices);

        return (vertices, indices);
    }

    /// <summary>
    /// Generates a sphere mesh with optional UV splits and vertical offset.
    /// </summary>
    /// <param name="radius">The radius of the sphere.</param>
    /// <param name="tessellations">The number of segments used to approximate the sphere.</param>
    /// <param name="uvSplits">The number of UV splits for wireframe rendering.</param>
    /// <param name="uvSplitOffsetVertical">Vertical offset for UV splits.</param>
    /// <returns>Arrays of vertices and indices representing the sphere.</returns>
    public static (VertexPositionTexture[] Vertices, int[] Indices) GenerateSphere(float radius = 0.5f, int tessellations = 16, int uvSplits = 4, int uvSplitOffsetVertical = 0)
        => SphericalDebugPrimitives.GenerateSphere(radius, tessellations, uvSplits, uvSplitOffsetVertical);

    /// <summary>
    /// Generates a cylinder mesh with optional UV splits and circle side splits.
    /// </summary>
    /// <param name="height">The height of the cylinder.</param>
    /// <param name="radius">The radius of the cylinder.</param>
    /// <param name="tessellations">The number of segments used to approximate the cylinder.</param>
    /// <param name="uvSplits">The number of UV splits for wireframe rendering.</param>
    /// <param name="uvSidesForCircle">Number of sides for the circle caps (optional).</param>
    /// <returns>Arrays of vertices and indices representing the cylinder.</returns>
    public static (VertexPositionTexture[] Vertices, int[] Indices) GenerateCylinder(float height = 1.0f, float radius = 0.5f, int tessellations = 16, int uvSplits = 4, int? uvSidesForCircle = null)
        => CircularDebugPrimitives.GenerateCylinder(height, radius, tessellations, uvSplits, uvSidesForCircle);

    /// <summary>
    /// Generates a cone mesh with optional UV splits for the top and bottom.
    /// </summary>
    /// <param name="height">The height of the cone.</param>
    /// <param name="radius">The radius of the base of the cone.</param>
    /// <param name="tessellations">The number of segments used to approximate the cone.</param>
    /// <param name="uvSplits">The number of UV splits for wireframe rendering (top).</param>
    /// <param name="uvSplitsBottom">The number of UV splits for wireframe rendering (bottom).</param>
    /// <returns>Arrays of vertices and indices representing the cone.</returns>
    public static (VertexPositionTexture[] Vertices, int[] Indices) GenerateCone(float height, float radius, int tessellations, int uvSplits = 4, int uvSplitsBottom = 0)
        => CircularDebugPrimitives.GenerateCone(height, radius, tessellations, uvSplits, uvSplitsBottom);

    /// <summary>
    /// Generates a capsule mesh with optional UV splits.
    /// </summary>
    /// <param name="length">The length of the capsule (distance between hemispheres).</param>
    /// <param name="radius">The radius of the capsule.</param>
    /// <param name="tessellations">The number of segments used to approximate the capsule.</param>
    /// <param name="uvSplits">The number of UV splits for wireframe rendering.</param>
    /// <returns>Arrays of vertices and indices representing the capsule.</returns>
    public static (VertexPositionTexture[] Vertices, int[] Indices) GenerateCapsule(float length, float radius, int tessellations, int uvSplits = 4)
        => SphericalDebugPrimitives.GenerateCapsule(length, radius, tessellations, uvSplits);
}
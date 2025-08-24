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

namespace Stride.CommunityToolkit.DebugShapes.Code;

/// <summary>
/// Provides methods for generating geometric primitives for debug visualization.
/// </summary>
public static class ImmediateDebugPrimitives
{
    private static readonly Vector2 _noLineUv = new(0.5f);
    private static readonly Vector2 _lineUv = new(1.0f);

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
    {
        if (tessellations < 3) tessellations = 3;

        if (uvSplits != 0 && tessellations % uvSplits != 0) // FIXME: this can read a lot nicer i think?
        {
            throw new ArgumentException("expected the desired number of uv splits to be a divisor of the number of tessellations");
        }

        int hasUvSplits = uvSplits > 0 ? 1 : 0;
        int extraVertices = 0;
        int extraIndices = 0;

        if (hasUvSplits > 0)
        {
            for (int i = 0; i < tessellations * 3; i += 3)
            {
                int splitMod = (i / 3 - uvOffset) % (tessellations / uvSplits);
                var timeToSplit = splitMod == 0;
                if (timeToSplit)
                {
                    extraVertices += 2;
                    extraIndices += 3;
                }
            }
        }

        VertexPositionTexture[] vertices = new VertexPositionTexture[tessellations + 1 + hasUvSplits + extraVertices];
        int[] indices = new int[(tessellations + 1) * 3 + extraIndices];

        double radiansPerSegment = MathUtil.TwoPi / tessellations;

        // center of our circle
        vertices[0].Position = new Vector3(0.0f, yOffset, 0.0f);
        vertices[0].TextureCoordinate = _noLineUv;

        // center, but with uv coords set
        if (hasUvSplits > 0)
        {
            vertices[1].Position = new Vector3(0.0f, yOffset, 0.0f);
            vertices[1].TextureCoordinate = _lineUv;
        }

        int offset = 1 + hasUvSplits;
        for (int i = 0; i < tessellations; ++i)
        {
            var normal = GetCircleVector(i, tessellations);
            vertices[offset + i].Position = normal * radius + new Vector3(0.0f, yOffset, 0.0f);
            vertices[offset + i].TextureCoordinate = _lineUv;
        }

        int curVert = tessellations + offset;
        int curIdx = (tessellations + 1) * 3;
        for (int i = 0; i < tessellations * 3; i += 3)
        {
            int? splitMod = uvSplits > 0 ? (i / 3 - uvOffset) % (tessellations / uvSplits) : null;
            var timeToSplit = splitMod == 0;
            if (timeToSplit)
            {
                indices[i] = 1;

                indices[i + 1] = curVert;
                vertices[curVert] = vertices[offset + i / 3 % tessellations];
                vertices[curVert++].TextureCoordinate = _lineUv;

                indices[i + 2] = curVert;
                vertices[curVert] = vertices[offset + (i / 3 + 1) % tessellations];
                vertices[curVert++].TextureCoordinate = _noLineUv;

                // FIXME: this is shit geometry really
                indices[curIdx++] = offset + i / 3 % tessellations;
                indices[curIdx++] = offset + i / 3 % tessellations;
                indices[curIdx++] = offset + (i / 3 + 1) % tessellations;
            }
            else
            {
                indices[i] = 0;
                indices[i + 1] = offset + i / 3 % tessellations;
                indices[i + 2] = offset + (i / 3 + 1) % tessellations;
            }
        }

        if (!isFlipped)
        {
            Array.Reverse(indices); // flip the winding if it's a top piece
        }

        return (vertices, indices);
    }

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
    {
        if (tessellations < 3) tessellations = 3;

        if (uvSplits != 0 && tessellations % uvSplits != 0) // FIXME: this can read a lot nicer i think?
        {
            throw new ArgumentException("expected the desired number of uv splits to be a divisor of the number of tessellations");
        }

        int verticalSegments = tessellations;
        int horizontalSegments = tessellations * 2;
        int hasUvSplit = uvSplits > 0 ? 1 : 0;

        // FIXME: i tried figuring out a closed form solution for this bugger here, but i feel like i'm missing something crucial...
        //  it basically is just here to calculate how many extra vertices are needed to create the wireframe topology we want
        // if *you* can figure out a closed form solution, have at it! you are very welcome!
        int extraVertexCount = 0;

        if (hasUvSplit > 0)
        {
            for (int i = 0; i < verticalSegments; i++)
            {
                for (int j = 0; j <= horizontalSegments; j++)
                {
                    int vertModulo = (i + uvSplitOffsetVertical) % (verticalSegments / uvSplits);
                    int horizModulo = j % (horizontalSegments / uvSplits);
                    if (hasUvSplit > 0 && vertModulo == 0 && horizModulo == 0)
                    {
                        extraVertexCount += 4;
                    }
                    else if (hasUvSplit > 0 && (vertModulo == 0 || horizModulo == 0))
                    {
                        extraVertexCount += 2;
                    }
                }
            }
        }

        var vertices = new VertexPositionTexture[(verticalSegments + 1) * (horizontalSegments + 1) + extraVertexCount];
        var indices = new int[verticalSegments * (horizontalSegments + 1) * 6];

        int vertexCount = 0;

        // generate the first extremity points
        for (int j = 0; j <= horizontalSegments; j++)
        {
            var normal = new Vector3(0, -1, 0);
            var textureCoordinate = new Vector2(0.5f);
            vertices[vertexCount++] = new VertexPositionTexture(normal * radius, textureCoordinate);
        }

        // Create rings of vertices at progressively higher latitudes.
        for (int i = 1; i < verticalSegments; i++)
        {

            var latitude = (float)(i * Math.PI / verticalSegments - Math.PI / 2.0);
            var dy = (float)Math.Sin(latitude);
            var dxz = (float)Math.Cos(latitude);

            // the first point
            var firstNormal = new Vector3(0, dy, dxz);
            var firstHorizontalVertex = new VertexPositionTexture(firstNormal * radius, _noLineUv);
            vertices[vertexCount++] = firstHorizontalVertex;

            // Create a single ring of vertices at this latitude.
            for (int j = 1; j < horizontalSegments; j++)
            {

                var longitude = (float)(j * 2.0 * Math.PI / horizontalSegments);
                var dx = (float)Math.Sin(longitude);
                var dz = (float)Math.Cos(longitude);

                dx *= dxz;
                dz *= dxz;

                var normal = new Vector3(dx, dy, dz);
                var textureCoordinate = _noLineUv;

                vertices[vertexCount++] = new VertexPositionTexture(normal * radius, textureCoordinate);
            }

            // the last point equal to the first point
            firstHorizontalVertex.TextureCoordinate = _noLineUv;
            vertices[vertexCount++] = firstHorizontalVertex;
        }

        // generate the end extremity points
        for (int j = 0; j <= horizontalSegments; j++)
        {
            var normal = new Vector3(0, 1, 0);
            var textureCoordinate = _noLineUv;
            vertices[vertexCount++] = new VertexPositionTexture(normal * radius, textureCoordinate);
        }

        // Fill the index buffer with triangles joining each pair of latitude rings.
        int stride = horizontalSegments + 1;

        int indexCount = 0;
        int newVertexCount = vertexCount;
        for (int i = 0; i < verticalSegments; i++)
        {
            for (int j = 0; j <= horizontalSegments; j++)
            {
                int nextI = i + 1;
                int nextJ = (j + 1) % stride;
                int? vertModulo = uvSplits > 0 ? (i + uvSplitOffsetVertical) % (verticalSegments / uvSplits) : null;
                int? horizModulo = uvSplits > 0 ? j % (horizontalSegments / uvSplits) : null;
                if (hasUvSplit > 0 && vertModulo == 0 && horizModulo == 0)
                {
                    vertices[newVertexCount] = vertices[i * stride + j];
                    vertices[newVertexCount].TextureCoordinate = _lineUv;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (i * stride + j);

                    vertices[newVertexCount] = vertices[nextI * stride + j];
                    vertices[newVertexCount].TextureCoordinate = _lineUv;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (nextI * stride + j);

                    indices[indexCount++] = i * stride + nextJ;

                    indices[indexCount++] = i * stride + nextJ;

                    vertices[newVertexCount] = vertices[nextI * stride + j];
                    vertices[newVertexCount].TextureCoordinate = _lineUv;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (nextI * stride + j);

                    vertices[newVertexCount] = vertices[nextI * stride + nextJ];
                    vertices[newVertexCount].TextureCoordinate = _lineUv;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (nextI * stride + nextJ);
                }
                else if (hasUvSplit > 0 && vertModulo == 0)
                {
                    indices[indexCount++] = i * stride + j;
                    indices[indexCount++] = nextI * stride + j;
                    indices[indexCount++] = i * stride + nextJ;

                    indices[indexCount++] = i * stride + nextJ;

                    vertices[newVertexCount] = vertices[nextI * stride + j];
                    vertices[newVertexCount].TextureCoordinate = _lineUv;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (nextI * stride + j);

                    vertices[newVertexCount] = vertices[nextI * stride + nextJ];
                    vertices[newVertexCount].TextureCoordinate = _lineUv;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (nextI * stride + nextJ);
                }
                else if (hasUvSplit > 0 && horizModulo == 0)
                {
                    vertices[newVertexCount] = vertices[i * stride + j];
                    vertices[newVertexCount].TextureCoordinate = _lineUv;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (i * stride + j);

                    vertices[newVertexCount] = vertices[nextI * stride + j];
                    vertices[newVertexCount].TextureCoordinate = _lineUv;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (nextI * stride + j);

                    indices[indexCount++] = i * stride + nextJ;

                    indices[indexCount++] = i * stride + nextJ;
                    indices[indexCount++] = nextI * stride + j;
                    indices[indexCount++] = nextI * stride + nextJ;
                }
                else
                {
                    indices[indexCount++] = i * stride + j;
                    indices[indexCount++] = nextI * stride + j;
                    indices[indexCount++] = i * stride + nextJ;

                    indices[indexCount++] = i * stride + nextJ;
                    indices[indexCount++] = nextI * stride + j;
                    indices[indexCount++] = nextI * stride + nextJ;
                }
            }
        }

        return (vertices, indices);
    }

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
    {
        const int uvOffset = 3; // FIXME: this magic constant here is to get the splits to appear aesthetically similar orientation wise for all the shapes

        if (tessellations < 3) tessellations = 3;

        if (uvSplits != 0 && tessellations % uvSplits != 0) // FIXME: this can read a lot nicer i think?
        {
            throw new ArgumentException("expected the desired number of uv splits to be a divisor of the number of tessellations");
        }

        var hasUvSplit = uvSplits > 0 ? 1 : 0;
        var (capVertices, capIndices) = GenerateCircle(radius, tessellations, uvSidesForCircle ?? uvSplits, uvOffset: 1 + uvOffset);

        VertexPositionTexture[] vertices = new VertexPositionTexture[capVertices.Length * 2 + tessellations * 4];
        int[] indices = new int[capIndices.Length * 2 + tessellations * 6];

        int bottomVertsOffset = vertices.Length - capVertices.Length;
        int topVertsOffset = vertices.Length - capVertices.Length * 2;
        int bottomIndicesOffset = indices.Length - capIndices.Length;
        int topIndicesOffset = indices.Length - capIndices.Length * 2;

        // copy vertices
        for (int i = 0; i < capVertices.Length; ++i)
        {
            vertices[bottomVertsOffset + i] = capVertices[i];
            vertices[bottomVertsOffset + i].Position.Y = -(height / 2.0f);
            vertices[topVertsOffset + i] = capVertices[i];
            vertices[topVertsOffset + i].Position.Y = height / 2.0f;
        }

        // copy indices
        for (int i = 0; i < capIndices.Length; ++i)
        {
            indices[bottomIndicesOffset + i] = capIndices[i] + bottomVertsOffset;
            indices[topIndicesOffset + i] = capIndices[i] + topVertsOffset;
        }

        // correct winding order so backface is inwards for bottom part
        Array.Reverse(indices, bottomIndicesOffset, capIndices.Length);

        // generate sides, using our top and bottom circle triangle fans
        int curVert = 0;
        int curIndex = 0;
        for (int i = 0; i < tessellations; ++i)
        {
            var normal = GetCircleVector(i, tessellations);
            var curTopPos = normal * radius + Vector3.UnitY * (height / 2.0f);
            var curBottomPos = normal * radius - Vector3.UnitY * (height / 2.0f);

            int? sideModulo = uvSplits > 0 ? (i + 1 - uvOffset) % (tessellations / uvSplits) : null;

            vertices[curVert].Position = curBottomPos;
            vertices[curVert].TextureCoordinate = sideModulo == 0 ? _lineUv : _noLineUv;
            var ip = curVert++;

            var nextBottomNormal = GetCircleVector(i + 1, tessellations) * radius - Vector3.UnitY * (height / 2.0f);
            vertices[curVert].Position = nextBottomNormal;
            vertices[curVert].TextureCoordinate = _noLineUv;
            var ip1 = curVert++;

            vertices[curVert].Position = curTopPos;
            vertices[curVert].TextureCoordinate = sideModulo == 0 ? _lineUv : _noLineUv;
            var ipv = curVert++;

            var nextTopNormal = GetCircleVector(i + 1, tessellations) * radius + Vector3.UnitY * (height / 2.0f);
            vertices[curVert].Position = nextTopNormal;
            vertices[curVert].TextureCoordinate = _noLineUv;
            var ipv1 = curVert++;

            // reuse the old stuff yo
            indices[curIndex++] = ipv;
            indices[curIndex++] = ip1;
            indices[curIndex++] = ip;

            indices[curIndex++] = ipv;
            indices[curIndex++] = ipv1;
            indices[curIndex++] = ip1;
        }

        return (vertices, indices);
    }

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
    {
        if (tessellations < 3) tessellations = 3;

        if (uvSplits != 0 && tessellations % uvSplits != 0) // FIXME: this can read a lot nicer i think?
        {
            throw new ArgumentException("expected the desired number of uv splits to be a divisor of the number of tessellations");
        }

        if (uvSplitsBottom != 0 && tessellations % uvSplitsBottom != 0) // FIXME: this can read a lot nicer i think?
        {
            throw new ArgumentException("expected the desired number of uv splits for the bottom to be a divisor of the number of tessellations");
        }

        var (bottomVertices, bottomIndices) = GenerateCircle(radius, tessellations, uvSplits, yOffset: -(height / 2.0f));
        var (topVertices, topIndices) = GenerateCircle(radius, tessellations, uvSplitsBottom, isFlipped: true, yOffset: -(height / 2.0f));
        VertexPositionTexture[] vertices = new VertexPositionTexture[bottomVertices.Length + topVertices.Length];
        int[] indices = new int[topIndices.Length + bottomIndices.Length];

        // copy vertices from circle
        for (int i = 0; i < bottomVertices.Length; ++i)
        {
            vertices[i] = bottomVertices[i];
        }

        for (int i = 0; i < topVertices.Length; ++i)
        {
            vertices[i + bottomVertices.Length] = topVertices[i];
        }

        // copy indices from circle
        for (int i = 0; i < bottomIndices.Length; ++i)
        {
            indices[i] = bottomIndices[i];
        }

        for (int i = 0; i < topIndices.Length; ++i)
        {
            indices[i + bottomIndices.Length] = topIndices[i] + bottomVertices.Length;
        }

        // extrude middle vertex of center of first circle triangle fan
        vertices[0].Position.Y = height / 2.0f;
        vertices[1].Position.Y = height / 2.0f;

        return (vertices, indices);
    }

    /// <summary>
    /// Generates a capsule mesh with optional UV splits.
    /// </summary>
    /// <param name="length">The length of the capsule (distance between hemispheres).</param>
    /// <param name="radius">The radius of the capsule.</param>
    /// <param name="tessellations">The number of segments used to approximate the capsule.</param>
    /// <param name="uvSplits">The number of UV splits for wireframe rendering.</param>
    /// <returns>Arrays of vertices and indices representing the capsule.</returns>
    public static (VertexPositionTexture[] Vertices, int[] Indices) GenerateCapsule(float length, float radius, int tessellations, int uvSplits = 4)
    {
        if (tessellations < 3) tessellations = 3;

        if (uvSplits != 0 && tessellations % uvSplits != 0) // FIXME: this can read a lot nicer i think?
        {
            throw new ArgumentException("expected the desired number of uv splits to be a divisor of the number of tessellations");
        }

        int verticalSegments = 2 * tessellations;
        int horizontalSegments = 4 * tessellations;
        int hasUvSplit = uvSplits > 0 ? 1 : 0;

        // FIXME: i tried figuring out a closed form solution for this bugger here, but i feel like i'm missing something crucial...
        //  it basically is just here to calculate how many extra vertices are needed to create the wireframe topology we want
        // if *you* can figure out a closed form solution, have at it! you are very welcome!
        int extraVertexCount = 0;

        if (hasUvSplit > 0)
        {
            for (int i = 0; i < verticalSegments - 1; i++)
            {
                for (int j = 0; j <= horizontalSegments; j++)
                {
                    int vertModulo = i % (verticalSegments / uvSplits);
                    int horizModulo = j % (horizontalSegments / uvSplits);
                    if (hasUvSplit > 0 && vertModulo == 0 && horizModulo == 0)
                    {
                        extraVertexCount += 4;
                    }
                    else if (hasUvSplit > 0 && (vertModulo == 0 || horizModulo == 0))
                    {
                        extraVertexCount += 2;
                    }
                }
            }
        }

        var vertices = new VertexPositionTexture[verticalSegments * (horizontalSegments + 1) + extraVertexCount];
        var indices = new int[(verticalSegments - 1) * (horizontalSegments + 1) * 6];

        var vertexCount = 0;
        // Create rings of vertices at progressively higher latitudes.
        for (int i = 0; i < verticalSegments; i++)
        {
            float deltaY;
            float latitude;

            if (i < verticalSegments / 2)
            {
                deltaY = -length / 2;
                latitude = (float)(i * Math.PI / (verticalSegments - 2) - Math.PI / 2.0);
            }
            else
            {
                deltaY = length / 2;
                latitude = (float)((i - 1) * Math.PI / (verticalSegments - 2) - Math.PI / 2.0);
            }

            var dy = (float)Math.Sin(latitude);
            var dxz = (float)Math.Cos(latitude);

            // Create a single ring of vertices at this latitude.
            for (int j = 0; j <= horizontalSegments; j++)
            {

                var longitude = (float)(j * 2.0 * Math.PI / horizontalSegments);
                var dx = (float)Math.Sin(longitude);
                var dz = (float)Math.Cos(longitude);

                dx *= dxz;
                dz *= dxz;

                var normal = new Vector3(dx, dy, dz);
                var textureCoordinate = _noLineUv;
                var position = radius * normal + new Vector3(0, deltaY, 0);

                vertices[vertexCount++] = new VertexPositionTexture(position, textureCoordinate);
            }
        }

        // Fill the index buffer with triangles joining each pair of latitude rings.
        int stride = horizontalSegments + 1;

        int indexCount = 0;
        int newVertexCount = vertexCount;
        for (int i = 0; i < verticalSegments - 1; i++)
        {
            for (int j = 0; j <= horizontalSegments; j++)
            {
                int nextI = i + 1;
                int nextJ = (j + 1) % stride;
                int? vertModulo = uvSplits > 0 ? i % (verticalSegments / uvSplits) : null;
                int? horizModulo = uvSplits > 0 ? j % (horizontalSegments / uvSplits) : null;
                if (hasUvSplit > 0 && vertModulo == 0 && horizModulo == 0)
                {
                    vertices[newVertexCount] = vertices[i * stride + j];
                    vertices[newVertexCount].TextureCoordinate = _lineUv;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (i * stride + j);

                    vertices[newVertexCount] = vertices[nextI * stride + j];
                    vertices[newVertexCount].TextureCoordinate = _lineUv;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (nextI * stride + j);

                    indices[indexCount++] = i * stride + nextJ;
                    indices[indexCount++] = i * stride + nextJ;

                    vertices[newVertexCount] = vertices[nextI * stride + j];
                    vertices[newVertexCount].TextureCoordinate = _lineUv;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (nextI * stride + j);

                    vertices[newVertexCount] = vertices[nextI * stride + nextJ];
                    vertices[newVertexCount].TextureCoordinate = _lineUv;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (nextI * stride + nextJ);
                }
                else if (hasUvSplit > 0 && vertModulo == 0)
                {
                    indices[indexCount++] = i * stride + j;
                    indices[indexCount++] = nextI * stride + j;
                    indices[indexCount++] = i * stride + nextJ;

                    indices[indexCount++] = i * stride + nextJ;

                    vertices[newVertexCount] = vertices[nextI * stride + j];
                    vertices[newVertexCount].TextureCoordinate = _lineUv;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (nextI * stride + j);

                    vertices[newVertexCount] = vertices[nextI * stride + nextJ];
                    vertices[newVertexCount].TextureCoordinate = _lineUv;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (nextI * stride + nextJ);
                }
                else if (hasUvSplit > 0 && horizModulo == 0)
                {
                    vertices[newVertexCount] = vertices[i * stride + j];
                    vertices[newVertexCount].TextureCoordinate = _lineUv;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (i * stride + j);

                    vertices[newVertexCount] = vertices[nextI * stride + j];
                    vertices[newVertexCount].TextureCoordinate = _lineUv;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (nextI * stride + j);

                    indices[indexCount++] = i * stride + nextJ;


                    indices[indexCount++] = i * stride + nextJ;
                    indices[indexCount++] = nextI * stride + j;
                    indices[indexCount++] = nextI * stride + nextJ;
                }
                else
                {
                    indices[indexCount++] = i * stride + j;
                    indices[indexCount++] = nextI * stride + j;
                    indices[indexCount++] = i * stride + nextJ;

                    indices[indexCount++] = i * stride + nextJ;
                    indices[indexCount++] = nextI * stride + j;
                    indices[indexCount++] = nextI * stride + nextJ;
                }
            }
        }

        return (vertices, indices);
    }
}
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

using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.CommunityToolkit.DebugShapes;

/// <summary>
/// Generates the latitude/longitude-based debug meshes: sphere and capsule. Both build the same
/// ring topology, so they share the wireframe vertex counting and index filling here. Split out of
/// <see cref="ImmediateDebugPrimitives"/>, which remains the public entry point.
/// </summary>
internal static class SphericalDebugPrimitives
{
    /// <summary>
    /// The lat/long grid a sphere or capsule is built on. <see cref="VerticalLoopCount"/> is the
    /// number of ring pairs to join with quads (sphere: all segments, capsule: one less), while
    /// <see cref="VerticalSegments"/> keeps feeding the split-line modulo.
    /// </summary>
    private readonly record struct WireframeGrid(int VerticalLoopCount, int VerticalSegments, int HorizontalSegments, int UvSplits, int UvSplitOffsetVertical);

    internal static (VertexPositionTexture[] Vertices, int[] Indices) GenerateSphere(float radius = 0.5f, int tessellations = 16, int uvSplits = 4, int uvSplitOffsetVertical = 0)
    {
        if (tessellations < 3) tessellations = 3;

        if (uvSplits != 0 && tessellations % uvSplits != 0) // FIXME: this can read a lot nicer i think?
        {
            throw new ArgumentException(DebugPrimitiveUv.SplitDivisorErrorMessage);
        }

        var verticalSegments = tessellations;
        var horizontalSegments = tessellations * 2;

        var grid = new WireframeGrid(verticalSegments, verticalSegments, horizontalSegments, uvSplits, uvSplitOffsetVertical);
        var extraVertexCount = CountExtraWireframeVertices(grid);

        var vertices = new VertexPositionTexture[(verticalSegments + 1) * (horizontalSegments + 1) + extraVertexCount];
        var indices = new int[verticalSegments * (horizontalSegments + 1) * 6];

        var vertexCount = 0;

        // generate the first extremity points
        for (var j = 0; j <= horizontalSegments; j++)
        {
            var normal = new Vector3(0, -1, 0);
            var textureCoordinate = new Vector2(0.5f);
            vertices[vertexCount++] = new VertexPositionTexture(normal * radius, textureCoordinate);
        }

        // Create rings of vertices at progressively higher latitudes.
        for (var i = 1; i < verticalSegments; i++)
        {
            var latitude = (float)(i * Math.PI / verticalSegments - Math.PI / 2.0);
            var dy = (float)Math.Sin(latitude);
            var dxz = (float)Math.Cos(latitude);

            // the first point
            var firstNormal = new Vector3(0, dy, dxz);
            var firstHorizontalVertex = new VertexPositionTexture(firstNormal * radius, DebugPrimitiveUv.NoLine);
            vertices[vertexCount++] = firstHorizontalVertex;

            // Create a single ring of vertices at this latitude.
            for (var j = 1; j < horizontalSegments; j++)
            {
                var longitude = (float)(j * 2.0 * Math.PI / horizontalSegments);
                var dx = (float)Math.Sin(longitude);
                var dz = (float)Math.Cos(longitude);

                dx *= dxz;
                dz *= dxz;

                var normal = new Vector3(dx, dy, dz);
                var textureCoordinate = DebugPrimitiveUv.NoLine;

                vertices[vertexCount++] = new VertexPositionTexture(normal * radius, textureCoordinate);
            }

            // the last point equal to the first point
            firstHorizontalVertex.TextureCoordinate = DebugPrimitiveUv.NoLine;
            vertices[vertexCount++] = firstHorizontalVertex;
        }

        // generate the end extremity points
        for (var j = 0; j <= horizontalSegments; j++)
        {
            var normal = new Vector3(0, 1, 0);
            var textureCoordinate = DebugPrimitiveUv.NoLine;
            vertices[vertexCount++] = new VertexPositionTexture(normal * radius, textureCoordinate);
        }

        // Fill the index buffer with triangles joining each pair of latitude rings.
        FillWireframeQuadStrip(vertices, indices, vertexCount, grid);

        return (vertices, indices);
    }

    internal static (VertexPositionTexture[] Vertices, int[] Indices) GenerateCapsule(float length, float radius, int tessellations, int uvSplits = 4)
    {
        if (tessellations < 3) tessellations = 3;

        if (uvSplits != 0 && tessellations % uvSplits != 0) // FIXME: this can read a lot nicer i think?
        {
            throw new ArgumentException(DebugPrimitiveUv.SplitDivisorErrorMessage);
        }

        var verticalSegments = 2 * tessellations;
        var horizontalSegments = 4 * tessellations;

        var grid = new WireframeGrid(verticalSegments - 1, verticalSegments, horizontalSegments, uvSplits, 0);
        var extraVertexCount = CountExtraWireframeVertices(grid);

        var vertices = new VertexPositionTexture[verticalSegments * (horizontalSegments + 1) + extraVertexCount];
        var indices = new int[(verticalSegments - 1) * (horizontalSegments + 1) * 6];

        var vertexCount = 0;
        // Create rings of vertices at progressively higher latitudes.
        for (var i = 0; i < verticalSegments; i++)
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
            for (var j = 0; j <= horizontalSegments; j++)
            {
                var longitude = (float)(j * 2.0 * Math.PI / horizontalSegments);
                var dx = (float)Math.Sin(longitude);
                var dz = (float)Math.Cos(longitude);

                dx *= dxz;
                dz *= dxz;

                var normal = new Vector3(dx, dy, dz);
                var textureCoordinate = DebugPrimitiveUv.NoLine;
                var position = radius * normal + new Vector3(0, deltaY, 0);

                vertices[vertexCount++] = new VertexPositionTexture(position, textureCoordinate);
            }
        }

        // Fill the index buffer with triangles joining each pair of latitude rings.
        FillWireframeQuadStrip(vertices, indices, vertexCount, grid);

        return (vertices, indices);
    }

    /// <summary>
    /// Counts the extra vertices the wireframe topology needs on top of the plain lat/long grid:
    /// 4 where a vertical and a horizontal split line cross, 2 where only one of them runs.
    /// </summary>
    // FIXME: i tried figuring out a closed form solution for this bugger here, but i feel like i'm missing something crucial...
    //  it basically is just here to calculate how many extra vertices are needed to create the wireframe topology we want
    // if *you* can figure out a closed form solution, have at it! you are very welcome!
    private static int CountExtraWireframeVertices(WireframeGrid grid)
    {
        var (verticalLoopCount, verticalSegments, horizontalSegments, uvSplits, uvSplitOffsetVertical) = grid;

        if (uvSplits <= 0)
        {
            return 0;
        }

        var extraVertexCount = 0;
        for (var i = 0; i < verticalLoopCount; i++)
        {
            for (var j = 0; j <= horizontalSegments; j++)
            {
                var vertModulo = (i + uvSplitOffsetVertical) % (verticalSegments / uvSplits);
                var horizModulo = j % (horizontalSegments / uvSplits);
                if (vertModulo == 0 && horizModulo == 0)
                {
                    extraVertexCount += 4;
                }
                else if (vertModulo == 0 || horizModulo == 0)
                {
                    extraVertexCount += 2;
                }
            }
        }

        return extraVertexCount;
    }

    /// <summary>
    /// Fills the index buffer with the two triangles of every lat/long quad, cloning vertices onto
    /// the wireframe uv where a split line runs so the shader can draw it. The clones are written
    /// starting at <paramref name="firstExtraVertex"/> (the count produced by
    /// <see cref="CountExtraWireframeVertices"/> reserves the space).
    /// </summary>
    private static void FillWireframeQuadStrip(VertexPositionTexture[] vertices, int[] indices, int firstExtraVertex, WireframeGrid grid)
    {
        var (verticalLoopCount, verticalSegments, horizontalSegments, uvSplits, uvSplitOffsetVertical) = grid;

        var stride = horizontalSegments + 1;
        var hasUvSplit = uvSplits > 0 ? 1 : 0;

        var indexCount = 0;
        var newVertexCount = firstExtraVertex;
        for (var i = 0; i < verticalLoopCount; i++)
        {
            for (var j = 0; j <= horizontalSegments; j++)
            {
                var nextI = i + 1;
                var nextJ = (j + 1) % stride;
                int? vertModulo = uvSplits > 0 ? (i + uvSplitOffsetVertical) % (verticalSegments / uvSplits) : null;
                int? horizModulo = uvSplits > 0 ? j % (horizontalSegments / uvSplits) : null;
                if (hasUvSplit > 0 && vertModulo == 0 && horizModulo == 0)
                {
                    vertices[newVertexCount] = vertices[i * stride + j];
                    vertices[newVertexCount].TextureCoordinate = DebugPrimitiveUv.Line;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (i * stride + j);

                    vertices[newVertexCount] = vertices[nextI * stride + j];
                    vertices[newVertexCount].TextureCoordinate = DebugPrimitiveUv.Line;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (nextI * stride + j);

                    indices[indexCount++] = i * stride + nextJ;

                    indices[indexCount++] = i * stride + nextJ;

                    vertices[newVertexCount] = vertices[nextI * stride + j];
                    vertices[newVertexCount].TextureCoordinate = DebugPrimitiveUv.Line;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (nextI * stride + j);

                    vertices[newVertexCount] = vertices[nextI * stride + nextJ];
                    vertices[newVertexCount].TextureCoordinate = DebugPrimitiveUv.Line;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (nextI * stride + nextJ);
                }
                else if (hasUvSplit > 0 && vertModulo == 0)
                {
                    indices[indexCount++] = i * stride + j;
                    indices[indexCount++] = nextI * stride + j;
                    indices[indexCount++] = i * stride + nextJ;

                    indices[indexCount++] = i * stride + nextJ;

                    vertices[newVertexCount] = vertices[nextI * stride + j];
                    vertices[newVertexCount].TextureCoordinate = DebugPrimitiveUv.Line;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (nextI * stride + j);

                    vertices[newVertexCount] = vertices[nextI * stride + nextJ];
                    vertices[newVertexCount].TextureCoordinate = DebugPrimitiveUv.Line;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (nextI * stride + nextJ);
                }
                else if (hasUvSplit > 0 && horizModulo == 0)
                {
                    vertices[newVertexCount] = vertices[i * stride + j];
                    vertices[newVertexCount].TextureCoordinate = DebugPrimitiveUv.Line;
                    indices[indexCount++] = newVertexCount++; // indices[indexCount++] = (i * stride + j);

                    vertices[newVertexCount] = vertices[nextI * stride + j];
                    vertices[newVertexCount].TextureCoordinate = DebugPrimitiveUv.Line;
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
    }
}
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
/// Generates the circle-based debug meshes: the circle itself, and the cylinder and cone that are
/// built out of circle caps. Split out of <see cref="ImmediateDebugPrimitives"/>, which remains the
/// public entry point.
/// </summary>
internal static class CircularDebugPrimitives
{
    internal static (VertexPositionTexture[] Vertices, int[] Indices) GenerateCircle(float radius = 0.5f, int tessellations = 16, int uvSplits = 0, float yOffset = 0.0f, bool isFlipped = false, int uvOffset = 0)
    {
        if (tessellations < 3) tessellations = 3;

        if (uvSplits != 0 && tessellations % uvSplits != 0) // FIXME: this can read a lot nicer i think?
        {
            throw new ArgumentException(DebugPrimitiveUv.SplitDivisorErrorMessage);
        }

        var hasUvSplits = uvSplits > 0 ? 1 : 0;
        var extraVertices = 0;
        var extraIndices = 0;

        if (hasUvSplits > 0)
        {
            for (var i = 0; i < tessellations * 3; i += 3)
            {
                var splitMod = (i / 3 - uvOffset) % (tessellations / uvSplits);
                var timeToSplit = splitMod == 0;
                if (timeToSplit)
                {
                    extraVertices += 2;
                    extraIndices += 3;
                }
            }
        }

        var vertices = new VertexPositionTexture[tessellations + 1 + hasUvSplits + extraVertices];
        var indices = new int[(tessellations + 1) * 3 + extraIndices];

        // center of our circle
        vertices[0].Position = new Vector3(0.0f, yOffset, 0.0f);
        vertices[0].TextureCoordinate = DebugPrimitiveUv.NoLine;

        // center, but with uv coords set
        if (hasUvSplits > 0)
        {
            vertices[1].Position = new Vector3(0.0f, yOffset, 0.0f);
            vertices[1].TextureCoordinate = DebugPrimitiveUv.Line;
        }

        var offset = 1 + hasUvSplits;
        for (var i = 0; i < tessellations; ++i)
        {
            var normal = ImmediateDebugPrimitives.GetCircleVector(i, tessellations);
            vertices[offset + i].Position = normal * radius + new Vector3(0.0f, yOffset, 0.0f);
            vertices[offset + i].TextureCoordinate = DebugPrimitiveUv.Line;
        }

        var curVert = tessellations + offset;
        var curIdx = (tessellations + 1) * 3;
        for (var i = 0; i < tessellations * 3; i += 3)
        {
            int? splitMod = uvSplits > 0 ? (i / 3 - uvOffset) % (tessellations / uvSplits) : null;
            var timeToSplit = splitMod == 0;
            if (timeToSplit)
            {
                indices[i] = 1;

                indices[i + 1] = curVert;
                vertices[curVert] = vertices[offset + i / 3 % tessellations];
                vertices[curVert++].TextureCoordinate = DebugPrimitiveUv.Line;

                indices[i + 2] = curVert;
                vertices[curVert] = vertices[offset + (i / 3 + 1) % tessellations];
                vertices[curVert++].TextureCoordinate = DebugPrimitiveUv.NoLine;

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

    internal static (VertexPositionTexture[] Vertices, int[] Indices) GenerateCylinder(float height = 1.0f, float radius = 0.5f, int tessellations = 16, int uvSplits = 4, int? uvSidesForCircle = null)
    {
        const int uvOffset = 3; // FIXME: this magic constant here is to get the splits to appear aesthetically similar orientation wise for all the shapes

        if (tessellations < 3) tessellations = 3;

        if (uvSplits != 0 && tessellations % uvSplits != 0) // FIXME: this can read a lot nicer i think?
        {
            throw new ArgumentException(DebugPrimitiveUv.SplitDivisorErrorMessage);
        }

        var (capVertices, capIndices) = GenerateCircle(radius, tessellations, uvSidesForCircle ?? uvSplits, uvOffset: 1 + uvOffset);

        var vertices = new VertexPositionTexture[capVertices.Length * 2 + tessellations * 4];
        var indices = new int[capIndices.Length * 2 + tessellations * 6];

        var bottomVertsOffset = vertices.Length - capVertices.Length;
        var topVertsOffset = vertices.Length - capVertices.Length * 2;
        var bottomIndicesOffset = indices.Length - capIndices.Length;
        var topIndicesOffset = indices.Length - capIndices.Length * 2;

        // copy vertices
        for (var i = 0; i < capVertices.Length; ++i)
        {
            vertices[bottomVertsOffset + i] = capVertices[i];
            vertices[bottomVertsOffset + i].Position.Y = -(height / 2.0f);
            vertices[topVertsOffset + i] = capVertices[i];
            vertices[topVertsOffset + i].Position.Y = height / 2.0f;
        }

        // copy indices
        for (var i = 0; i < capIndices.Length; ++i)
        {
            indices[bottomIndicesOffset + i] = capIndices[i] + bottomVertsOffset;
            indices[topIndicesOffset + i] = capIndices[i] + topVertsOffset;
        }

        // correct winding order so backface is inwards for bottom part
        Array.Reverse(indices, bottomIndicesOffset, capIndices.Length);

        // generate sides, using our top and bottom circle triangle fans
        var curVert = 0;
        var curIndex = 0;
        for (var i = 0; i < tessellations; ++i)
        {
            var normal = ImmediateDebugPrimitives.GetCircleVector(i, tessellations);
            var curTopPos = normal * radius + Vector3.UnitY * (height / 2.0f);
            var curBottomPos = normal * radius - Vector3.UnitY * (height / 2.0f);

            int? sideModulo = uvSplits > 0 ? (i + 1 - uvOffset) % (tessellations / uvSplits) : null;

            vertices[curVert].Position = curBottomPos;
            vertices[curVert].TextureCoordinate = sideModulo == 0 ? DebugPrimitiveUv.Line : DebugPrimitiveUv.NoLine;
            var ip = curVert++;

            var nextBottomNormal = ImmediateDebugPrimitives.GetCircleVector(i + 1, tessellations) * radius - Vector3.UnitY * (height / 2.0f);
            vertices[curVert].Position = nextBottomNormal;
            vertices[curVert].TextureCoordinate = DebugPrimitiveUv.NoLine;
            var ip1 = curVert++;

            vertices[curVert].Position = curTopPos;
            vertices[curVert].TextureCoordinate = sideModulo == 0 ? DebugPrimitiveUv.Line : DebugPrimitiveUv.NoLine;
            var ipv = curVert++;

            var nextTopNormal = ImmediateDebugPrimitives.GetCircleVector(i + 1, tessellations) * radius + Vector3.UnitY * (height / 2.0f);
            vertices[curVert].Position = nextTopNormal;
            vertices[curVert].TextureCoordinate = DebugPrimitiveUv.NoLine;
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

    internal static (VertexPositionTexture[] Vertices, int[] Indices) GenerateCone(float height, float radius, int tessellations, int uvSplits = 4, int uvSplitsBottom = 0)
    {
        if (tessellations < 3) tessellations = 3;

        if (uvSplits != 0 && tessellations % uvSplits != 0) // FIXME: this can read a lot nicer i think?
        {
            throw new ArgumentException(DebugPrimitiveUv.SplitDivisorErrorMessage);
        }

        if (uvSplitsBottom != 0 && tessellations % uvSplitsBottom != 0) // FIXME: this can read a lot nicer i think?
        {
            throw new ArgumentException("expected the desired number of uv splits for the bottom to be a divisor of the number of tessellations");
        }

        var (bottomVertices, bottomIndices) = GenerateCircle(radius, tessellations, uvSplits, yOffset: -(height / 2.0f));
        var (topVertices, topIndices) = GenerateCircle(radius, tessellations, uvSplitsBottom, isFlipped: true, yOffset: -(height / 2.0f));
        var vertices = new VertexPositionTexture[bottomVertices.Length + topVertices.Length];
        var indices = new int[topIndices.Length + bottomIndices.Length];

        // The top circle follows the bottom one, its indices shifted past the bottom's vertices
        bottomVertices.CopyTo(vertices, 0);
        topVertices.CopyTo(vertices, bottomVertices.Length);
        bottomIndices.CopyTo(indices, 0);

        for (var i = 0; i < topIndices.Length; ++i)
        {
            indices[i + bottomIndices.Length] = topIndices[i] + bottomVertices.Length;
        }

        // extrude middle vertex of center of first circle triangle fan
        vertices[0].Position.Y = height / 2.0f;
        vertices[1].Position.Y = height / 2.0f;

        return (vertices, indices);
    }
}
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Physics;
using Stride.Rendering;

namespace Stride.CommunityToolkit.Bullet;

/// <summary>
/// Extension helpers for working with <see cref="Heightmap"/> data: sampling heights and normals, ray picking,
/// exporting raw height values, generating preview textures and building renderable / collision meshes.
/// </summary>
/// <remarks>
/// Typical usage:
/// <code>
/// // Sample a height and normal
/// var h = heightmap.GetHeightAt(x, z);
/// var n = heightmap.GetNormal(x, z);
///
/// // Build a mesh (tessellating every 2nd sample)
/// var mesh = heightmap.ToMesh(graphicsDevice, 1f, 1f, 4f, out var terrainPoints, tessellation: 2);
///
/// // Create a grayscale preview texture
/// var preview = heightmap.ToTexture(graphicsDevice, commandList);
/// </code>
/// </remarks>
public static class HeightmapExtensions
{
    /// <summary>
    /// Divisor applied to each stored sample before it is mapped into <see cref="Heightmap.HeightRange"/>;
    /// 255 suits 8-bit greyscale heightmaps.
    /// </summary>
    public const float HeightMultiplier = 255.0f;

    // The vertex colour of a terrain mesh, an earthy brown
    private static readonly Vector4 TerrainColor = new(149, 135, 118, 255);

    /// <summary>
    /// Tests a ray against each sampled height point (approximated as small spheres) and returns the closest intersection.
    /// </summary>
    /// <param name="heightmap">Source heightmap.</param>
    /// <param name="ray">Ray in heightmap local space.</param>
    /// <param name="point">Closest intersection point if any.</param>
    /// <param name="quadWidthX">Horizontal spacing between height samples in world units (X axis).</param>
    /// <param name="quadWidthZ">Horizontal spacing between height samples in world units (Z axis).</param>
    /// <returns><c>true</c> if the ray intersects at least one sample; otherwise <c>false</c>.</returns>
    public static bool IntersectsRay(this Heightmap heightmap, Ray ray, out Vector3 point, float quadWidthX = 1.0f, float quadWidthZ = 1.0f)
    {
        var sphere = new BoundingSphere(Vector3.Zero, 1.5f);
        var nearest = float.MaxValue;
        var found = false;

        point = Vector3.Zero;

        for (var z = 0; z < heightmap.Size.Y; z++)
        {
            for (var x = 0; x < heightmap.Size.X; x++)
            {
                sphere.Center = new Vector3(x * quadWidthX, heightmap.GetHeightAt(x, z), z * quadWidthZ);

                if (!sphere.Intersects(ref ray, out Vector3 hit)) continue;

                // The nearest hit, not the first
                var distance = Vector3.Distance(hit, ray.Position);

                if (distance < nearest)
                {
                    nearest = distance;
                    point = hit;
                    found = true;
                }
            }
        }

        return found;
    }

    /// <summary>
    /// Returns the height of sample (x, y) mapped into <see cref="Heightmap.HeightRange"/>, or <c>HeightRange.X</c> when out of range.
    /// </summary>
    public static float GetHeightAt(this Heightmap heightmap, int x, int y)
    {
        if (!IsValidCoordinate(heightmap, x, y)) return heightmap.HeightRange.X;

        var heightData = heightmap.Shorts[GetHeightIndex(heightmap, x, y)] / HeightMultiplier;

        return heightmap.HeightRange.X + (heightmap.HeightRange.Y - heightmap.HeightRange.X) * heightData;
    }

    /// <summary>
    /// Checks whether the provided coordinate lies within the heightmap sample bounds.
    /// </summary>
    public static bool IsValidCoordinate(this Heightmap heightmap, int x, int y)
        => x >= 0 && x < heightmap.Size.X && y >= 0 && y < heightmap.Size.Y;

    /// <summary>
    /// Converts a 2D sample coordinate into the flat array index of <see cref="Heightmap.Shorts"/>.
    /// </summary>
    public static int GetHeightIndex(this Heightmap heightmap, int x, int y)
        => y * heightmap.Size.X + x;

    /// <summary>
    /// Estimates the vertex tangent direction at the given sample.
    /// </summary>
    public static Vector3 GetTangent(this Heightmap heightmap, int x, int z)
    {
        var here = new Vector3(x, GetHeightAt(heightmap, x, z), z);
        var left = new Vector3(x - 1, GetHeightAt(heightmap, x - 1, z), z);
        var flip = 1;

        if (left.X < 0.0f)
        {
            flip = -1;
            left = new Vector3(x + 1, GetHeightAt(heightmap, x + 1, z), z);
        }

        return Vector3.Normalize((left - here) * flip);
    }

    /// <summary>
    /// Estimates a normal vector using central differences at the given sample coordinate.
    /// </summary>
    public static Vector3 GetNormal(this Heightmap heightmap, int x, int y)
    {
        var heightL = GetHeightAt(heightmap, x - 1, y);
        var heightR = GetHeightAt(heightmap, x + 1, y);
        var heightD = GetHeightAt(heightmap, x, y - 1);
        var heightU = GetHeightAt(heightmap, x, y + 1);

        return Vector3.Normalize(new Vector3(heightL - heightR, 2.0f, heightD - heightU));
    }

    /// <summary>
    /// Copies raw height values (as floats) into a new array for processing/export.
    /// </summary>
    public static float[] ToFloats(this Heightmap heightmap)
    {
        var heightValues = new float[heightmap.Size.X * heightmap.Size.Y];

        for (var i = 0; i < heightValues.Length; i++)
        {
            heightValues[i] = heightmap.Shorts[i];
        }

        return heightValues;
    }

    /// <summary>
    /// Generates a grayscale texture representation of the heightmap where each texel encodes height in its color channels.
    /// Higher height values map to brighter colors.
    /// </summary>
    /// <param name="heightmap">Source heightmap.</param>
    /// <param name="graphicsDevice">Graphics device used to allocate the texture.</param>
    /// <param name="commandList">Command list used to upload texture data.</param>
    /// <returns>A newly created 2D texture containing the height preview.</returns>
    public static Texture ToTexture(this Heightmap heightmap, GraphicsDevice graphicsDevice, CommandList commandList)
    {
        var width = heightmap.Size.X;
        var height = heightmap.Size.Y;
        var texture = Texture.New2D(graphicsDevice, width, height, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource, 1, GraphicsResourceUsage.Dynamic);
        var heightValues = new Color[width * height];

        for (var i = 0; i < heightValues.Length; i++)
        {
            heightValues[i] = heightmap.Shorts[i].AsStrideColor();
        }

        texture.SetData(commandList, heightValues);

        return texture;
    }

    /// <summary>
    /// Builds a triangle mesh for the heightmap using the specified quad spacing, texture repetition and tessellation stride.
    /// </summary>
    /// <param name="heightmap">Source heightmap.</param>
    /// <param name="graphicsDevice">Graphics device used for buffer allocation.</param>
    /// <param name="quadWidthX">Width of a single height sample quad along X.</param>
    /// <param name="quadWidthZ">Width of a single height sample quad along Z.</param>
    /// <param name="textureRepeat">If &gt; 0, number of times the texture repeats across the terrain extents.</param>
    /// <param name="terrainPoints">Output array of vertex positions.</param>
    /// <param name="tessellation">Stride between samples (1 = full resolution).</param>
    /// <returns>Constructed mesh.</returns>
    public static Mesh ToMesh(this Heightmap heightmap, GraphicsDevice graphicsDevice, float quadWidthX, float quadWidthZ, float textureRepeat, out Vector3[] terrainPoints, int tessellation)
    {
        var quadsX = (heightmap.Size.X - 1) / tessellation;
        var quadsZ = (heightmap.Size.Y - 1) / tessellation;
        var vertsX = quadsX + 1;
        var vertsZ = quadsZ + 1;
        var vertexCount = vertsX * vertsZ;

        // The terrain starts at the origin and spans one quad width per sample
        var minBounds = Vector3.Zero;
        var maxBounds = new Vector3((heightmap.Size.X - 1) * quadWidthX, 0, (heightmap.Size.Y - 1) * quadWidthZ);
        var stepX = tessellation * (maxBounds.X - minBounds.X) / (heightmap.Size.X - 1);
        var stepZ = tessellation * (maxBounds.Z - minBounds.Z) / (heightmap.Size.Y - 1);

        var vertices = new VertexTypePosTexNormColor[vertexCount];
        var position = new Vector3(minBounds.X, 0, minBounds.Z);
        var count = 0;

        terrainPoints = new Vector3[vertexCount];

        for (var z = 0; z < vertsZ; z++)
        {
            position.X = minBounds.X;

            for (var x = 0; x < vertsX; x++)
            {
                // The vertex at (x, z) stands on the sample tessellation steps away; sampling (x, z) itself
                // would stretch the heights of one corner over the whole terrain
                var vertexPosition = new Vector3(position.X, heightmap.GetHeightAt(x * tessellation, z * tessellation), position.Z);

                terrainPoints[count] = vertexPosition;

                // textureRepeat > 0 tiles the texture over the whole terrain; otherwise each quad gets one copy.
                var texCoord = textureRepeat > 0
                    ? new Vector2(quadWidthX * textureRepeat * x / (float)vertsX * tessellation, quadWidthZ * textureRepeat * (z * 1.0f) / (float)vertsZ * tessellation)
                    : new Vector2(quadWidthX * x * tessellation, quadWidthZ * z * tessellation);

                vertices[count] = new VertexTypePosTexNormColor(vertexPosition, heightmap.GetNormal(x * tessellation, z * tessellation), heightmap.GetTangent(x * tessellation, z * tessellation), texCoord, TerrainColor);
                position.X += stepX;
                count++;
            }

            position.Z += stepZ;
        }

        var indices = new int[vertexCount * 6];

        count = 0;

        for (var z = 0; z < quadsZ; z++)
        {
            for (var x = 0; x < quadsX; x++)
            {
                var vbase = vertsX * z + x;

                indices[count++] = vbase + 1;
                indices[count++] = vbase + 1 + vertsX;
                indices[count++] = vbase + vertsX;
                indices[count++] = vbase + 1;
                indices[count++] = vbase + vertsX;
                indices[count++] = vbase;
            }
        }

        var vertexBuffer = Stride.Graphics.Buffer.Vertex.New(graphicsDevice, vertices, GraphicsResourceUsage.Dynamic);
        var indexBuffer = Stride.Graphics.Buffer.Index.New(graphicsDevice, indices);

        return new Mesh
        {
            Draw = new MeshDraw
            {
                PrimitiveType = PrimitiveType.TriangleList,
                DrawCount = indices.Length,
                IndexBuffer = new IndexBufferBinding(indexBuffer, true, indices.Length),
                VertexBuffers = [new VertexBufferBinding(vertexBuffer, VertexTypePosTexNormColor.Layout, vertexBuffer.ElementCount)],
            },
            BoundingBox = BoundingBox.FromPoints(terrainPoints),
            BoundingSphere = BoundingSphere.FromPoints(terrainPoints)
        };
    }

    /// <summary>
    /// Converts all sample heights to world-space points using the provided quad spacing.
    /// </summary>
    public static Vector3[] ToWorldPoints(this Heightmap heightmap, float quadWidthX, float quadWidthZ)
    {
        var vertsX = heightmap.Size.X;
        var vertsZ = heightmap.Size.Y;
        var points = new Vector3[vertsX * vertsZ];
        var count = 0;

        for (var z = 0; z < vertsZ; z++)
        {
            for (var x = 0; x < vertsX; x++)
            {
                points[count++] = new Vector3(x * quadWidthX, heightmap.GetHeightAt(x, z), z * quadWidthZ);
            }
        }

        return points;
    }

    /// <summary>
    /// Converts a signed 16-bit height value to a packed <see cref="Color"/> using a float-to-RGBA conversion.
    /// </summary>
    /// <remarks>
    /// The value is widened to a float and its four bytes become the four channels, least significant
    /// byte in red. That is the same reinterpretation a packed-float heightmap texture uses, so a
    /// texture built from these colours can be read back the same way.
    /// </remarks>
    public static Color AsStrideColor(this short val)
    {
        var bits = BitConverter.SingleToInt32Bits(val);

        return new Color((byte)bits, (byte)(bits >> 8), (byte)(bits >> 16), (byte)(bits >> 24));
    }
}
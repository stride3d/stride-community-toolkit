using Stride.Graphics;
using Stride.Physics;
using Stride.Rendering;

namespace Stride.CommunityToolkit.Physics;

public static partial class HeightmapExtensions
{
    /// <summary>
    /// Used to distinguish between Grey scale heightmaps HeightMultiplier=255.0f (yields a byte 0-255)
    /// or float heightmaps HeightMultiplier=10000.0f (based on a short -32,768 to 32,767,
    /// sum yields 65,535 levels for much smoother maps).
    /// </summary>
    public const float HeightMultiplier = 255.0f;

    //ToDo: Needs refactoring
    //Example of picking a Heightmap, and its extension
    public static bool IntersectsRay(this Heightmap heightmap, Ray ray, out Vector3 point, float m_QuadSideWidthX = 1.0f, float m_QuadSideWidthZ = 1.0f)
    {
        BoundingSphere sphere = new BoundingSphere(Vector3.Zero, 1.5f);
        int x, z;
        float mindist = 1000000000.0f;
        point = Vector3.Zero;
        bool foundit = false;
        for (z = 0; z < heightmap.Size.Y; z++)
        {
            for (x = 0; x < heightmap.Size.X; x++)
            {
                sphere.Center = new Vector3(x * m_QuadSideWidthX, heightmap.GetHeightAt(x, z), z * m_QuadSideWidthZ);

                if (sphere.Intersects(ref ray, out Vector3 pt))
                {
                    //get nearest hit
                    float dist = Vector3.Distance(pt, ray.Position);
                    if (dist < mindist)
                    {
                        mindist = dist;
                        point = pt;
                        foundit = true;
                    }
                    //return true;//gets the first hit, replace out Vector3 pt with out point and comment the above
                }
            }
        }
        return foundit;
    }

    public static float GetHeightAt(this Heightmap heightmap, int x, int y)
    {
        if (!IsValidCoordinate(heightmap, x, y))
        {
            return heightmap.HeightRange.X;
        }

        var index = GetHeightIndex(heightmap, x, y);
        var heightData = 1.0f * heightmap.Shorts[index] / HeightMultiplier;
        var height = heightmap.HeightRange.X + (heightmap.HeightRange.Y -
            heightmap.HeightRange.X) * heightData;
        return height;
    }

    public static bool IsValidCoordinate(this Heightmap heightmap, int x, int y)
        => x >= 0 && x < heightmap.Size.X && y >= 0 && y < heightmap.Size.Y;

    public static int GetHeightIndex(this Heightmap heightmap, int x, int y)
        => y * heightmap.Size.X + x;

    public static Vector3 GetTangent(this Heightmap heightmap, int x, int z)
    {
        var flip = 1;
        var here = new Vector3(x, GetHeightAt(heightmap, x, z), z);
        var left = new Vector3(x - 1, GetHeightAt(heightmap, x - 1, z), z);
        if (left.X < 0.0f)
        {
            flip *= -1;
            left = new Vector3(x + 1, GetHeightAt(heightmap, x + 1, z), z);
        }

        left -= here;

        var tangent = left * flip;
        tangent.Normalize();

        return tangent;
    }

    public static Vector3 GetNormal(this Heightmap heightmap, int x, int y)
    {
        var heightL = GetHeightAt(heightmap, x - 1, y);
        var heightR = GetHeightAt(heightmap, x + 1, y);
        var heightD = GetHeightAt(heightmap, x, y - 1);
        var heightU = GetHeightAt(heightmap, x, y + 1);

        var normal = new Vector3(heightL - heightR, 2.0f, heightD - heightU);
        normal.Normalize();
        return normal;
    }

    public static float[] ToFloats(this Heightmap heightmap)
    {
        int i, j, index, m_Width = heightmap.Size.X, m_Height = heightmap.Size.Y;
        float[] heightValues = new float[m_Width * m_Height];
        for (i = 0; i < m_Width; i++)
        {
            for (j = 0; j < m_Height; j++)
            {
                index = (m_Width * j) + i;
                heightValues[index] = heightmap.Shorts[index];
            }
        }
        return heightValues;
    }

    public static Texture ToTexture(this Heightmap heightmap, GraphicsDevice graphicsDevice, CommandList commandList)
    {
        int i, j, index, m_Width = heightmap.Size.X, m_Height = heightmap.Size.Y;
        Texture tex = Texture.New2D(graphicsDevice, m_Width,
            m_Height, PixelFormat.R8G8B8A8_UNorm,
            TextureFlags.ShaderResource, 1, GraphicsResourceUsage.Dynamic);
        Color[] heightValues = new Color[m_Width * m_Height];
        // Get the height information and put it in the array
        for (i = 0; i < m_Width; i++)
        {
            for (j = 0; j < m_Height; j++)
            {
                index = (m_Width * j) + i;
                heightValues[index] = heightmap.Shorts[index].AsStrideColor();
            }
        }
        tex.SetData(commandList, heightValues);
        return tex;
    }

    /// <summary>
    /// Creates the terrain mesh from a given heightmap. Tessellation divides
    /// the quad numbers.
    /// </summary>
    /// <param name="heightmap"></param>
    /// <param name="graphicsDevice"></param>
    /// <param name="m_QuadSideWidthX"></param>
    /// <param name="m_QuadSideWidthZ"></param>
    /// <param name="TEXTURE_REPEAT"></param>
    /// <param name="terrainPoints"></param>
    /// <param name="tessellation"></param>
    /// <returns></returns>
    public static Mesh ToMesh(this Heightmap heightmap,
        GraphicsDevice graphicsDevice,
        float m_QuadSideWidthX, float m_QuadSideWidthZ, float TEXTURE_REPEAT,
        out Vector3[] terrainPoints, int tessellation)
    {
        Vector3 minBounds = Vector3.Zero;
        int m_num_quads_z = (heightmap.Size.Y - 1) / tessellation,
            m_num_quads_x = (heightmap.Size.X - 1) / tessellation;
        Vector3 maxBounds = new Vector3((heightmap.Size.X - 1)
            * m_QuadSideWidthX, 0,
            (heightmap.Size.Y - 1)
            * m_QuadSideWidthZ);
        Vector3 center = 0.5f * (minBounds + maxBounds);
        int numVertsX = m_num_quads_x + 1;
        int numVertsZ = m_num_quads_z + 1;
        float stepX = tessellation * (maxBounds.X - minBounds.X) / (heightmap.Size.X - 1);
        float stepZ = tessellation * (maxBounds.Z - minBounds.Z) / (heightmap.Size.Y - 1);
        int count = 0, x, z, m_vertexCount = numVertsX * numVertsZ;
        Vector3 pos = new Vector3(minBounds.X, 0, minBounds.Z);
        byte R = 149, G = 135, B = 118;
        // Create the vertex array.
        VertexTypePosTexNormColor[] m_vertices = new VertexTypePosTexNormColor[m_vertexCount];
        terrainPoints = new Vector3[m_vertexCount];

        // Vector3[] Normals = heightmap.CalculateNormals();
        // Initialize the index to the vertex buffer.
        for (z = 0; z < numVertsZ; z++)
        {
            pos.X = minBounds.X;
            for (x = 0; x < numVertsX; x++)
            {
                m_vertices[count].Position = new Vector3(pos.X,
                    heightmap.GetHeightAt(x, z), pos.Z);
                terrainPoints[count] = m_vertices[count].Position;
                if (TEXTURE_REPEAT > 0)//whole terrain has the texture repeatedly
                {
                    m_vertices[count].TexCoord.X = m_QuadSideWidthX * TEXTURE_REPEAT * x / (float)numVertsX * tessellation;
                    m_vertices[count].TexCoord.Y = m_QuadSideWidthZ * TEXTURE_REPEAT * (z * 1.0f) / (float)numVertsZ * tessellation;
                }
                else //make each quad have the texture
                {
                    m_vertices[count].TexCoord.X = m_QuadSideWidthX * x * tessellation;
                    m_vertices[count].TexCoord.Y = m_QuadSideWidthZ * z * tessellation;
                }
                m_vertices[count].Normal = heightmap.GetNormal(x, z);
                m_vertices[count].Tangent = heightmap.GetTangent(x, z);
                m_vertices[count].Color = new Vector4(R, G, B, 255.0f);
                pos.X += stepX;
                count++;
            }
            // Increment Z
            pos.Z += stepZ;
        }

        int[] indices = new int[m_vertexCount * 6];
        count = 0;
        for (z = 0; z < m_num_quads_z; z++)
        {
            for (x = 0; x < m_num_quads_x; x++)
            {
                var vbase = numVertsX * z + x;
                indices[count++] = (vbase + 1);
                indices[count++] = (vbase + 1 + numVertsX);
                indices[count++] = (vbase + numVertsX);
                indices[count++] = (vbase + 1);
                indices[count++] = (vbase + numVertsX);
                indices[count++] = (vbase);
            }
        }

        var vertexBuffer = Stride.Graphics.Buffer.Vertex.New(graphicsDevice, m_vertices, GraphicsResourceUsage.Dynamic);
        var indexBuffer = Stride.Graphics.Buffer.Index.New(graphicsDevice, indices);

        return new Mesh
        {
            Draw = new MeshDraw
            {
                PrimitiveType = PrimitiveType.TriangleList,
                DrawCount = indices.Length,
                IndexBuffer = new IndexBufferBinding(indexBuffer, true, indices.Length),
                VertexBuffers = new[] { new VertexBufferBinding(vertexBuffer, VertexTypePosTexNormColor.Layout, vertexBuffer.ElementCount) },
            },
            BoundingBox = BoundingBox.FromPoints(terrainPoints),
            BoundingSphere = BoundingSphere.FromPoints(terrainPoints)
        };
    }

    public static Vector3[] ToWorldPoints(this Heightmap heightmap,
        float m_QuadSideWidthX, float m_QuadSideWidthZ)
    {
        Vector3 minBounds = Vector3.Zero;
        int m_num_quads_z = heightmap.Size.Y - 1, m_num_quads_x = heightmap.Size.X - 1;
        Vector3 maxBounds = new Vector3(m_num_quads_x * m_QuadSideWidthX, 0,
            m_num_quads_z * m_QuadSideWidthZ);
        Vector3 center = 0.5f * (minBounds + maxBounds);
        int numVertsX = m_num_quads_x + 1;
        int numVertsZ = m_num_quads_z + 1;
        float stepX = (maxBounds.X - minBounds.X) / m_num_quads_x;
        float stepZ = (maxBounds.Z - minBounds.Z) / m_num_quads_z;
        int count = 0, x, z, m_vertexCount = numVertsX * numVertsZ;
        Vector3 pos = new Vector3(minBounds.X, 0, minBounds.Z);
        Vector3[] points = new Vector3[m_vertexCount];

        for (z = 0; z < numVertsZ; z++)
        {
            pos.X = minBounds.X;
            for (x = 0; x < numVertsX; x++)
            {
                points[count] = new Vector3(pos.X,
                    heightmap.GetHeightAt(x, z), pos.Z);
                pos.X += stepX;
                count++;
            }
            // Increment Z
            pos.Z += stepZ;
        }

        return points;
    }

    public static Color AsStrideColor(this short val)
    {
        FloatRGBAConverter converter = new FloatRGBAConverter((float)val);
        return new Color(converter.R, converter.G,
             converter.B, converter.A);
    }
}
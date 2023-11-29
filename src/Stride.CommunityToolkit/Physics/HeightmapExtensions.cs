using Stride.Physics;

namespace Stride.CommunityToolkit.Physics;

public static class HeightmapExtensions
{
    public const float HeightMultiplier = 255.0f;

    //ToDo: Needs refactoring
    //Example of picking a Heightmap, and its extension
    public static bool IntersectsRay(this Heightmap heightmap,
        Ray ray, out Vector3 point, float m_QuadSideWidthX = 1.0f,
        float m_QuadSideWidthZ = 1.0f)
    {
        //point = ray.Position;
        //check each quad
        // int quadnumx = (heightmap.Size.X - 1), quadnumz = (heightmap.Size.Y - 1);
        BoundingSphere sphere = new BoundingSphere(Vector3.Zero, 1.5f);
        int x, z;
        float mindist = 1000000000.0f;
        point = Vector3.Zero;
        bool foundit = false;
        for (z = 0; z < heightmap.Size.Y; z++)
        {
            for (x = 0; x < heightmap.Size.X; x++)
            {
                // ToDo Get GetHeightAt
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
        if (!IsValidCoordinate(heightmap, x, y))// || x == 0 || y == 0 || x == heightmap.Size.X - 1 || y == heightmap.Size.Y - 1)
        {
            return heightmap.HeightRange.X;// - heightmap.HeightRange.Y;
        }

        var index = GetHeightIndex(heightmap, x, y);
        var heightData = 1.0f * heightmap.Shorts[index] / HeightMultiplier;/// 255.0f;// (1.0f*short.MaxValue);//
        //  (heightmap.Shorts[index]-short.MinValue)/ (1.0f*short.MaxValue - 1.0f * short.MinValue);

        // var height = HeightmapUtils.ConvertToFloatHeight(short.MinValue, short.MaxValue, heightData);

        // height *= heightmap.HeightRange.Y;
        var height = heightmap.HeightRange.X + (heightmap.HeightRange.Y -
            heightmap.HeightRange.X) * heightData;// HeightmapUtils.ConvertToShortHeight(                        short.MinValue, short.MaxValue,
                                                  //  MathUtil.Clamp(Utility.RGBAToFloat(heightValues[index]),
                                                  //  Heightmap.HeightRange.X, Heightmap.HeightRange.Y))
        return height;
    }

    public static bool IsValidCoordinate(this Heightmap heightmap, int x, int y)
        => x >= 0 && x < heightmap.Size.X && y >= 0 && y < heightmap.Size.Y;

    public static int GetHeightIndex(this Heightmap heightmap, int x, int y)
        => y * heightmap.Size.X + x;
}
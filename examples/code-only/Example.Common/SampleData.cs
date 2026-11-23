namespace Example.Common;

/// <summary>Made-up data for the examples that plot some.</summary>
public static class SampleData
{
    /// <summary>
    /// Noisy measurements around 2 sin(x), for x from -4.5 to 4.5: the classic "data points versus fitted
    /// curve" picture. The jitter is a hash of x, so the points are the same every run without a Random.
    /// </summary>
    /// <param name="z">The plane the points lie on.</param>
    public static List<Vector3> ScatterAroundSine(float z = 0f)
    {
        var samples = new List<Vector3>();

        for (var x = -4.5f; x <= 4.5f; x += 0.45f)
        {
            var hash = MathF.Sin(x * 12.9898f) * 43758.5453f;
            var jitter = (hash - MathF.Floor(hash) - 0.5f) * 0.7f;

            samples.Add(new Vector3(x, 2f * MathF.Sin(x) + jitter, z));
        }

        return samples;
    }
}
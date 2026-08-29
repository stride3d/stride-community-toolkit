using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Rendering.Lines;

/// <summary>
/// Turns functions into point lists for <see cref="PolylineMeshBuilder"/>.
/// </summary>
public static class PolylineSampling
{
    /// <summary>
    /// Samples <c>y = f(x)</c> at evenly spaced <c>x</c> values, producing points in the XY plane.
    /// </summary>
    /// <param name="f">The function to plot.</param>
    /// <param name="from">The first <c>x</c>.</param>
    /// <param name="to">The last <c>x</c>.</param>
    /// <param name="samples">The number of points; more gives a smoother curve. At least two.</param>
    /// <returns><paramref name="samples"/> points with <c>z = 0</c>.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="f"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="samples"/> is less than two.</exception>
    public static Vector3[] Function(Func<float, float> f, float from, float to, int samples = 200)
    {
        ArgumentNullException.ThrowIfNull(f);
        ArgumentOutOfRangeException.ThrowIfLessThan(samples, 2);

        var points = new Vector3[samples];
        var step = (to - from) / (samples - 1);

        for (var i = 0; i < samples; i++)
        {
            var x = from + i * step;
            points[i] = new Vector3(x, f(x), 0f);
        }

        return points;
    }

    /// <summary>
    /// Samples a parametric curve <c>p(t)</c> at evenly spaced <c>t</c> values.
    /// </summary>
    /// <param name="p">The curve.</param>
    /// <param name="from">The first <c>t</c>.</param>
    /// <param name="to">The last <c>t</c>.</param>
    /// <param name="samples">The number of points. At least two.</param>
    /// <returns><paramref name="samples"/> points.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="p"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="samples"/> is less than two.</exception>
    public static Vector3[] Parametric(Func<float, Vector3> p, float from, float to, int samples = 200)
    {
        ArgumentNullException.ThrowIfNull(p);
        ArgumentOutOfRangeException.ThrowIfLessThan(samples, 2);

        var points = new Vector3[samples];
        var step = (to - from) / (samples - 1);

        for (var i = 0; i < samples; i++)
        {
            points[i] = p(from + i * step);
        }

        return points;
    }
}

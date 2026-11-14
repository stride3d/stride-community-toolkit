using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Scripts;
using Stride.Input;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// The key names the overlay and the key menus print, which are cached for the process, and the zoom step the
/// camera controllers share.
/// </summary>
public class KeyNamesTests
{
    [Theory]
    [InlineData(Keys.D2, "2")]
    [InlineData(Keys.NumPad7, "7")]
    [InlineData(Keys.F3, "F3")]
    [InlineData(Keys.Space, "Space")]
    public void KeyNames_ShowDigitsBare_AndEveryOtherKeyByName(Keys key, string expected)
    {
        Assert.Equal(expected, KeyNames.Describe(key));

        // The second ask is the cached one
        Assert.Equal(expected, KeyNames.Describe(key));
    }

    [Fact]
    public void KeyNames_AskedFromManyThreadsAtOnce_AgreeAndDoNotThrow()
    {
        var keys = Enum.GetValues<Keys>();

        Parallel.For(0, 64, _ =>
        {
            foreach (var key in keys) Assert.False(string.IsNullOrEmpty(KeyNames.Describe(key)));
        });
    }

    [Theory]
    [InlineData(10f, 1f, 9.0909f)]   // one notch in: divided by 1.1
    [InlineData(10f, -1f, 11f)]      // one notch out: multiplied by 1.1
    [InlineData(10f, 0f, 10f)]
    [InlineData(10f, 100f, 2f)]      // clamped to the minimum
    [InlineData(10f, -100f, 50f)]    // clamped to the maximum
    public void StepZoom_IsMultiplicativePerNotch_AndClamped(float value, float wheel, float expected)
        => Assert.Equal(expected, CameraMath.StepZoom(value, wheel, step: 0.1f, min: 2f, max: 50f), 3);
}
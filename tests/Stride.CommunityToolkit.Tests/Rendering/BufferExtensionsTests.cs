using Stride.CommunityToolkit.Graphics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// The pinned upload needs a graphics device to do its work, which a unit test has none of. What
/// can be held here is its contract at the edges; the gold images cover the upload itself, since
/// every shape they draw goes through it.
/// </summary>
public class BufferExtensionsTests
{
    [Fact]
    public void SetDataPinnedRejectsAMissingBuffer()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => BufferExtensions.SetDataPinned<int>(null!, null!, [1, 2, 3]));

        Assert.Equal("buffer", exception.ParamName);
    }
}
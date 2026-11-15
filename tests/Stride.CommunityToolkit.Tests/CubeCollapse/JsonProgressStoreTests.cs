using CubeCollapse.Gameplay;
using Xunit;

namespace Stride.CommunityToolkit.Tests.CubeCollapse;

/// <summary>
/// Covers the progress file: a level saved is the level loaded, and a missing or corrupt file is a fresh start.
/// </summary>
public class JsonProgressStoreTests
{
    [Fact]
    public void JsonProgressStoreRoundTrips()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cube-collapse-progress-{Guid.NewGuid():N}.json");
        var store = new JsonProgressStore(path);

        try
        {
            // A missing file is a fresh start, not an error
            Assert.Equal(1, store.Load().Level);

            store.Save(new GameProgress { Level = 7 });

            Assert.Equal(7, store.Load().Level);

            // A corrupt file is also a fresh start
            File.WriteAllText(path, "not json at all");

            Assert.Equal(1, store.Load().Level);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
using Example_CubicleCalamity.Gameplay;
using Example_CubicleCalamity.Shared;
using Xunit;

namespace Stride.CommunityToolkit.Tests.CubicleCalamity;

/// <summary>
/// Covers how the board grows from level to level, and the shape rules the palettes must obey.
/// </summary>
public class LevelRulesTests
{
    [Fact]
    public void LevelOneIsTheStartingBoard()
    {
        var level = LevelRules.ForNumber(1);

        Assert.Equal(1, level.Number);
        Assert.Equal(LevelRules.StartingSize, level.Rows);
        Assert.Equal(LevelRules.StartingSize, level.Layers);
    }

    [Fact]
    public void EachLevelGrowsBySideByOne()
    {
        var previous = LevelRules.ForNumber(1);
        var next = LevelRules.ForNumber(2);

        Assert.Equal(previous.Rows + 1, next.Rows);
        Assert.Equal(previous.Layers + 1, next.Layers);
    }

    [Fact]
    public void GrowthStopsAtTheFullBoard()
    {
        // The cap is where the physics tuning lives - see the solver substep notes - so a level
        // past it must not keep growing
        var atCap = LevelRules.ForNumber(GameSettings.Rows - LevelRules.StartingSize + 1);
        var beyond = LevelRules.ForNumber(100);

        Assert.Equal(GameSettings.Rows, atCap.Rows);
        Assert.Equal(GameSettings.Rows, beyond.Rows);
        Assert.Equal(GameSettings.MaxLayers, beyond.Layers);
        Assert.Equal(100, beyond.Number);
    }

    [Fact]
    public void NonsenseLevelNumbersFallBackToLevelOne()
    {
        Assert.Equal(LevelRules.ForNumber(1), LevelRules.ForNumber(0) with { Number = 1 });
        Assert.Equal(LevelRules.StartingSize, LevelRules.ForNumber(-5).Rows);
    }

    [Fact]
    public void SmallerBoardsStayCentredOnTheOrigin()
    {
        // A 5-wide board spans 4 * 0.5 = 2 units, so its origin must pull back by half of that
        var level = LevelRules.ForNumber(1);

        Assert.Equal(-(level.Rows - 1) * GameSettings.CubeSize.X * 0.5f, level.GridOrigin);
        Assert.Equal(0, level.PlatformCentre.X);
        Assert.True(level.PlatformCentre.Y > 0);
    }

    [Fact]
    public void EveryPaletteHasTheSameColourCount()
    {
        // A live palette switch repaints by index, so unequal counts would throw mid-game
        var expected = ColourPalettes.All[0].Colours.Count;

        Assert.All(ColourPalettes.All, palette => Assert.Equal(expected, palette.Colours.Count));
    }

    [Fact]
    public void ColoursAreDistinctWithinEachPalette()
    {
        // Two identical colours in one palette would silently merge two logical colours into one
        Assert.All(ColourPalettes.All, palette =>
            Assert.Equal(palette.Colours.Count, palette.Colours.Distinct().Count()));
    }

    [Fact]
    public void JsonProgressStoreRoundTrips()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cubicle-progress-{Guid.NewGuid():N}.json");
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

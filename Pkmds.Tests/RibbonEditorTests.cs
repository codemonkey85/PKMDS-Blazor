namespace Pkmds.Tests;

/// <summary>
/// Tests for the ribbon editor logic in RibbonHelper.
/// </summary>
public class RibbonEditorTests
{
    [Theory]
    [InlineData("RibbonMarkLunchtime", true)]
    [InlineData("RibbonMarkSleepyTime", true)]
    [InlineData("RibbonMarkDusk", true)]
    [InlineData("RibbonMarkSlump", true)]
    [InlineData("RibbonMarkAlpha", true)]
    [InlineData("RibbonMarkMightiest", true)]
    [InlineData("RibbonMarkTitan", true)]
    [InlineData("RibbonMarkJumbo", false)]
    [InlineData("RibbonMarkMini", false)]
    [InlineData("RibbonMarkPartner", false)]
    [InlineData("RibbonMarkGourmand", false)]
    [InlineData("RibbonMarkItemfinder", false)]
    [InlineData("RibbonChampionKalos", false)]
    [InlineData("RibbonMasterRank", false)]
    [InlineData("RibbonCountMemoryContest", false)]
    public void IsMarkEntry_ReturnsCorrectResult(string ribbonName, bool expected)
    {
        // Act
        var result = RibbonHelper.IsMarkEntry(ribbonName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("RibbonChampionKalos", "Kalos Champion")]
    [InlineData("RibbonMarkLunchtime", "Lunchtime Mark")]
    [InlineData("RibbonMasterRank", "Master Rank")]
    [InlineData("RibbonChampionG3", "Champion (Gen3)")]
    [InlineData("RibbonCountMemoryContest", "Contest Memory")]
    public void GetRibbonDisplayName_ReturnsRibbonStringsName(string propertyName, string expected)
    {
        // Act
        var result = RibbonHelper.GetRibbonDisplayName(propertyName);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetAllRibbonInfo_PK9_ReturnsNonEmptyList()
    {
        // Arrange
        var pk9 = new PK9();

        // Act
        var ribbons = RibbonHelper.GetAllRibbonInfo(pk9);

        // Assert
        ribbons.Should().NotBeEmpty();
        ribbons.Should().Contain(r => r.Name == "RibbonChampionKalos");
        ribbons.Should().Contain(r => r.Name == "RibbonMarkLunchtime");
    }

    [Fact]
    public void GetAllRibbonInfo_PK3_ReturnsGen3Ribbons()
    {
        // Arrange
        var pk3 = new PK3();

        // Act
        var ribbons = RibbonHelper.GetAllRibbonInfo(pk3);

        // Assert
        ribbons.Should().NotBeEmpty();
        ribbons.Should().Contain(r => r.Name == "RibbonChampionG3");
    }

    [Fact]
    public void GetRibbonSprite_ReturnsExpectedPath()
    {
        // Arrange
        var infoList = RibbonInfo.GetRibbonInfo(new PK9());
        var champKalos = infoList.First(r => r.Name == "RibbonChampionKalos");

        // Act
        var sprite = RibbonHelper.GetRibbonSprite(champKalos);

        // Assert
        sprite.Should().EndWith("ribbonchampionkalos.png");
    }

    [Fact]
    public void GetAllRibbonInfo_NullPokemon_ReturnsEmpty()
    {
        // Act
        var ribbons = RibbonHelper.GetAllRibbonInfo(null);

        // Assert
        ribbons.Should().BeEmpty();
    }

    [Fact]
    public void GetAllRibbonInfo_PK9_HasSeparateRibbonsAndMarks()
    {
        // Arrange
        var pk9 = new PK9();

        // Act
        var allRibbons = RibbonHelper.GetAllRibbonInfo(pk9);
        var ribbons = allRibbons.Where(r => !RibbonHelper.IsMarkEntry(r.Name)).ToList();
        var marks = allRibbons.Where(r => RibbonHelper.IsMarkEntry(r.Name)).ToList();

        // Assert
        ribbons.Should().NotBeEmpty();
        marks.Should().NotBeEmpty();
        ribbons.Should().Contain(r => r.Name == "RibbonChampionKalos");
        marks.Should().Contain(r => r.Name == "RibbonMarkLunchtime");
        ribbons.Should().NotContain(r => r.Name == "RibbonMarkLunchtime");
    }

    [Fact]
    public void GetAllRibbonInfo_ReflectsRibbonStateFromPKM()
    {
        // Arrange
        var pk9 = new PK9();
        pk9.RibbonChampionKalos = true;
        pk9.RibbonMarkLunchtime = true;

        // Act
        var allRibbons = RibbonHelper.GetAllRibbonInfo(pk9);

        // Assert - HasRibbon reflects PKM state
        var champKalos = allRibbons.First(r => r.Name == "RibbonChampionKalos");
        var lunchtime = allRibbons.First(r => r.Name == "RibbonMarkLunchtime");
        var master = allRibbons.First(r => r.Name == "RibbonMasterRank");

        champKalos.HasRibbon.Should().BeTrue();
        lunchtime.HasRibbon.Should().BeTrue();
        master.HasRibbon.Should().BeFalse();
    }

    [Fact]
    public void ByteRibbons_HaveNonZeroMaxCount()
    {
        // Arrange
        var pk3 = new PK3();

        // Act
        var byteRibbons = RibbonHelper.GetAllRibbonInfo(pk3)
            .Where(r => r.Type == RibbonValueType.Byte)
            .ToList();

        // Assert - MaxCount should be > 0 so Give All can set them to max
        byteRibbons.Should().NotBeEmpty();
        byteRibbons.Should().AllSatisfy(r => r.MaxCount.Should().BeGreaterThan(0));
    }
}

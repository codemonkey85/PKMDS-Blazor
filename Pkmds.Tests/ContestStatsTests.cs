namespace Pkmds.Tests;

/// <summary>
/// Tests for contest stat validation and Pokémon contest stat support.
/// </summary>
public class ContestStatsTests
{
    [Theory]
    [InlineData(typeof(PK3))]
    [InlineData(typeof(PK4))]
    [InlineData(typeof(PK5))]
    [InlineData(typeof(PK6))]
    [InlineData(typeof(PK7))]
    [InlineData(typeof(PK8))]
    [InlineData(typeof(PB8))]
    [InlineData(typeof(PA8))]
    [InlineData(typeof(PK9))]
    public void PKM_ImplementsIContestStats(Type pkmType) =>
        // Assert
        typeof(IContestStats).IsAssignableFrom(pkmType).Should().BeTrue();

    [Theory]
    [InlineData(typeof(PK1))]
    [InlineData(typeof(PK2))]
    public void PKM_DoesNotImplementIContestStats(Type pkmType) =>
        // Assert
        typeof(IContestStats).IsAssignableFrom(pkmType).Should().BeFalse();

    [Fact]
    public void ContestStats_PK3_CanBeSetAndRead()
    {
        // Arrange
        var pk3 = new PK3();

        // Act
        pk3.ContestCool = 100;
        pk3.ContestBeauty = 150;
        pk3.ContestCute = 200;
        pk3.ContestSmart = 50;
        pk3.ContestTough = 75;
        pk3.ContestSheen = 255;

        // Assert
        pk3.ContestCool.Should().Be(100);
        pk3.ContestBeauty.Should().Be(150);
        pk3.ContestCute.Should().Be(200);
        pk3.ContestSmart.Should().Be(50);
        pk3.ContestTough.Should().Be(75);
        pk3.ContestSheen.Should().Be(255);
    }

    [Fact]
    public void ContestStats_PK4_CanBeSetAndRead()
    {
        // Arrange
        var pk4 = new PK4();

        // Act
        pk4.ContestCool = 200;
        pk4.ContestBeauty = 200;
        pk4.ContestCute = 200;
        pk4.ContestSmart = 200;
        pk4.ContestTough = 200;
        pk4.ContestSheen = 200;

        // Assert
        pk4.ContestCool.Should().Be(200);
        pk4.ContestBeauty.Should().Be(200);
        pk4.ContestCute.Should().Be(200);
        pk4.ContestSmart.Should().Be(200);
        pk4.ContestTough.Should().Be(200);
        pk4.ContestSheen.Should().Be(200);
    }

    [Fact]
    public void ContestStats_PB8_CanBeSetAndRead()
    {
        // Arrange
        var pb8 = new PB8();

        // Act
        pb8.ContestCool = 255;
        pb8.ContestBeauty = 255;
        pb8.ContestCute = 255;
        pb8.ContestSmart = 255;
        pb8.ContestTough = 255;
        pb8.ContestSheen = 255;

        // Assert
        pb8.ContestCool.Should().Be(255);
        pb8.ContestBeauty.Should().Be(255);
        pb8.ContestCute.Should().Be(255);
        pb8.ContestSmart.Should().Be(255);
        pb8.ContestTough.Should().Be(255);
        pb8.ContestSheen.Should().Be(255);
    }

    [Fact]
    public void ContestStats_MaxValue_IsByteMaxValue() =>
        // Assert
        ((int)byte.MaxValue).Should().Be(255);

    [Fact]
    public void ContestStats_NewPKM_DefaultsToZero()
    {
        // Arrange
        var pk3 = new PK3();

        // Assert
        pk3.ContestCool.Should().Be(0);
        pk3.ContestBeauty.Should().Be(0);
        pk3.ContestCute.Should().Be(0);
        pk3.ContestSmart.Should().Be(0);
        pk3.ContestTough.Should().Be(0);
        pk3.ContestSheen.Should().Be(0);
    }

    [Fact]
    public void ContestStats_ClearAll_SetsAllToZero()
    {
        // Arrange
        IContestStats stats = new PK4
        {
            ContestCool = 255,
            ContestBeauty = 255,
            ContestCute = 255,
            ContestSmart = 255,
            ContestTough = 255,
            ContestSheen = 255
        };

        // Act
        stats.ContestCool = 0;
        stats.ContestBeauty = 0;
        stats.ContestCute = 0;
        stats.ContestSmart = 0;
        stats.ContestTough = 0;
        stats.ContestSheen = 0;

        // Assert
        stats.ContestCool.Should().Be(0);
        stats.ContestBeauty.Should().Be(0);
        stats.ContestCute.Should().Be(0);
        stats.ContestSmart.Should().Be(0);
        stats.ContestTough.Should().Be(0);
        stats.ContestSheen.Should().Be(0);
    }

    [Fact]
    public void ContestStats_MaxAll_SetsAllToByteMaxValue()
    {
        // Arrange
        IContestStats stats = new PK3();

        // Act
        stats.ContestCool = byte.MaxValue;
        stats.ContestBeauty = byte.MaxValue;
        stats.ContestCute = byte.MaxValue;
        stats.ContestSmart = byte.MaxValue;
        stats.ContestTough = byte.MaxValue;
        stats.ContestSheen = byte.MaxValue;

        // Assert
        stats.ContestCool.Should().Be(byte.MaxValue);
        stats.ContestBeauty.Should().Be(byte.MaxValue);
        stats.ContestCute.Should().Be(byte.MaxValue);
        stats.ContestSmart.Should().Be(byte.MaxValue);
        stats.ContestTough.Should().Be(byte.MaxValue);
        stats.ContestSheen.Should().Be(byte.MaxValue);
    }
}

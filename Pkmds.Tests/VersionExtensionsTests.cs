using Pkmds.Rcl.Extensions;

namespace Pkmds.Tests;

public class VersionExtensionsTests
{
    // Build = DDHH (day * 100 + hour), Revision = MMSS (minute * 100 + second)
    [Theory]
    [InlineData(2026, 3, 1109, 4912, "2026.03.11.094912", 2026, 3, 11, 9, 49, 12)]
    [InlineData(2026, 3, 100, 500, "2026.03.01.000500", 2026, 3, 1, 0, 5, 0)]
    [InlineData(2026, 3, 3123, 5959, "2026.03.31.235959", 2026, 3, 31, 23, 59, 59)]
    public void ToDateTime_ValidVersion_ParsesUtcDateTime(
        int year, int month, int build, int revision,
        string expectedVersionString,
        int expectedYear, int expectedMonth, int expectedDay,
        int expectedHour, int expectedMinute, int expectedSecond)
    {
        // Arrange
        var version = new Version(year, month, build, revision);

        // Act
        var versionString = version.ToVersionString();
        var dateTime = version.ToDateTime();

        // Assert
        versionString.Should().Be(expectedVersionString);
        dateTime.Should().Be(new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, expectedSecond, DateTimeKind.Utc));
    }

    [Fact]
    public void ToDateTime_NullVersion_ReturnsNull()
    {
        // Arrange
        Version? version = null;

        // Act
        var result = version.ToDateTime();

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(3224, 0)] // invalid hour (24)
    [InlineData(100, 6000)] // invalid minute (60)
    [InlineData(100, 160)] // invalid second (60)
    public void ToDateTime_InvalidBuildOrRevision_ThrowsArgumentOutOfRangeException(int build, int revision)
    {
        // Arrange
        var version = new Version(2026, 3, build, revision);

        // Act
        Action act = () => version.ToDateTime();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

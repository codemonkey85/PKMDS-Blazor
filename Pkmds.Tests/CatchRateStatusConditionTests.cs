namespace Pkmds.Tests;

/// <summary>
/// Tests for Gen 1-2 Catch Rate and Status Condition editing.
/// </summary>
public class CatchRateStatusConditionTests
{
    [Fact]
    public void PK1_CatchRate_CanBeSetAndRead()
    {
        // Arrange
        var pk1 = new PK1();

        // Act
        pk1.CatchRate = 45;

        // Assert
        pk1.CatchRate.Should().Be(45);
    }

    [Fact]
    public void PK1_CatchRate_SupportsFullByteRange()
    {
        // Arrange
        var pk1 = new PK1();

        // Act & Assert
        pk1.CatchRate = byte.MinValue;
        pk1.CatchRate.Should().Be(byte.MinValue);

        pk1.CatchRate = byte.MaxValue;
        pk1.CatchRate.Should().Be(byte.MaxValue);
    }

    [Fact]
    public void PK1_StatusCondition_DefaultsToNone()
    {
        // Arrange & Act
        var pk1 = new PK1();

        // Assert
        pk1.Status_Condition.Should().Be((int)StatusCondition.None);
    }

    [Theory]
    [InlineData(StatusCondition.None)]
    [InlineData(StatusCondition.Sleep1)]
    [InlineData(StatusCondition.Sleep2)]
    [InlineData(StatusCondition.Sleep3)]
    [InlineData(StatusCondition.Sleep4)]
    [InlineData(StatusCondition.Sleep5)]
    [InlineData(StatusCondition.Sleep6)]
    [InlineData(StatusCondition.Sleep7)]
    [InlineData(StatusCondition.Poison)]
    [InlineData(StatusCondition.Burn)]
    [InlineData(StatusCondition.Freeze)]
    [InlineData(StatusCondition.Paralysis)]
    [InlineData(StatusCondition.PoisonBad)]
    public void PK1_StatusCondition_CanBeSetAndRead(StatusCondition condition)
    {
        // Arrange
        var pk1 = new PK1();

        // Act
        pk1.Status_Condition = (int)condition;

        // Assert
        pk1.Status_Condition.Should().Be((int)condition);
    }

    [Fact]
    public void PK2_StatusCondition_DefaultsToNone()
    {
        // Arrange & Act
        var pk2 = new PK2();

        // Assert
        pk2.Status_Condition.Should().Be((int)StatusCondition.None);
    }

    [Theory]
    [InlineData(StatusCondition.None)]
    [InlineData(StatusCondition.Sleep1)]
    [InlineData(StatusCondition.Sleep2)]
    [InlineData(StatusCondition.Sleep3)]
    [InlineData(StatusCondition.Sleep4)]
    [InlineData(StatusCondition.Sleep5)]
    [InlineData(StatusCondition.Sleep6)]
    [InlineData(StatusCondition.Sleep7)]
    [InlineData(StatusCondition.Poison)]
    [InlineData(StatusCondition.Burn)]
    [InlineData(StatusCondition.Freeze)]
    [InlineData(StatusCondition.Paralysis)]
    [InlineData(StatusCondition.PoisonBad)]
    public void PK2_StatusCondition_CanBeSetAndRead(StatusCondition condition)
    {
        // Arrange
        var pk2 = new PK2();

        // Act
        pk2.Status_Condition = (int)condition;

        // Assert
        pk2.Status_Condition.Should().Be((int)condition);
    }

    [Fact]
    public void PK1_HasCatchRateProperty() =>
        // Assert
        typeof(PK1).GetProperty(nameof(PK1.CatchRate)).Should().NotBeNull();

    [Fact]
    public void PK1_HasStatusConditionProperty() =>
        // Assert
        typeof(PK1).GetProperty(nameof(PKM.Status_Condition)).Should().NotBeNull();

    [Fact]
    public void PK2_HasStatusConditionProperty() =>
        // Assert
        typeof(PK2).GetProperty(nameof(PKM.Status_Condition)).Should().NotBeNull();

    [Fact]
    public void PK1_CatchRate_DefaultsToZero()
    {
        // Arrange & Act
        var pk1 = new PK1();

        // Assert
        pk1.CatchRate.Should().Be(0);
    }
}

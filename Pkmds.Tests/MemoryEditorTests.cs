namespace Pkmds.Tests;

/// <summary>
///     Tests for memory editing support (Gen 6+).
/// </summary>
public class MemoryEditorTests
{
    [Theory]
    [InlineData(typeof(PK6))]
    [InlineData(typeof(PK7))]
    [InlineData(typeof(PK8))]
    [InlineData(typeof(PB8))]
    [InlineData(typeof(PA8))]
    [InlineData(typeof(PK9))]
    [InlineData(typeof(PA9))]
    public void PKM_Gen6Plus_ImplementsIMemoryOT(Type pkmType) =>
        typeof(IMemoryOT).IsAssignableFrom(pkmType).Should().BeTrue();

    [Theory]
    [InlineData(typeof(PK6))]
    [InlineData(typeof(PK7))]
    [InlineData(typeof(PK8))]
    [InlineData(typeof(PB8))]
    [InlineData(typeof(PA8))]
    [InlineData(typeof(PK9))]
    [InlineData(typeof(PA9))]
    public void PKM_Gen6Plus_ImplementsIMemoryHT(Type pkmType) =>
        typeof(IMemoryHT).IsAssignableFrom(pkmType).Should().BeTrue();

    [Theory]
    [InlineData(typeof(PK1))]
    [InlineData(typeof(PK2))]
    [InlineData(typeof(PK3))]
    [InlineData(typeof(PK4))]
    [InlineData(typeof(PK5))]
    public void PKM_PreGen6_DoesNotImplementIMemoryOT(Type pkmType) =>
        typeof(IMemoryOT).IsAssignableFrom(pkmType).Should().BeFalse();

    [Theory]
    [InlineData(typeof(PK6))]
    [InlineData(typeof(PK7))]
    public void PKM_Gen6And7_ImplementsIAffection(Type pkmType) =>
        typeof(IAffection).IsAssignableFrom(pkmType).Should().BeTrue();

    [Theory]
    [InlineData(typeof(PK8))]
    [InlineData(typeof(PB8))]
    [InlineData(typeof(PA8))]
    [InlineData(typeof(PK9))]
    [InlineData(typeof(PA9))]
    public void PKM_Gen8Plus_DoesNotImplementIAffection(Type pkmType) =>
        typeof(IAffection).IsAssignableFrom(pkmType).Should().BeFalse();

    [Fact]
    public void OTMemory_PK6_CanBeSetAndRead()
    {
        // Arrange
        var pk6 = new PK6();
        const byte memory = 4;
        const byte intensity = 3;
        const byte feeling = 5;
        const ushort variable = 100;

        // Act
        IMemoryOT otMemory = pk6;
        otMemory.OriginalTrainerMemory = memory;
        otMemory.OriginalTrainerMemoryIntensity = intensity;
        otMemory.OriginalTrainerMemoryFeeling = feeling;
        otMemory.OriginalTrainerMemoryVariable = variable;

        // Assert
        otMemory.OriginalTrainerMemory.Should().Be(memory);
        otMemory.OriginalTrainerMemoryIntensity.Should().Be(intensity);
        otMemory.OriginalTrainerMemoryFeeling.Should().Be(feeling);
        otMemory.OriginalTrainerMemoryVariable.Should().Be(variable);
    }

    [Fact]
    public void HTMemory_PK6_CanBeSetAndRead()
    {
        // Arrange
        var pk6 = new PK6();
        const byte memory = 7;
        const byte intensity = 2;
        const byte feeling = 1;
        const ushort variable = 200;

        // Act
        IMemoryHT htMemory = pk6;
        htMemory.HandlingTrainerMemory = memory;
        htMemory.HandlingTrainerMemoryIntensity = intensity;
        htMemory.HandlingTrainerMemoryFeeling = feeling;
        htMemory.HandlingTrainerMemoryVariable = variable;

        // Assert
        htMemory.HandlingTrainerMemory.Should().Be(memory);
        htMemory.HandlingTrainerMemoryIntensity.Should().Be(intensity);
        htMemory.HandlingTrainerMemoryFeeling.Should().Be(feeling);
        htMemory.HandlingTrainerMemoryVariable.Should().Be(variable);
    }

    [Fact]
    public void Affection_PK6_CanBeSetAndRead()
    {
        // Arrange
        var pk6 = new PK6();
        const byte otAffection = 200;
        const byte htAffection = 128;

        // Act
        IAffection affection = pk6;
        affection.OriginalTrainerAffection = otAffection;
        affection.HandlingTrainerAffection = htAffection;

        // Assert
        affection.OriginalTrainerAffection.Should().Be(otAffection);
        affection.HandlingTrainerAffection.Should().Be(htAffection);
    }

    [Fact]
    public void Friendship_PK6_CanBeSetAndRead()
    {
        // Arrange
        var pk6 = new PK6();
        const byte otFriendship = 255;
        const byte htFriendship = 100;

        // Act
        pk6.OriginalTrainerFriendship = otFriendship;
        pk6.HandlingTrainerFriendship = htFriendship;

        // Assert
        pk6.OriginalTrainerFriendship.Should().Be(otFriendship);
        pk6.HandlingTrainerFriendship.Should().Be(htFriendship);
    }

    [Fact]
    public void OTMemory_ClearAll_SetsAllToZero()
    {
        // Arrange
        var pk6 = new PK6();
        IMemoryOT otMemory = pk6;
        otMemory.OriginalTrainerMemory = 5;
        otMemory.OriginalTrainerMemoryIntensity = 3;
        otMemory.OriginalTrainerMemoryFeeling = 2;
        otMemory.OriginalTrainerMemoryVariable = 100;

        // Act
        otMemory.OriginalTrainerMemory = 0;
        otMemory.OriginalTrainerMemoryIntensity = 0;
        otMemory.OriginalTrainerMemoryFeeling = 0;
        otMemory.OriginalTrainerMemoryVariable = 0;

        // Assert
        otMemory.OriginalTrainerMemory.Should().Be(0);
        otMemory.OriginalTrainerMemoryIntensity.Should().Be(0);
        otMemory.OriginalTrainerMemoryFeeling.Should().Be(0);
        otMemory.OriginalTrainerMemoryVariable.Should().Be(0);
    }

    [Fact]
    public void HTMemory_ClearAll_SetsAllToZero()
    {
        // Arrange
        var pk6 = new PK6();
        IMemoryHT htMemory = pk6;
        htMemory.HandlingTrainerMemory = 12;
        htMemory.HandlingTrainerMemoryIntensity = 5;
        htMemory.HandlingTrainerMemoryFeeling = 3;
        htMemory.HandlingTrainerMemoryVariable = 250;

        // Act
        htMemory.HandlingTrainerMemory = 0;
        htMemory.HandlingTrainerMemoryIntensity = 0;
        htMemory.HandlingTrainerMemoryFeeling = 0;
        htMemory.HandlingTrainerMemoryVariable = 0;

        // Assert
        htMemory.HandlingTrainerMemory.Should().Be(0);
        htMemory.HandlingTrainerMemoryIntensity.Should().Be(0);
        htMemory.HandlingTrainerMemoryFeeling.Should().Be(0);
        htMemory.HandlingTrainerMemoryVariable.Should().Be(0);
    }

    [Fact]
    public void OTMemory_PK8_CanBeSetAndRead()
    {
        // Arrange
        var pk8 = new PK8();
        IMemoryOT otMemory = pk8;

        // Act
        otMemory.OriginalTrainerMemory = 3;
        otMemory.OriginalTrainerMemoryIntensity = 4;
        otMemory.OriginalTrainerMemoryFeeling = 2;
        otMemory.OriginalTrainerMemoryVariable = 50;

        // Assert
        otMemory.OriginalTrainerMemory.Should().Be(3);
        otMemory.OriginalTrainerMemoryIntensity.Should().Be(4);
        otMemory.OriginalTrainerMemoryFeeling.Should().Be(2);
        otMemory.OriginalTrainerMemoryVariable.Should().Be(50);
    }

    [Fact]
    public void MemoryApplicator_ClearMemories_ResetsAllMemoryFields()
    {
        // Arrange
        var pk6 = new PK6();
        IMemoryOT otMemory = pk6;
        IMemoryHT htMemory = pk6;
        otMemory.OriginalTrainerMemory = 5;
        otMemory.OriginalTrainerMemoryIntensity = 3;
        htMemory.HandlingTrainerMemory = 7;
        htMemory.HandlingTrainerMemoryIntensity = 2;

        // Act
        pk6.ClearMemories();

        // Assert
        otMemory.OriginalTrainerMemory.Should().Be(0);
        otMemory.OriginalTrainerMemoryIntensity.Should().Be(0);
        htMemory.HandlingTrainerMemory.Should().Be(0);
        htMemory.HandlingTrainerMemoryIntensity.Should().Be(0);
    }

    [Fact]
    public void Memory_DefaultValues_AreAllZero()
    {
        // Arrange & Act
        var pk6 = new PK6();
        IMemoryOT otMemory = pk6;
        IMemoryHT htMemory = pk6;

        // Assert
        otMemory.OriginalTrainerMemory.Should().Be(0);
        otMemory.OriginalTrainerMemoryIntensity.Should().Be(0);
        otMemory.OriginalTrainerMemoryFeeling.Should().Be(0);
        otMemory.OriginalTrainerMemoryVariable.Should().Be(0);
        htMemory.HandlingTrainerMemory.Should().Be(0);
        htMemory.HandlingTrainerMemoryIntensity.Should().Be(0);
        htMemory.HandlingTrainerMemoryFeeling.Should().Be(0);
        htMemory.HandlingTrainerMemoryVariable.Should().Be(0);
    }
}

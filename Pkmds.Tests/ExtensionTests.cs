using FluentAssertions;
using PKHeX.Core;
using Pkmds.Rcl.Extensions;

namespace Pkmds.Tests;

/// <summary>
/// Tests for extension methods in PkmExtensions
/// </summary>
public class ExtensionTests
{
    private const string TestFilesPath = "../../../TestFiles";

    [Theory, InlineData((ushort)Species.Bulbasaur, true), InlineData((ushort)Species.Pikachu, true), InlineData((ushort)Species.Lucario, true), InlineData((ushort)Species.None, false)]
    public void IsValidSpecies_ReturnsCorrectResult(ushort speciesId, bool expected)
    {
        // Act
        var result = speciesId.IsValidSpecies();

        // Assert
        result.Should().Be(expected);
    }

    [Theory, InlineData((ushort)Species.Bulbasaur, false), InlineData((ushort)Species.None, true)]
    public void IsInvalidSpecies_ReturnsCorrectResult(ushort speciesId, bool expected)
    {
        // Act
        var result = speciesId.IsInvalidSpecies();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsValidSpecies_NullableValid_ReturnsTrue()
    {
        // Arrange
        ushort? species = (ushort)Species.Pikachu;

        // Act
        var result = species.IsValidSpecies();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidSpecies_NullableNull_ReturnsFalse()
    {
        // Arrange
        ushort? species = null;

        // Act
        var result = species.IsValidSpecies();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetPP_ReturnsFourMoves()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "Lucario_B06DDFAD.pk5");
        var data = File.ReadAllBytes(filePath);
        FileUtil.TryGetPKM(data, out var pkm, ".pk5").Should().BeTrue();

        // Act
        var pp = pkm!.GetPP();

        // Assert
        pp.Should().NotBeNull();
        pp.Count.Should().Be(4);
    }

    [Fact]
    public void SetPP_UpdatesPPCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "Lucario_B06DDFAD.pk5");
        var data = File.ReadAllBytes(filePath);
        var success = FileUtil.TryGetPKM(data, out var pkm, ".pk5");
        success.Should().BeTrue();
        var testPP = 10;

        // Act
        pkm!.SetPP(0, testPP);
        var pp = pkm!.GetPP();

        // Assert
        pp[0].Should().Be(testPP);
    }

    [Fact]
    public void SetPP_NegativeValue_SetToZero()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "Lucario_B06DDFAD.pk5");
        var data = File.ReadAllBytes(filePath);
        var success = FileUtil.TryGetPKM(data, out var pkm, ".pk5");
        success.Should().BeTrue();

        // Act
        pkm!.SetPP(0, -5);
        var pp = pkm!.GetPP();

        // Assert
        pp[0].Should().Be(0);
    }

    [Fact]
    public void GetMaxPP_CalculatesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "Lucario_B06DDFAD.pk5");
        var data = File.ReadAllBytes(filePath);
        FileUtil.TryGetPKM(data, out var pkm, ".pk5").Should().BeTrue();

        // Act
        var maxPP = pkm!.GetMaxPP(0);

        // Assert
        maxPP.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetGenerationTypes_Gen5Pokemon_ReturnsTypes()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "Lucario_B06DDFAD.pk5");
        var data = File.ReadAllBytes(filePath);
        FileUtil.TryGetPKM(data, out var pkm, ".pk5").Should().BeTrue();

        // Act
        var types = pkm!.GetGenerationTypes();

        // Assert
        types.Type1.Should().BeGreaterThan(0);
        types.Type2.Should().BeGreaterThanOrEqualTo(0);
    }
}

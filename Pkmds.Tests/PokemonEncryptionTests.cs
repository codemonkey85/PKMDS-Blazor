namespace Pkmds.Tests;

/// <summary>
/// Tests for Pokémon encryption, decryption, and checksum validation
/// </summary>
public class PokemonEncryptionTests
{
    private const string TestFilesPath = "../../../TestFiles";

    [Fact]
    public void LoadPokemonFile_ValidPK5_DecryptsCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "Lucario_B06DDFAD.pk5");
        var data = File.ReadAllBytes(filePath);

        // Act
        var success = FileUtil.TryGetPKM(data, out var pkm, ".pk5");

        // Assert
        success.Should().BeTrue();
        pkm.Should().NotBeNull();
        pkm.Species.Should().Be((ushort)Species.Lucario);
        pkm.ChecksumValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("POKEMON RED-0.sav")]
    [InlineData("POKEMON RUBY_AXVE-0.sav")]
    [InlineData("Black - Full Completion.sav")]
    public void ExtractPokemonFromSave_ValidatesChecksum(string fileName)
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, fileName);
        var data = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(data, out var saveFile, fileName).Should().BeTrue();

        // Act
        var partyPokemon = saveFile!.GetPartySlotAtIndex(0);

        // Assert
        partyPokemon.Should().NotBeNull();
        if (partyPokemon.Species > 0)
        {
            partyPokemon.ChecksumValid.Should().BeTrue();
        }
    }

    [Fact]
    public void ModifyPokemon_RecalculatesChecksum_CorrectlyUpdatesChecksum()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "Lucario_B06DDFAD.pk5");
        var data = File.ReadAllBytes(filePath);
        FileUtil.TryGetPKM(data, out var pkm, ".pk5").Should().BeTrue();
        var originalLevel = pkm!.CurrentLevel;

        // Act - Modify the Pokémon
        pkm.CurrentLevel = 100;
        pkm.RefreshChecksum();

        // Assert - Checksum should still be valid after modification
        pkm.CurrentLevel.Should().Be(100);
        pkm.CurrentLevel.Should().NotBe(originalLevel);
        pkm.ChecksumValid.Should().BeTrue();
    }

    [Fact]
    public void ClonePokemon_PreservesAllData()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "Lucario_B06DDFAD.pk5");
        var data = File.ReadAllBytes(filePath);
        FileUtil.TryGetPKM(data, out var pkm, ".pk5").Should().BeTrue();

        // Act
        var clone = pkm!.Clone();

        // Assert
        clone.Should().NotBeNull();
        clone.Should().NotBeSameAs(pkm);
        clone.Species.Should().Be(pkm.Species);
        clone.PID.Should().Be(pkm.PID);
        clone.ChecksumValid.Should().BeTrue();
        clone.Data.Length.Should().Be(pkm.Data.Length);
    }

    [Fact]
    public void SavePokemonToBox_UpdatesChecksum()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "Black - Full Completion.sav");
        var data = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(data, out var saveFile, "Black - Full Completion.sav").Should().BeTrue();
        var pokemon = saveFile!.GetBoxSlotAtIndex(0, 0);

        if (pokemon.Species == 0)
        {
            return; // Skip if no Pokémon
        }

        // Act - Modify and save back
        pokemon.Nickname = "TEST";
        pokemon.RefreshChecksum();
        saveFile.SetBoxSlotAtIndex(pokemon, 0, 0);

        // Re-read the Pokémon from save
        var modifiedPokemon = saveFile.GetBoxSlotAtIndex(0, 0);

        // Assert
        modifiedPokemon.Should().NotBeNull();
        modifiedPokemon.Nickname.Should().Be("TEST");
        modifiedPokemon.ChecksumValid.Should().BeTrue();
    }
}

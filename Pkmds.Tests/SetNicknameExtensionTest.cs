namespace Pkmds.Tests;

/// <summary>
/// Test to verify SetNickname extension method exists and works
/// </summary>
public class SetNicknameExtensionTest
{
    private const string TestFilesPath = "../../../TestFiles";

    [Fact]
    public void Gen2Pokemon_UseSetNicknameExtension_ShouldWork()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "PM_CRYSTAL_BXTJ-0.sav");
        var originalData = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(originalData, out var saveFile, "PM_CRYSTAL_BXTJ-0.sav").Should().BeTrue();
        var pokemon = saveFile!.GetPartySlotAtIndex(0);

        if (pokemon.Species == 0)
        {
            return; // Skip if no Pokémon
        }

        var testNickname = "TEST";

        // Act - Use SetNickname extension method and refresh checksum
        pokemon.SetNickname(testNickname);
        pokemon.RefreshChecksum();
        saveFile.SetPartySlotAtIndex(pokemon, 0);
        var savedData = saveFile.Write();

        // Reload and verify
        SaveUtil.TryGetSaveFile(savedData, out var reloadedSave, "PM_CRYSTAL_BXTJ-0.sav").Should().BeTrue();
        var reloadedPokemon = reloadedSave!.GetPartySlotAtIndex(0);

        // Assert
        reloadedPokemon.Should().NotBeNull();
        reloadedPokemon.Nickname.Should().Be(testNickname);
        reloadedPokemon.ChecksumValid.Should().BeTrue();
        reloadedSave.ChecksumsValid.Should().BeTrue();
    }
}

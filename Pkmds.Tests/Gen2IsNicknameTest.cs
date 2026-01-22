namespace Pkmds.Tests;

/// <summary>
/// Test setting IsNicknamed flag explicitly for Gen II
/// </summary>
public class Gen2IsNicknameTest
{
    private const string TestFilesPath = "../../../TestFiles";

    [Fact]
    public void Gen2Pokemon_SetIsNicknameFlag_ShouldPreserveNickname()
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

        // Act - Set nickname AND IsNicknamed flag explicitly
        pokemon.Nickname = testNickname;
        pokemon.IsNicknamed = true;
        pokemon.RefreshChecksum();
        saveFile.SetPartySlotAtIndex(pokemon, 0);
        var savedData = saveFile.Write();

        // Reload and verify
        SaveUtil.TryGetSaveFile(savedData, out var reloadedSave, "PM_CRYSTAL_BXTJ-0.sav").Should().BeTrue();
        var reloadedPokemon = reloadedSave!.GetPartySlotAtIndex(0);

        // Assert
        reloadedPokemon.Should().NotBeNull();
        reloadedPokemon.Nickname.Should().Be(testNickname);
        reloadedPokemon.IsNicknamed.Should().BeTrue();
        reloadedPokemon.ChecksumValid.Should().BeTrue();
        reloadedSave.ChecksumsValid.Should().BeTrue();
    }
}

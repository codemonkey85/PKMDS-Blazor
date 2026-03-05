namespace Pkmds.Tests;

/// <summary>
/// Test Gen II Silver save (English) for nickname handling
/// </summary>
public class Gen2SilverTest
{
    private const string TestFilesPath = "../../../TestFiles";

    [Fact]
    public void SilverParty_SetNickname_ShouldWork()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "Pokemon - Silver Version (UE) [C][!].sav");
        var originalData = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(originalData, out var saveFile, "Pokemon - Silver Version (UE) [C][!].sav").Should()
            .BeTrue();

        var pokemon = saveFile!.GetPartySlotAtIndex(0);

        if (pokemon.Species == 0)
        {
            return; // Skip if no Pokémon
        }

        const string testNickname = "TESTER";

        // Act - Set nickname
        pokemon.Nickname = testNickname;
        pokemon.IsNicknamed = true;
        pokemon.RefreshChecksum();

        saveFile.SetPartySlotAtIndex(pokemon, 0);
        var savedData = saveFile.Write();

        // Reload and verify
        SaveUtil.TryGetSaveFile(savedData, out var reloadedSave, "Pokemon - Silver Version (UE) [C][!].sav").Should()
            .BeTrue();
        var reloadedPokemon = reloadedSave!.GetPartySlotAtIndex(0);

        // Assert
        reloadedPokemon.Should().NotBeNull();
        reloadedPokemon.Nickname.Should().Be(testNickname);
        reloadedPokemon.ChecksumValid.Should().BeTrue();
        reloadedSave.ChecksumsValid.Should().BeTrue();
    }
}

namespace Pkmds.Tests;

/// <summary>
/// Tests for Gen II nickname and string handling
/// </summary>
public class Gen2NicknameTests
{
    private const string TestFilesPath = "../../../TestFiles";

    [Fact]
    public void Gen2Pokemon_SetNickname_DoesNotProduceQuestionMarks()
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

        // Act - Modify Pokémon nickname
        pokemon.Nickname = testNickname;
        saveFile.SetPartySlotAtIndex(pokemon, 0);
        var savedData = saveFile.Write();

        // Reload and verify
        SaveUtil.TryGetSaveFile(savedData, out var reloadedSave, "PM_CRYSTAL_BXTJ-0.sav").Should().BeTrue();
        var reloadedPokemon = reloadedSave!.GetPartySlotAtIndex(0);

        // Assert
        reloadedPokemon.Should().NotBeNull();
        reloadedPokemon.Nickname.Should().Be(testNickname);
        reloadedPokemon.Nickname.Should().NotContain("?");
        reloadedPokemon.ChecksumValid.Should().BeTrue();
        reloadedSave.ChecksumsValid.Should().BeTrue();
    }

    [Fact]
    public void Gen2Pokemon_SetOTName_DoesNotProduceQuestionMarks()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "Pokemon - Silver Version (UE) [C][!].sav");
        var originalData = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(originalData, out var saveFile, "Pokemon - Silver Version (UE) [C][!].sav").Should().BeTrue();
        var pokemon = saveFile!.GetPartySlotAtIndex(0);

        if (pokemon.Species == 0)
        {
            return; // Skip if no Pokémon
        }

        var testOTName = "TESTER";

        // Act - Modify Pokémon OT name
        pokemon.OriginalTrainerName = testOTName;
        saveFile.SetPartySlotAtIndex(pokemon, 0);
        var savedData = saveFile.Write();

        // Reload and verify
        SaveUtil.TryGetSaveFile(savedData, out var reloadedSave, "Pokemon - Silver Version (UE) [C][!].sav").Should().BeTrue();
        var reloadedPokemon = reloadedSave!.GetPartySlotAtIndex(0);

        // Assert
        reloadedPokemon.Should().NotBeNull();
        reloadedPokemon.OriginalTrainerName.Should().Be(testOTName);
        reloadedPokemon.OriginalTrainerName.Should().NotContain("?");
        reloadedPokemon.ChecksumValid.Should().BeTrue();
        reloadedSave.ChecksumsValid.Should().BeTrue();
    }

    [Fact]
    public void Gen2Pokemon_ModifyNicknameInBox_PreservesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "PM_CRYSTAL_BXTJ-0.sav");
        var originalData = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(originalData, out var saveFile, "PM_CRYSTAL_BXTJ-0.sav").Should().BeTrue();
        var pokemon = saveFile!.GetBoxSlotAtIndex(0, 0);

        if (pokemon.Species == 0)
        {
            return; // Skip if no Pokémon
        }

        var testNickname = "NICK";

        // Act - Modify Pokémon in box
        pokemon.Nickname = testNickname;
        pokemon.RefreshChecksum();
        saveFile.SetBoxSlotAtIndex(pokemon, 0, 0);
        var savedData = saveFile.Write();

        // Reload and verify
        SaveUtil.TryGetSaveFile(savedData, out var reloadedSave, "PM_CRYSTAL_BXTJ-0.sav").Should().BeTrue();
        var reloadedPokemon = reloadedSave!.GetBoxSlotAtIndex(0, 0);

        // Assert
        reloadedPokemon.Should().NotBeNull();
        reloadedPokemon.Nickname.Should().Be(testNickname);
        reloadedPokemon.Nickname.Should().NotContain("?");
        reloadedPokemon.ChecksumValid.Should().BeTrue();
        reloadedSave.ChecksumsValid.Should().BeTrue();
    }
}

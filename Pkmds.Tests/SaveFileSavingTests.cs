namespace Pkmds.Tests;

/// <summary>
/// Tests for saving save files and verifying data integrity
/// </summary>
public class SaveFileSavingTests
{
    private const string TestFilesPath = "../../../TestFiles";

    [Theory]
    [InlineData("POKEMON RED-0.sav")]
    [InlineData("POKEMON RUBY_AXVE-0.sav")]
    [InlineData("x.sav")]
    [InlineData("sun.sav")]
    [InlineData("Test-Save-Shield.sav")]
    [InlineData("Test-Save-Scarlet.sav")]
    public void SaveFile_WritesAndReloads_MaintainsIntegrity(string fileName)
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, fileName);
        var originalData = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(originalData, out var saveFile, fileName).Should().BeTrue();

        // Act - Write and reload
        var savedData = saveFile!.Write();
        var success = SaveUtil.TryGetSaveFile(savedData, out var reloadedSave, fileName);

        // Assert
        success.Should().BeTrue();
        reloadedSave.Should().NotBeNull();
        reloadedSave.Should().BeOfType(saveFile.GetType());
        reloadedSave.ChecksumsValid.Should().BeTrue();
    }

    [Fact]
    public void ModifyAndSave_PreservesChanges()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "Black - Full Completion.sav");
        var originalData = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(originalData, out var saveFile, "Black - Full Completion.sav").Should().BeTrue();
        var pokemon = saveFile!.GetPartySlotAtIndex(0);

        if (pokemon.Species == 0)
        {
            return; // Skip if no Pokémon
        }

        const string testNickname = "TESTMON";

        // Act - Modify Pokémon and save
        pokemon.Nickname = testNickname;
        saveFile.SetPartySlotAtIndex(pokemon, 0);
        var savedData = saveFile.Write();

        // Reload and verify
        SaveUtil.TryGetSaveFile(savedData, out var reloadedSave, "Black - Full Completion.sav").Should().BeTrue();
        var reloadedPokemon = reloadedSave!.GetPartySlotAtIndex(0);

        // Assert
        reloadedPokemon.Should().NotBeNull();
        reloadedPokemon.Nickname.Should().Be(testNickname);
        reloadedPokemon.ChecksumValid.Should().BeTrue();
        reloadedSave.ChecksumsValid.Should().BeTrue();
    }

    [Fact]
    public void ModifyBoxPokemon_SavesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "Black - Full Completion.sav");
        var originalData = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(originalData, out var saveFile, "Black - Full Completion.sav").Should().BeTrue();
        var pokemon = saveFile!.GetBoxSlotAtIndex(0, 0);

        if (pokemon.Species == 0)
        {
            return; // Skip if no Pokémon
        }

        const byte testLevel = 99;

        // Act - Modify Pokémon in box
        pokemon.CurrentLevel = testLevel;
        pokemon.RefreshChecksum();
        saveFile.SetBoxSlotAtIndex(pokemon, 0, 0);
        var savedData = saveFile.Write();

        // Reload and verify
        SaveUtil.TryGetSaveFile(savedData, out var reloadedSave, "Black - Full Completion.sav").Should().BeTrue();
        var reloadedPokemon = reloadedSave!.GetBoxSlotAtIndex(0, 0);

        // Assert
        reloadedPokemon.Should().NotBeNull();
        reloadedPokemon.CurrentLevel.Should().Be(testLevel);
        reloadedPokemon.ChecksumValid.Should().BeTrue();
        reloadedSave.ChecksumsValid.Should().BeTrue();
    }

    [Fact]
    public void ModifyTrainerInfo_SavesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "Black - Full Completion.sav");
        var originalData = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(originalData, out var saveFile, "Black - Full Completion.sav").Should().BeTrue();
        const uint testMoney = 123456;

        // Act - Modify trainer money
        saveFile!.Money = testMoney;
        var savedData = saveFile.Write();

        // Reload and verify
        SaveUtil.TryGetSaveFile(savedData, out var reloadedSave, "Black - Full Completion.sav").Should().BeTrue();

        // Assert
        reloadedSave.Should().NotBeNull();
        reloadedSave.Money.Should().Be(testMoney);
        reloadedSave.ChecksumsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateBlankPokemon_SaveToBox_Works()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "Black - Full Completion.sav");
        var originalData = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(originalData, out var saveFile, "Black - Full Completion.sav").Should().BeTrue();

        // Act - Create a blank Pokémon
        var blankPokemon = saveFile!.BlankPKM;
        saveFile.SetBoxSlotAtIndex(blankPokemon, 0, 5);
        var savedData = saveFile.Write();

        // Reload and verify
        SaveUtil.TryGetSaveFile(savedData, out var reloadedSave, "Black - Full Completion.sav").Should().BeTrue();
        var reloadedPokemon = reloadedSave!.GetBoxSlotAtIndex(0, 5);

        // Assert
        reloadedPokemon.Should().NotBeNull();
        reloadedPokemon.Species.Should().Be(0);
        reloadedSave.ChecksumsValid.Should().BeTrue();
    }
}

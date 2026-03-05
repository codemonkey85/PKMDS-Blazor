namespace Pkmds.Tests;

/// <summary>
/// Tests validating the fix for Gen II nickname encoding issues
/// </summary>
public class Gen2NicknameFallbackTests
{
    private const string TestFilesPath = "../../../TestFiles";

    [Fact]
    public void EnglishGen2_SetValidNickname_ShouldWork()
    {
        // Arrange  
        var filePath = Path.Combine(TestFilesPath, "Pokemon - Silver Version (UE) [C][!].sav");
        var originalData = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(originalData, out var saveFile, "Pokemon - Silver Version (UE) [C][!].sav").Should()
            .BeTrue();
        var pokemon = saveFile!.GetPartySlotAtIndex(0);

        if (pokemon.Species == 0)
        {
            return;
        }

        const string testNickname = "TESTER";

        // Act - Simulating what MainTab.SetPokemonNickname does
        pokemon.IsNicknamed = true;
        pokemon.Nickname = testNickname;

        // For Gen I/II, verify the nickname was set correctly
        if (pokemon.Format <= 2 && string.IsNullOrEmpty(pokemon.Nickname))
        {
            var defaultName = SpeciesName.GetSpeciesNameGeneration(pokemon.Species, pokemon.Language, pokemon.Format);
            pokemon.Nickname = defaultName;
            pokemon.IsNicknamed = false;
        }

        // Assert
        pokemon.Nickname.Should().NotBeEmpty();
        pokemon.Nickname.Should().Be(testNickname);
    }

    [Fact]
    public void JapaneseGen2_SetInvalidEnglishNickname_ShouldFallbackToSpeciesName()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "PM_CRYSTAL_BXTJ-0.sav");
        var originalData = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(originalData, out var saveFile, "PM_CRYSTAL_BXTJ-0.sav").Should().BeTrue();
        var pokemon = saveFile!.GetPartySlotAtIndex(0);

        if (pokemon.Species == 0)
        {
            return;
        }

        const string testNickname = "TEST"; // English characters - invalid for Japanese encoding

        // Act - Simulating what MainTab.SetPokemonNickname does
        var defaultName = SpeciesName.GetSpeciesNameGeneration(pokemon.Species, pokemon.Language, pokemon.Format);
        pokemon.IsNicknamed = true;
        pokemon.Nickname = testNickname;

        // For Gen I/II, verify the nickname was set correctly
        // If it becomes empty, the characters were not valid for the Pokémon's language/encoding
        if (pokemon.Format <= 2 && string.IsNullOrEmpty(pokemon.Nickname))
        {
            // Fallback to default name if nickname couldn't be encoded
            pokemon.Nickname = defaultName;
            pokemon.IsNicknamed = false;
        }

        // Assert
        pokemon.Nickname.Should().NotBeEmpty();
        pokemon.Nickname.Should().Be(defaultName); // Should fall back to species name
        pokemon.IsNicknamed.Should().BeFalse(); // Should not be marked as nicknamed
    }

    [Fact]
    public void JapaneseGen2_SetValidJapaneseNickname_ShouldWork()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "PM_CRYSTAL_BXTJ-0.sav");
        var originalData = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(originalData, out var saveFile, "PM_CRYSTAL_BXTJ-0.sav").Should().BeTrue();
        var pokemon = saveFile!.GetPartySlotAtIndex(0);

        if (pokemon.Species == 0)
        {
            return;
        }

        const string testNickname = "ピカ"; // Japanese characters - valid for Japanese encoding

        // Act - Simulating what MainTab.SetPokemonNickname does
        var defaultName = SpeciesName.GetSpeciesNameGeneration(pokemon.Species, pokemon.Language, pokemon.Format);
        pokemon.IsNicknamed = true;
        pokemon.Nickname = testNickname;

        // For Gen I/II, verify the nickname was set correctly
        if (pokemon.Format <= 2 && string.IsNullOrEmpty(pokemon.Nickname))
        {
            pokemon.Nickname = defaultName;
            pokemon.IsNicknamed = false;
        }

        // Assert
        pokemon.Nickname.Should().NotBeEmpty();
        pokemon.Nickname.Should().Be(testNickname); // Should keep Japanese nickname
        pokemon.IsNicknamed.Should().BeTrue();
    }
}

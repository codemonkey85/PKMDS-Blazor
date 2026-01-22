namespace Pkmds.Tests;

/// <summary>
/// Test Japanese Crystal with valid Japanese nickname
/// </summary>
public class JapaneseNicknameTest
{
    private const string TestFilesPath = "../../../TestFiles";

    [Fact]
    public void JapaneseCrystal_SetJapaneseNickname_ShouldWork()
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

        // Try using a simple Japanese nickname (Katakana ピカ = Pika)
        var testNickname = "ピカ";

        // Act - Set Japanese nickname
        pokemon.Nickname = testNickname;
        pokemon.IsNicknamed = true;
        pokemon.RefreshChecksum();
        
        Console.WriteLine($"After setting - nickname: '{pokemon.Nickname}'");
        Console.WriteLine($"After setting - Is nicknamed: {pokemon.IsNicknamed}");
        
        saveFile.SetPartySlotAtIndex(pokemon, 0);
        var savedData = saveFile.Write();

        // Reload and verify
        SaveUtil.TryGetSaveFile(savedData, out var reloadedSave, "PM_CRYSTAL_BXTJ-0.sav").Should().BeTrue();
        var reloadedPokemon = reloadedSave!.GetPartySlotAtIndex(0);

        Console.WriteLine($"After reload - nickname: '{reloadedPokemon.Nickname}'");
        Console.WriteLine($"After reload - Is nicknamed: {reloadedPokemon.IsNicknamed}");

        // Assert
        reloadedPokemon.Should().NotBeNull();
        reloadedPokemon.Nickname.Should().Be(testNickname);
        reloadedPokemon.IsNicknamed.Should().BeTrue();
    }
    
    [Fact]
    public void JapaneseCrystal_ChangeLanguageFirst_ThenSetEnglishNickname()
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

        var testNickname = "TEST";

        // Act - Change language to English first, then set nickname
        pokemon.Language = 2; // English
        pokemon.Nickname = testNickname;
        pokemon.IsNicknamed = true;
        pokemon.RefreshChecksum();
        
        Console.WriteLine($"After setting - Language: {pokemon.Language}");
        Console.WriteLine($"After setting - nickname: '{pokemon.Nickname}'");
        Console.WriteLine($"After setting - Is nicknamed: {pokemon.IsNicknamed}");
        
        saveFile.SetPartySlotAtIndex(pokemon, 0);
        var savedData = saveFile.Write();

        // Reload and verify
        SaveUtil.TryGetSaveFile(savedData, out var reloadedSave, "PM_CRYSTAL_BXTJ-0.sav").Should().BeTrue();
        var reloadedPokemon = reloadedSave!.GetPartySlotAtIndex(0);

        Console.WriteLine($"After reload - Language: {reloadedPokemon.Language}");
        Console.WriteLine($"After reload - nickname: '{reloadedPokemon.Nickname}'");
        Console.WriteLine($"After reload - Is nicknamed: {reloadedPokemon.IsNicknamed}");

        // Assert
        reloadedPokemon.Should().NotBeNull();
        reloadedPokemon.Nickname.Should().Be(testNickname);
        reloadedPokemon.IsNicknamed.Should().BeTrue();
    }
}

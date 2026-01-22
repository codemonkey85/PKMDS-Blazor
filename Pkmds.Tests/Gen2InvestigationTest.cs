namespace Pkmds.Tests;

/// <summary>
/// Investigation test for Gen II nickname handling
/// </summary>
public class Gen2InvestigationTest
{
    private const string TestFilesPath = "../../../TestFiles";

    [Fact]
    public void InvestigateGen2NicknameHandling()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "PM_CRYSTAL_BXTJ-0.sav");
        var originalData = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(originalData, out var saveFile, "PM_CRYSTAL_BXTJ-0.sav").Should().BeTrue();
        
        Console.WriteLine($"Save file type: {saveFile!.GetType().Name}");
        Console.WriteLine($"Generation: {saveFile.Generation}");
        Console.WriteLine($"Max nickname length: {saveFile.MaxStringLengthNickname}");
        
        var pokemon = saveFile.GetPartySlotAtIndex(0);
        
        if (pokemon.Species == 0)
        {
            Console.WriteLine("No Pokemon in party slot 0");
            return;
        }

        Console.WriteLine($"\nPokemon type: {pokemon.GetType().Name}");
        Console.WriteLine($"Species: {pokemon.Species}");
        Console.WriteLine($"Original Nickname: '{pokemon.Nickname}'");
        Console.WriteLine($"IsNicknamed: {pokemon.IsNicknamed}");
        
        // Test setting nickname
        Console.WriteLine($"\nSetting nickname to 'TEST'...");
        pokemon.Nickname = "TEST";
        Console.WriteLine($"After set - Nickname: '{pokemon.Nickname}'");
        Console.WriteLine($"After set - IsNicknamed: {pokemon.IsNicknamed}");
        
        // Save and reload
        saveFile.SetPartySlotAtIndex(pokemon, 0);
        var savedData = saveFile.Write();
        
        SaveUtil.TryGetSaveFile(savedData, out var reloadedSave, "PM_CRYSTAL_BXTJ-0.sav");
        var reloadedPokemon = reloadedSave!.GetPartySlotAtIndex(0);
        
        Console.WriteLine($"\nAfter reload - Nickname: '{reloadedPokemon.Nickname}'");
        Console.WriteLine($"After reload - IsNicknamed: {reloadedPokemon.IsNicknamed}");
    }
}

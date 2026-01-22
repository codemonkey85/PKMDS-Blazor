namespace Pkmds.Tests;

/// <summary>
/// Test Japanese Crystal save nickname handling details
/// </summary>
public class JapaneseCrystalTest
{
    private const string TestFilesPath = "../../../TestFiles";

    [Fact]
    public void JapaneseCrystal_ExamineNicknameState()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "PM_CRYSTAL_BXTJ-0.sav");
        var originalData = File.ReadAllBytes(filePath);
        SaveUtil.TryGetSaveFile(originalData, out var saveFile, "PM_CRYSTAL_BXTJ-0.sav").Should().BeTrue();
        
        Console.WriteLine($"Save type: {saveFile!.GetType().Name}");
        Console.WriteLine($"Generation: {saveFile.Generation}");
        Console.WriteLine($"Language: {saveFile.Language}");
        Console.WriteLine($"OT: {saveFile.OT}");
        
        var pokemon = saveFile.GetPartySlotAtIndex(0);

        if (pokemon.Species == 0)
        {
            Console.WriteLine("No Pokemon in slot 0");
            return;
        }

        Console.WriteLine($"\nPokemon type: {pokemon.GetType().Name}");
        Console.WriteLine($"Species: {pokemon.Species}");
        Console.WriteLine($"Language: {pokemon.Language}");
        Console.WriteLine($"Original nickname: '{pokemon.Nickname}'");
        Console.WriteLine($"Is nicknamed: {pokemon.IsNicknamed}");
        
        // Check what the species name should be for this language
        var speciesName = PKHeX.Core.SpeciesName.GetSpeciesNameGeneration(pokemon.Species, pokemon.Language, 2);
        Console.WriteLine($"Expected species name for language: '{speciesName}'");
        Console.WriteLine($"Nickname matches species name: {pokemon.Nickname == speciesName}");

        var testNickname = "TEST";

        // Act - Set nickname
        pokemon.Nickname = testNickname;
        pokemon.IsNicknamed = true;
        pokemon.RefreshChecksum();
        
        Console.WriteLine($"\nAfter setting - nickname: '{pokemon.Nickname}'");
        Console.WriteLine($"After setting - Is nicknamed: {pokemon.IsNicknamed}");
        
        saveFile.SetPartySlotAtIndex(pokemon, 0);
        
        // Check before writing
        var checkPokemon = saveFile.GetPartySlotAtIndex(0);
        Console.WriteLine($"Before write - nickname: '{checkPokemon.Nickname}'");
        Console.WriteLine($"Before write - Is nicknamed: {checkPokemon.IsNicknamed}");
        
        var savedData = saveFile.Write();

        // Reload and verify
        SaveUtil.TryGetSaveFile(savedData, out var reloadedSave, "PM_CRYSTAL_BXTJ-0.sav").Should().BeTrue();
        var reloadedPokemon = reloadedSave!.GetPartySlotAtIndex(0);

        Console.WriteLine($"\nAfter reload - nickname: '{reloadedPokemon.Nickname}'");
        Console.WriteLine($"After reload - Is nicknamed: {reloadedPokemon.IsNicknamed}");
    }
}

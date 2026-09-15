namespace Pkmds.Tests;

public class PokemonStorageTests
{
    [Fact]
    public void LeafGreenSave_UsesStandardBoxStorage()
    {
        var (saveFile, _, _, _) = BunitTestHelpers.LoadSave("POKEMON LEAF_BPGE-0.sav");

        saveFile.Version.Should().Be(GameVersion.LG);
        PokemonStorageComponent.UsesLetsGoStorage(saveFile).Should().BeFalse();
    }

    [Fact]
    public void LetsGoSave_UsesLetsGoBoxStorage()
    {
        var (saveFile, _, _, _) = BunitTestHelpers.LoadSave("Lets-Go-Pikachu-All-Pokemon.bin");

        saveFile.Should().BeOfType<SAV7b>();
        PokemonStorageComponent.UsesLetsGoStorage(saveFile).Should().BeTrue();
    }
}

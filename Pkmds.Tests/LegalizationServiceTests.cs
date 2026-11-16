namespace Pkmds.Tests;

[Collection("Sequential")]
public class LegalizationServiceTests
{
    [Fact]
    public async Task LegalizeZA_SkipsUnsupportedVersionContextsAndLeavesInputUnchanged()
    {
        var save = new SAV9ZA();
        ParseSettings.InitFromSaveFileData(save);
        var state = new TestAppState { SaveFile = save };
        var service = new LegalizationService(state);
        var pokemon = new PA9
        {
            Species = (ushort)Species.Charmander,
            Version = GameVersion.ZA,
            CurrentLevel = 100,
            Language = (int)LanguageID.English,
            Move1 = (ushort)Move.Ember,
        };
        pokemon.SetDefaultNickname();
        var original = pokemon.Data.ToArray();

        // Both preserve and rebuild paths enumerate the modern version range.
        // PKHeX.Core 26.10.18 throws for its transfer-only Context.None entries.
        var result = await service.LegalizeAsync(pokemon, save,
            ct: TestContext.Current.CancellationToken, timeoutSeconds: 2);

        result.Pokemon.Species.Should().Be(pokemon.Species);
        result.Pokemon.Should().BeOfType<PA9>();
        pokemon.Data.ToArray().Should().Equal(original);
    }
}

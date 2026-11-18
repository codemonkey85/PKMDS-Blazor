namespace Pkmds.Tests;

[Collection("Sequential")]
public class LegalizationServiceTests
{
    [Fact]
    public async Task EmptySetReturnsExplainedFailureInBothGenerationPaths()
    {
        var save = new SAV7SM();
        var service = new LegalizationService(new TestAppState());
        var set = new ShowdownSet("");

        var sync = service.GenerateFromSetSync(set, save).Should().BeOfType<LegalizationFailure>().Subject;
        var asyncResult = (await service.GenerateFromSetAsync(set, save,
                ct: TestContext.Current.CancellationToken))
            .Should().BeOfType<LegalizationFailure>().Subject;

        sync.Reason.Should().Be("No species specified.");
        asyncResult.Reason.Should().Be(sync.Reason);
        sync.Pokemon.Species.Should().Be(0);
        asyncResult.Pokemon.Species.Should().Be(0);
    }

    [Fact]
    public async Task PreCancelledGenerationRetainsPopulatedBestEffortFailure()
    {
        var save = new SAV7SM { OT = "OUTCOME", Language = (int)LanguageID.English };
        ParseSettings.InitFromSaveFileData(save);
        var original = save.Data.ToArray();
        var service = new LegalizationService(new TestAppState());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var result = await service.GenerateFromSetAsync(new ShowdownSet("Pikachu\nLevel: 100\n- Thunderbolt"),
            save, ct: cancellation.Token);

        var failure = result.Should().BeOfType<LegalizationFailure>().Subject;
        failure.Reason.Should().Be("Cancelled.");
        failure.Pokemon.Species.Should().Be((ushort)Species.Pikachu);
        save.Data.ToArray().Should().Equal(original);
    }

    [Fact]
    public async Task CancellationAtCooperativeYieldStillPropagates()
    {
        var save = new SAV7SM { OT = "OUTCOME", Language = (int)LanguageID.English };
        ParseSettings.InitFromSaveFileData(save);
        var original = save.Data.ToArray();
        var service = new LegalizationService(new TestAppState());
        using var cancellation = new CancellationTokenSource();
        var progress = new CancelOnProgress(cancellation);

        var act = () => service.GenerateFromSetAsync(new ShowdownSet("Pikachu\nLevel: 100\n- Thunderbolt"),
            save, progress, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        save.Data.ToArray().Should().Equal(original);
    }

    private sealed class CancelOnProgress(CancellationTokenSource cancellation) : IProgress<string>
    {
        public void Report(string value) => cancellation.Cancel();
    }

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

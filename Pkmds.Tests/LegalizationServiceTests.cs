namespace Pkmds.Tests;

[Collection("Sequential")]
public class LegalizationServiceTests
{
    [Theory]
    [InlineData("Pikachu", 0, true)]
    [InlineData("Pikachu", 1, true)]
    [InlineData("Pikachu", 2, true)]
    [InlineData("Pikachu (M)", 0, true)]
    [InlineData("Pikachu (M)", 1, false)]
    [InlineData("Pikachu (F)", 0, false)]
    [InlineData("Pikachu (F)", 1, true)]
    [InlineData("Pikachu (M)", 255, true)]
    [InlineData("Pikachu (F)", 255, true)]
    public void EncounterGenderFilterHonorsOnlyExplicitGenderRequests(string text, byte gender, bool expected)
    {
        var encounter = new EncounterStatic7(GameVersion.SN)
        {
            Species = (ushort)Species.Pikachu,
            Level = 5,
            Gender = gender
        };

        LegalizationService.IsEncounterValid(new ShowdownSet(text), encounter).Should().Be(expected);
    }

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

        var originalPriority = EncounterMovesetGenerator.PriorityList;
        try
        {
            // The first egg encounter reports progress and cancels. Eagerly collecting
            // the search results would reach the guard before processing that encounter.
            EncounterMovesetGenerator.PriorityList = new StopAfterEggGroup();
            var act = () => service.GenerateFromSetAsync(new ShowdownSet("Pikachu\nLevel: 100"),
                save, progress, cancellation.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            EncounterMovesetGenerator.PriorityList = originalPriority;
        }

        save.Data.ToArray().Should().Equal(original);
    }

    private sealed class StopAfterEggGroup : IReadOnlyCollection<EncounterTypeGroup>
    {
        public int Count => 1;

        public IEnumerator<EncounterTypeGroup> GetEnumerator()
        {
            yield return EncounterTypeGroup.Egg;
            throw new InvalidOperationException("Encounter search continued past the cancellation point.");
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
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

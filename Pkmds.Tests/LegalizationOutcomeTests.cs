namespace Pkmds.Tests;

[Collection("Sequential")]
public class LegalizationOutcomeTests
{
    [Theory]
    [InlineData("success")]
    [InlineData("success-with-changes")]
    [InlineData("failure")]
    [InlineData("cancelled")]
    [InlineData("timeout")]
    [InlineData("hax")]
    public async Task EditorPreservesReplacementWarningAndChangeSummaryBehavior(string kind)
    {
        var (save, state, refresh, appService) = BunitTestHelpers.LoadSave("Black - Full Completion.sav");
        await using var ctx = BunitTestHelpers.CreateBunitContext(state, refresh, appService);
        var replacement = save.GetPartySlotAtIndex(0);
        var input = replacement.Clone();
        input.Ability = 0;
        var changes = new LegalizationChanges([new(LegalizationChangeCategory.Stats, "HP IV", "0", "31")]);
        var outcome = Outcome(kind, replacement, changes);
        ctx.Services.AddSingleton<ILegalizationService>(new FixedLegalizationService(outcome));
        ctx.Render<MudPopoverProvider>();
        var replacements = new List<PKM>();
        var cut = ctx.Render<LegalityTab>(p => p
            .Add(c => c.Pokemon, input)
            .Add(c => c.Analysis, appService.GetLegalityAnalysis(input))
            .Add(c => c.OnPokemonLegalized, (PKM pokemon) => replacements.Add(pokemon)));

        await cut.FindAll("button").Single(b => b.TextContent.Trim() == "Legalize")
            .ClickAsync(new MouseEventArgs());

        if (kind is "success" or "success-with-changes" or "hax")
        {
            replacements.Should().ContainSingle().Which.Should().BeSameAs(replacement);
        }
        else
        {
            replacements.Should().BeEmpty();
        }

        var snackbar = ctx.Services.GetRequiredService<ISnackbar>().ShownSnackbars.Should().ContainSingle().Subject;
        snackbar.Severity.Should().Be(kind.StartsWith("success", StringComparison.Ordinal)
            ? MudBlazor.Severity.Success
            : MudBlazor.Severity.Warning);
        snackbar.Message.Should().Contain(kind switch
        {
            "success" => "Legalized successfully. Click Save",
            "success-with-changes" => "1 change. Click Save",
            "hax" => "Imported via HaX retry — illegal but populated.",
            "timeout" => "Timed out after 3s.",
            "cancelled" => "Cancelled.",
            _ => "No legal encounter."
        });
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Legalize")
            .HasAttribute("disabled").Should().BeFalse();
    }

    [Theory]
    [InlineData("success")]
    [InlineData("failure")]
    [InlineData("timeout")]
    [InlineData("hax")]
    public void GenerationRetainsPopulatedBestEffortResults(string kind)
    {
        var state = new TestAppState { SaveFile = new SAV9SV() };
        var pokemon = new PK9 { Species = (ushort)Species.Pikachu };
        var app = new AppService(state, new TestRefreshService(), new FixedLegalizationService(Outcome(kind, pokemon)));

        app.ConvertShowdownSetToPkm(new ShowdownSet("Pikachu")).Should().BeSameAs(pokemon);

        var blankApp = new AppService(state, new TestRefreshService(),
            new FixedLegalizationService(Outcome(kind, new PK9())));
        blankApp.ConvertShowdownSetToPkm(new ShowdownSet("Pikachu")).Should().BeNull();
    }

    [Theory]
    [InlineData("success")]
    [InlineData("failure")]
    [InlineData("timeout")]
    [InlineData("hax")]
    public async Task ReportBatchOnlyWritesSuccessResults(string kind)
    {
        var save = new SAV9SV();
        ParseSettings.InitFromSaveFileData(save);
        save.SetBoxSlotAtIndex(new PK9 { Species = (ushort)Species.Pikachu }, 0, 0, EntityImportSettings.None);
        var original = save.Data.ToArray();
        var state = new TestAppState { SaveFile = save };
        var refresh = new TestRefreshService();
        var service = new FixedLegalizationService(Outcome(kind, new PK9 { Species = (ushort)Species.Raichu }));
        var app = new AppService(state, refresh, service);
        await using var ctx = BunitTestHelpers.CreateBunitContext(state, refresh, app);
        ctx.Services.AddSingleton<ILegalizationService>(service);
        ctx.Render<MudPopoverProvider>();
        var cut = ctx.Render<LegalityReportTab>();
        await cut.FindAll("button").Single(b => b.TextContent.Trim() == "Run Legality Report")
            .ClickAsync(new MouseEventArgs());

        await cut.FindAll("button").Single(b => b.TextContent.Trim() == "Legalize All Illegal")
            .ClickAsync(new MouseEventArgs());

        service.LegalizeCalls.Should().Be(1);
        if (kind == "success")
        {
            save.GetBoxSlotAtIndex(0, 0).Species.Should().Be((ushort)Species.Raichu);
        }
        else
        {
            save.Data.ToArray().Should().Equal(original);
            ctx.Services.GetRequiredService<ISnackbar>().ShownSnackbars.Should()
                .Contain(s => s.Message != null && s.Message.Contains("could not fix: 1", StringComparison.Ordinal));
        }
    }

    private static LegalizationOutcome Outcome(string kind, PKM pokemon, LegalizationChanges? changes = null) => kind switch
    {
        "success" => new LegalizationSuccess(pokemon),
        "success-with-changes" => new LegalizationSuccess(pokemon) { Changes = changes! },
        "failure" => new LegalizationFailure(pokemon, "No legal encounter."),
        "cancelled" => new LegalizationFailure(pokemon, "Cancelled."),
        "timeout" => new LegalizationTimeout(pokemon, "Timed out after 3s."),
        "hax" => new LegalizationHaX(pokemon, "Imported via HaX retry — illegal but populated."),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private sealed class FixedLegalizationService(LegalizationOutcome outcome) : ILegalizationService
    {
        internal int LegalizeCalls { get; private set; }

        public Task<LegalizationOutcome> LegalizeAsync(PKM pk, SaveFile sav, IProgress<string>? progress = null,
            CancellationToken ct = default, int? timeoutSeconds = null)
        {
            LegalizeCalls++;
            return Task.FromResult(outcome);
        }

        public Task<LegalizationOutcome> GenerateFromSetAsync(ShowdownSet set, SaveFile sav,
            IProgress<string>? progress = null, CancellationToken ct = default) => Task.FromResult(outcome);

        public LegalizationOutcome GenerateFromSetSync(ShowdownSet set, SaveFile sav) => outcome;
    }
}

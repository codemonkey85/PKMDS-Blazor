namespace Pkmds.Tests;

/// <summary>
/// bUnit component tests for <see cref="LegalityTab" />.
/// </summary>
public class LegalityTabTests
{
    [Fact]
    public async Task LegalityTab_LegalPokemon_RendersSuccessAlert()
    {
        var (saveFile, appState, refreshService, appService) = BunitTestHelpers.LoadSave("Black - Full Completion.sav");
        await using var ctx = BunitTestHelpers.CreateBunitContext(appState, refreshService, appService);

        var pkm = saveFile.GetPartySlotAtIndex(0);
        pkm.Species.Should().BeGreaterThan(0);

        var la = appService.GetLegalityAnalysis(pkm);

        var cut = ctx.Render<LegalityTab>(p => p
            .Add(c => c.Pokemon, pkm)
            .Add(c => c.Analysis, la));

        cut.Markup.Should().Contain("mud-alert", "a MudAlert must be rendered for any Pokémon with an analysis");
        cut.Markup.Should().Contain("success", "a legal Pokémon must produce a success-colored alert");
    }

    [Fact]
    public async Task LegalityTab_IllegalPokemon_RendersErrorAlertAndIssueList()
    {
        var (saveFile, appState, refreshService, appService) = BunitTestHelpers.LoadSave("Black - Full Completion.sav");
        await using var ctx = BunitTestHelpers.CreateBunitContext(appState, refreshService, appService);

        var pkm = saveFile.GetPartySlotAtIndex(0);
        pkm.Ability = 0;
        pkm.RefreshChecksum();

        var la = appService.GetLegalityAnalysis(pkm);

        var cut = ctx.Render<LegalityTab>(p => p
            .Add(c => c.Pokemon, pkm)
            .Add(c => c.Analysis, la));

        cut.Markup.Should().Contain("mud-alert", "a MudAlert must be rendered");
        cut.Markup.Should().Contain("error", "an illegal Pokémon must produce an error-colored alert");
        cut.Markup.Should().Contain("Issues", "the issue list header must appear when there are legality problems");
    }

    [Fact]
    public async Task LegalityTab_NullPokemon_DoesNotThrow()
    {
        var (saveFile, appState, refreshService, appService) = BunitTestHelpers.LoadSave("Black - Full Completion.sav");
        await using var ctx = BunitTestHelpers.CreateBunitContext(appState, refreshService, appService);

        var act = () => ctx.Render<LegalityTab>(p => p
            .Add(c => c.Pokemon, null)
            .Add(c => c.Analysis, null));

        act.Should().NotThrow("rendering LegalityTab with Pokemon=null must not throw");
    }

    [Fact]
    public async Task LegalityTab_SpeciesZero_RendersNoErrorIndicators()
    {
        var (saveFile, appState, refreshService, appService) = BunitTestHelpers.LoadSave("Black - Full Completion.sav");
        await using var ctx = BunitTestHelpers.CreateBunitContext(appState, refreshService, appService);

        var blank = saveFile.BlankPKM;
        blank.Species.Should().Be(0, "BlankPKM must have Species 0");

        var la = appService.GetLegalityAnalysis(blank);

        var cut = ctx.Render<LegalityTab>(p => p
            .Add(c => c.Pokemon, blank)
            .Add(c => c.Analysis, la));

        // The component suppresses legality UI for a blank slot even though PKHeX reports it invalid.
        cut.Markup.Should().NotContain("mud-alert-filled-error",
            "a Species-0 blank slot should not produce an error alert");
    }
}

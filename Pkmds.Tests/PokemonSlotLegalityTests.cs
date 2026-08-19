using Microsoft.AspNetCore.Components;

namespace Pkmds.Tests;

/// <summary>
/// bUnit component tests for the legality overlay rendered by <see cref="PokemonSlotComponent" />.
/// </summary>
public class PokemonSlotLegalityTests
{
    [Fact]
    public async Task PokemonSlotComponent_LegalPokemon_RendersValidOverlay()
    {
        var (saveFile, appState, refreshService, appService) = BunitTestHelpers.LoadSave("Black - Full Completion.sav");
        await using var ctx = BunitTestHelpers.CreateBunitContext(appState, refreshService, appService);

        var pkm = saveFile.GetPartySlotAtIndex(0);
        pkm.Species.Should().BeGreaterThan(0, "need a real Pokémon for the legality overlay to appear");

        var cut = ctx.Render<PokemonSlotComponent>(p => p
            .Add(c => c.SlotNumber, 0)
            .Add(c => c.Pokemon, pkm)
            .Add(c => c.OnSlotClick, EventCallback.Empty)
            .Add(c => c.GetClassFunction, () => string.Empty));

        cut.Markup.Should().Contain("legality-indicator-icon--legal",
            "a legal Pokémon must render the legal SVG indicator");
    }

    [Fact]
    public async Task PokemonSlotComponent_IllegalPokemon_RendersWarnOverlay()
    {
        var (saveFile, appState, refreshService, appService) = BunitTestHelpers.LoadSave("Black - Full Completion.sav");
        await using var ctx = BunitTestHelpers.CreateBunitContext(appState, refreshService, appService);

        var pkm = saveFile.GetPartySlotAtIndex(0);
        pkm.Ability = 0;
        pkm.RefreshChecksum();

        var cut = ctx.Render<PokemonSlotComponent>(p => p
            .Add(c => c.SlotNumber, 0)
            .Add(c => c.Pokemon, pkm)
            .Add(c => c.OnSlotClick, EventCallback.Empty)
            .Add(c => c.GetClassFunction, () => string.Empty));

        cut.Markup.Should().Contain("legality-indicator-icon--illegal",
            "an illegal Pokémon must render the illegal SVG indicator");
    }

    [Fact]
    public async Task PokemonSlotComponent_EmptySlot_RendersNoLegalityOverlay()
    {
        var (saveFile, appState, refreshService, appService) = BunitTestHelpers.LoadSave("Black - Full Completion.sav");
        await using var ctx = BunitTestHelpers.CreateBunitContext(appState, refreshService, appService);

        var blank = saveFile.BlankPKM;
        blank.Species.Should().Be(0, "BlankPKM must have Species 0");

        var cut = ctx.Render<PokemonSlotComponent>(p => p
            .Add(c => c.SlotNumber, 0)
            .Add(c => c.Pokemon, blank)
            .Add(c => c.OnSlotClick, EventCallback.Empty)
            .Add(c => c.GetClassFunction, () => string.Empty));

        cut.Markup.Should().NotContain("legality-indicator-icon",
            "an empty slot should not show a legality overlay");
    }
}

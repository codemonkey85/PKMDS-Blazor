using Microsoft.AspNetCore.Components;

namespace Pkmds.Tests;

/// <summary>
/// Regression tests covering the PartyCount clamp in <see cref="PokemonSlotComponent" />.
/// SAV3.PartyCount reads a single raw byte from the save block; a corrupt or unusual GBA save
/// can report &gt; 6, which without clamping would slice past the party buffer in
/// <c>GetPartySlotAtIndex</c> and throw <see cref="ArgumentOutOfRangeException" /> during render.
/// See issue #844.
/// </summary>
public class PokemonSlotPartyCountTests
{
    [Fact]
    public void PokemonSlotComponent_PartySlot_CorruptGen3PartyCount_RendersWithoutThrowing()
    {
        var (saveFile, appState, refreshService, appService) =
            BunitTestHelpers.LoadSave("POKEMON EMER_BPEE-0.sav");

        var sav3 = saveFile.Should().BeAssignableTo<SAV3>().Subject;
        sav3.LargeBlock.PartyCount = 99; // force a corrupt count > the canonical 6-slot maximum

        var pkm = saveFile.BlankPKM;
        pkm.Species = (ushort)Species.Bulbasaur;

        using var ctx = BunitTestHelpers.CreateBunitContext(appState, refreshService, appService);

        var render = () => ctx.Render<PokemonSlotComponent>(p => p
            .Add(c => c.SlotNumber, 0)
            .Add(c => c.Pokemon, pkm)
            .Add(c => c.IsPartySlot, true)
            .Add(c => c.OnSlotClick, EventCallback.Empty)
            .Add(c => c.GetClassFunction, () => string.Empty));

        render.Should().NotThrow(
            "GetBattleReadyCount must clamp PartyCount to MaxPartyCount so corrupt Gen3 saves don't slice past the party buffer");
    }
}

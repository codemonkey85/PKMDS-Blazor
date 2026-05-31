namespace Pkmds.Tests;

/// <summary>
/// Regression tests for LGPE (SAV7b) party handling. Let's Go stores the party as a pointer list
/// into unified storage; an empty slot holds the SLOT_EMPTY sentinel, and reading or writing such
/// a slot via the index API throws <see cref="ArgumentOutOfRangeException"/>. Saves in the wild
/// (emulator / JKSM dumps) can report a <see cref="SaveFile.PartyCount"/> larger than the number of
/// populated pointer slots, which crashed the party grid, the editor save path, and CompactParty
/// (issues #942–#948). The safe extension helpers must tolerate that state instead of throwing.
/// </summary>
public class LgpePartySafetyTests
{
    /// <summary>
    /// Builds a Let's Go save whose reported party count exceeds the number of populated pointer
    /// slots — the malformed state of the user-reported saves. The pointers stay at SLOT_EMPTY, so
    /// every reported slot throws when read/written through PKHeX's index API.
    /// </summary>
    private static SAV7b CreateLgpeWithOverReportedParty(int reportedCount)
    {
        var sav = new SAV7b();
        sav.Storage.PartyCount = reportedCount;
        return sav;
    }

    [Fact]
    public void RawGetPartySlotAtIndex_PhantomSlot_Throws()
    {
        // Documents the underlying PKHeX behavior the safe helpers shield callers from.
        var sav = CreateLgpeWithOverReportedParty(2);

        Action act = () => sav.GetPartySlotAtIndex(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TryGetPartySlot_PhantomSlot_ReturnsNullInsteadOfThrowing()
    {
        var sav = CreateLgpeWithOverReportedParty(2);

        sav.TryGetPartySlot(0).Should().BeNull();
        sav.TryGetPartySlot(5).Should().BeNull();
    }

    [Fact]
    public void GetSafePartyCount_NoPopulatedSlots_ReturnsZero()
    {
        var sav = CreateLgpeWithOverReportedParty(6);

        sav.GetSafePartyCount().Should().Be(0);
    }

    [Fact]
    public void CompactParty_Lgpe_DoesNotThrow()
    {
        var sav = CreateLgpeWithOverReportedParty(6);

        Action act = sav.CompactParty;

        act.Should().NotThrow();
    }

    [Fact]
    public void TrySetPartySlot_PhantomSlot_ReturnsFalseAndDoesNotThrow()
    {
        var sav = CreateLgpeWithOverReportedParty(2);
        var pkm = new PB7 { Species = (ushort)Species.Pikachu };

        var result = true;
        Action act = () => result = sav.TrySetPartySlot(pkm, 1);

        act.Should().NotThrow();
        result.Should().BeFalse();
    }

    [Fact]
    public void GetSafePartyCount_NonLgpeSave_EqualsPartyCount()
    {
        var sav = new SAV8SWSH();

        sav.GetSafePartyCount().Should().Be(sav.PartyCount);
    }
}

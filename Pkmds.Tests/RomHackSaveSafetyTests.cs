namespace Pkmds.Tests;

/// <summary>
/// Regression tests for ROM-hack saves that reuse a vanilla layout but corrupt fixed-size
/// invariants. The SAV3-based Pokémon Unbound is detected as a valid <c>SAV3E</c>
/// (<see cref="SaveState.Exportable"/> is <see langword="true"/>) yet writes an out-of-range value
/// into the single-byte party-count field at <c>0x234</c>. The party buffer only holds 6 slots, so
/// reading any reported slot past the sixth throws <see cref="ArgumentOutOfRangeException"/> — which
/// crashed the storage component on load (issue #1003). The save must be rejected at import, and the
/// safe helpers must tolerate the over-reported count if they are reached anyway.
/// </summary>
public class RomHackSaveSafetyTests
{
    // The real Pokémon Unbound save from issue #1003 reported a party count of 187.
    private const int OverReportedPartyCount = 187;

    private static SAV3E CreateEmeraldWithOverReportedParty(int reportedCount)
    {
        var sav = new SAV3E();
        sav.LargeBlock.PartyCount = (byte)reportedCount;
        return sav;
    }

    [Fact]
    public void RawGetPartySlotAtIndex_PastBuffer_Throws()
    {
        // Documents the underlying PKHeX behavior the import guard and safe helpers shield against.
        var sav = CreateEmeraldWithOverReportedParty(OverReportedPartyCount);

        Action act = () => sav.GetPartySlotAtIndex(6);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void IsSupportedForEditing_OverReportedParty_RejectsWithReason()
    {
        var sav = CreateEmeraldWithOverReportedParty(OverReportedPartyCount);

        var supported = sav.IsSupportedForEditing(out var reason);

        supported.Should().BeFalse();
        reason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void IsSupportedForEditing_VanillaSave_IsSupported()
    {
        var sav = new SAV3E();

        var supported = sav.IsSupportedForEditing(out var reason);

        supported.Should().BeTrue();
        reason.Should().BeNull();
    }

    [Fact]
    public void GetSafePartyCount_OverReportedParty_ClampsToMax()
    {
        var sav = CreateEmeraldWithOverReportedParty(OverReportedPartyCount);

        sav.GetSafePartyCount().Should().Be(6);
    }

    [Fact]
    public void GetSafePartyCount_VanillaSave_EqualsPartyCount()
    {
        var sav = new SAV3E();

        sav.GetSafePartyCount().Should().Be(sav.PartyCount);
    }
}

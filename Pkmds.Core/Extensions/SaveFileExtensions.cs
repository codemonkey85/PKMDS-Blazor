namespace Pkmds.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="SaveFile"/> that deal with storage-slot layout invariants.
/// </summary>
public static class SaveFileExtensions
{
    private const int PartySize = 6;

    /// <summary>
    /// Rewrites the party so all non-blank members are contiguous at indices 0..N-1, with the
    /// remaining slots blanked. Use after any party write that could have left a gap (e.g. writing
    /// to an index past <see cref="SaveFile.PartyCount"/> or deleting a middle slot).
    /// </summary>
    /// <remarks>
    /// Mirrors the invariant PKHeX's <c>SlotInfoParty.WriteTo</c> enforces by realigning writes to
    /// <c>Math.Min(slot, PartyCount)</c> — but applied post-hoc so callers don't need to compute
    /// the realigned slot themselves.
    /// </remarks>
    public static void CompactParty(this SaveFile sav)
    {
        // LGPE (SAV7b) stores the party as a pointer list into unified storage, maintained by
        // PKHeX itself — there are no interstitial gaps to remove. Such saves can also report a
        // PartyCount larger than the number of populated pointers; reading or writing a slot whose
        // pointer is the SLOT_EMPTY sentinel throws ArgumentOutOfRangeException (issues #942–#948).
        // Nothing to compact, so leave it alone.
        if (sav is SAV7b)
        {
            return;
        }

        var nonBlank = new List<PKM>(PartySize);
        for (var i = 0; i < PartySize; i++)
        {
            var pkm = sav.GetPartySlotAtIndex(i);
            if (pkm.Species != 0)
            {
                nonBlank.Add(pkm);
            }
        }

        for (var i = 0; i < nonBlank.Count; i++)
        {
            sav.SetPartySlotAtIndex(nonBlank[i], i);
        }

        for (var i = nonBlank.Count; i < PartySize; i++)
        {
            sav.SetPartySlotAtIndex(sav.BlankPKM, i);
        }
    }

    /// <summary>
    /// Reads a party slot, returning <see langword="null" /> instead of throwing when the slot
    /// cannot be read. Mainline saves return a blank Pokémon for empty slots, but LGPE (SAV7b)
    /// stores the party as a pointer list and throws <see cref="ArgumentOutOfRangeException" />
    /// when the pointer is the SLOT_EMPTY sentinel — which happens on saves whose party-count
    /// field over-reports the number of populated slots (issues #942–#948).
    /// </summary>
    public static PKM? TryGetPartySlot(this SaveFile sav, int index)
    {
        if (index < 0 || index >= PartySize)
        {
            return null;
        }

        try
        {
            return sav.GetPartySlotAtIndex(index);
        }
        catch (ArgumentOutOfRangeException)
        {
            // LGPE pointer-list slot points at the SLOT_EMPTY sentinel; treat as no Pokémon.
            return null;
        }
    }

    /// <summary>
    /// The number of leading party slots that can actually be read without throwing. The stored
    /// party count is never trusted as-is: it is clamped to the 6-slot physical maximum first.
    /// On mainline saves the count cannot legitimately exceed 6, but ROM hacks (e.g. SAV3-based
    /// Pokémon Unbound) write garbage into the single-byte party-count field, which would otherwise
    /// drive <see cref="SaveFile.GetPartySlotAtIndex"/> past the party buffer and throw
    /// <see cref="ArgumentOutOfRangeException"/> (issue #1003). For LGPE (SAV7b) the stored count can
    /// also exceed the number of populated pointers, so there this additionally walks the reported
    /// slots and stops at the first one that is unreadable or empty (issues #942–#948).
    /// </summary>
    public static int GetSafePartyCount(this SaveFile sav)
    {
        var reported = Math.Min(sav.PartyCount, PartySize);
        if (sav is not SAV7b)
        {
            return reported;
        }

        var count = 0;
        for (var i = 0; i < reported; i++)
        {
            if (sav.TryGetPartySlot(i) is not { Species: > 0 })
            {
                break;
            }

            count++;
        }

        return count;
    }

    /// <summary>
    /// Writes a Pokémon to a party slot, returning <see langword="false" /> instead of throwing
    /// when the slot cannot be written. On LGPE (SAV7b) a slot whose pointer is the SLOT_EMPTY
    /// sentinel cannot be written through the index API (PKHeX dereferences the pointer first and
    /// throws), so callers should treat <see langword="false" /> as "this slot is not writable".
    /// </summary>
    public static bool TrySetPartySlot(this SaveFile sav, PKM pokemon, int index)
    {
        if (index < 0 || index >= PartySize)
        {
            return false;
        }

        // On LGPE the slot must already point at a real storage entry; an empty pointer can't be
        // written via SetPartySlotAtIndex (it throws while resolving the offset).
        if (sav is SAV7b && sav.TryGetPartySlot(index) is not { Species: > 0 })
        {
            return false;
        }

        try
        {
            sav.SetPartySlotAtIndex(pokemon, index);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            // Defensive: a malformed pointer list could still resolve to an out-of-range offset
            // even after the guard above. Honor the "never throw" contract and report failure.
            return false;
        }
    }

    /// <summary>
    /// For Gen 1/2 saves — whose box storage is a packed list, not a grid — collects non-blank
    /// slots in <paramref name="box"/> and rewrites them contiguously starting at slot 0. No-op
    /// for other generations, where gaps between box slots are valid.
    /// </summary>
    public static void CompactBoxIfGen12(this SaveFile sav, int box)
    {
        if (sav.Context is not (EntityContext.Gen1 or EntityContext.Gen2))
        {
            return;
        }

        var slotCount = sav.BoxSlotCount;
        var nonBlank = new List<PKM>(slotCount);
        for (var i = 0; i < slotCount; i++)
        {
            var pkm = sav.GetBoxSlotAtIndex(box, i);
            if (pkm.Species != 0)
            {
                nonBlank.Add(pkm);
            }
        }

        for (var i = 0; i < nonBlank.Count; i++)
        {
            sav.SetBoxSlotAtIndex(nonBlank[i], box, i);
        }

        for (var i = nonBlank.Count; i < slotCount; i++)
        {
            sav.SetBoxSlotAtIndex(sav.BlankPKM, box, i);
        }
    }
}

namespace Pkmds.Core.Utilities;

/// <summary>Generates classic PID/IV pairs and checks their encounter RNG requirements.</summary>
public static class ClassicPidIvGenerator
{
    public readonly record struct PidIvPair(uint Pid, uint Ivs);

    /// <summary>Finds every pair for the exact IV spread without a random seed search.</summary>
    public static PidIvPair[] GenerateForIVs(PIDType method, uint ivs)
    {
        if (method is not (PIDType.Method_1 or PIDType.Method_2 or PIDType.Method_4))
        {
            throw new ArgumentOutOfRangeException(nameof(method));
        }

        Span<uint> seeds = stackalloc uint[LCRNG.MaxCountSeedsIV];
        var first = (ivs & 0x7FFF) << 16;
        var second = (ivs >> 15 & 0x7FFF) << 16;
        var count = method == PIDType.Method_4
            ? LCRNGReversalSkip.GetSeedsIVs(seeds, first, second)
            : LCRNGReversal.GetSeedsIVs(seeds, first, second);
        var pairs = new PidIvPair[count];
        for (var i = 0; i < count; i++)
        {
            // Reversal returns the state just before the first IV call. Undo
            // the two PID calls, plus Method 2's intervening call.
            var origin = method == PIDType.Method_2 ? LCRNG.Prev3(seeds[i]) : LCRNG.Prev2(seeds[i]);
            pairs[i] = Generate(method, origin);
        }

        return pairs;
    }

    public static PidIvPair Generate(PIDType method, uint seed)
    {
        var pid = ClassicEraRNG.GetSequentialPID(ref seed);
        uint ivs;
        switch (method)
        {
            case PIDType.Method_1:
                ivs = ClassicEraRNG.GetSequentialIVs(ref seed);
                break;
            case PIDType.Method_2:
                LCRNG.Next16(ref seed);
                ivs = ClassicEraRNG.GetSequentialIVs(ref seed);
                break;
            case PIDType.Method_4:
                var iv1 = LCRNG.Next15(ref seed);
                LCRNG.Next16(ref seed);
                ivs = iv1 | LCRNG.Next15(ref seed) << 15;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(method));
        }

        return new PidIvPair(pid, ivs);
    }

    /// <summary>
    /// Checks PID/IV correlation and encounter frames on a temporary copy.
    /// Other legality checks are left to the caller; the original Pokémon is never modified.
    /// </summary>
    public static bool MatchesEncounterRng(PKM pokemon, PidIvPair pair)
    {
        var candidate = pokemon.Clone();
        candidate.PID = pair.Pid;
        candidate.SetIVs(pair.Ivs);
        var analysis = new LegalityAnalysis(candidate);
        return analysis.EncounterMatch is not EncounterInvalid
               && analysis.Info is { PIDParsed: true, PIDIVMatches: true, FrameMatches: true }
               && !analysis.Results.Any(r => r is { Valid: false, Identifier: CheckIdentifier.Encounter });
    }
}

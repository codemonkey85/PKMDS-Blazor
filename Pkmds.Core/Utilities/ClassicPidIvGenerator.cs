namespace Pkmds.Core.Utilities;

/// <summary>Generates classic PID/IV pairs and checks their encounter RNG requirements.</summary>
public static class ClassicPidIvGenerator
{
    public readonly record struct PidIvPair(uint Pid, uint Ivs);

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
               && analysis.Info.PIDParsed
               && analysis.Info.PIDIVMatches
               && analysis.Info.FrameMatches
               && !analysis.Results.Any(r => !r.Valid && r.Identifier == CheckIdentifier.Encounter);
    }
}

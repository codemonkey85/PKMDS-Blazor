namespace Pkmds.Rcl;

/// <summary>
/// Represents the legality status of a Pokémon as determined by the batch legality sweep.
/// </summary>
public enum LegalityStatus
{
    /// <summary>All checks passed — the Pokémon is fully legal.</summary>
    Legal,

    /// <summary>No invalid checks, but at least one suspicious (fishy) check was flagged.</summary>
    Fishy,

    /// <summary>At least one check returned an invalid result.</summary>
    Illegal
}

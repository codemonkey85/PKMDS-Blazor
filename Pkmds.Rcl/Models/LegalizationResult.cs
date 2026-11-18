namespace Pkmds.Rcl.Models;

/// <summary>
/// The result of a single legalization or generation attempt. Every case retains
/// its Pokémon: non-success cases can carry a populated best-effort result.
/// Editor callers may accept HaX results with a warning; batch callers accept only
/// <see cref="LegalizationSuccess" />.
/// </summary>
public closed record LegalizationOutcome(PKM Pokemon);

/// <summary>The Pokémon was successfully legalized.</summary>
public sealed record LegalizationSuccess(PKM Pokemon) : LegalizationOutcome(Pokemon)
{
    /// <summary>
    /// Field-level differences from the input Pokémon. Empty when generating
    /// from a set, where there is no starting Pokémon to compare.
    /// </summary>
    public LegalizationChanges Changes { get; init; } = LegalizationChanges.Empty;
}

/// <summary>No valid encounter could produce a legal result.</summary>
public sealed record LegalizationFailure(PKM Pokemon, string Reason) : LegalizationOutcome(Pokemon);

/// <summary>The attempt exceeded its time limit.</summary>
public sealed record LegalizationTimeout(PKM Pokemon, string Reason) : LegalizationOutcome(Pokemon);

/// <summary>
/// A relaxed HaX retry produced a populated but intentionally illegal Pokémon.
/// Surface its warning rather than treating it as a legal success.
/// </summary>
public sealed record LegalizationHaX(PKM Pokemon, string Warning) : LegalizationOutcome(Pokemon);

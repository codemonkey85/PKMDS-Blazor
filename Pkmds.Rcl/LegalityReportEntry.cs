namespace Pkmds.Rcl;

/// <summary>
/// Represents a single row in the batch legality report, describing the legality
/// status of one Pokémon slot (party or box).
/// </summary>
public sealed record LegalityReportEntry
{
    /// <summary>Gets the Pokémon instance.</summary>
    public required PKM Pokemon { get; init; }

    /// <summary>Gets the localised species name (e.g. "Pikachu").</summary>
    public required string SpeciesName { get; init; }

    /// <summary>Gets a human-readable slot description (e.g. "Party 1" or "Box 3, Slot 7").</summary>
    public required string Location { get; init; }

    /// <summary>Gets the overall legality status of this Pokémon.</summary>
    public required LegalityStatus Status { get; init; }

    /// <summary>Gets the comment text of the first failing check, or an empty string when legal.</summary>
    public required string FirstIssue { get; init; }

    /// <summary>Gets whether the slot is a party slot (<see langword="true" />) or a box slot (<see langword="false" />).</summary>
    public required bool IsParty { get; init; }

    /// <summary>Gets the 0-based slot index within the party or box.</summary>
    public required int SlotNumber { get; init; }

    /// <summary>Gets the 0-based box number. Only meaningful when <see cref="IsParty" /> is <see langword="false" />.</summary>
    public int BoxNumber { get; init; }
}

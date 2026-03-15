namespace Pkmds.Rcl.Models;

/// <summary>
///     Represents a single row in the Advanced Search results table.
/// </summary>
public sealed record AdvancedSearchResult
{
    /// <summary>The matched Pokémon instance (read directly from the save file).</summary>
    public required PKM Pokemon { get; init; }

    /// <summary>Localised species name (e.g., "Pikachu").</summary>
    public required string SpeciesName { get; init; }

    /// <summary>Human-readable slot description (e.g., "Party 1" or "Box 3, Slot 5").</summary>
    public required string Location { get; init; }

    /// <summary><see langword="true" /> if the slot is a party slot; <see langword="false" /> for box slots.</summary>
    public bool IsParty { get; init; }

    /// <summary>0-based box number. Meaningful only when <see cref="IsParty" /> is <see langword="false" />.</summary>
    public int BoxNumber { get; init; }

    /// <summary>0-based slot index within the party or box.</summary>
    public int SlotNumber { get; init; }
}

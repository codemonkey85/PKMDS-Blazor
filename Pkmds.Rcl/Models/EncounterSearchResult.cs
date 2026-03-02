namespace Pkmds.Rcl.Models;

/// <summary>
/// Represents a single encounter result row in the Encounter Database.
/// </summary>
public sealed record EncounterSearchResult
{
    /// <summary>The underlying PKHeX.Core encounter template.</summary>
    public required IEncounterable Encounter { get; init; }

    /// <summary>Localized species name.</summary>
    public required string SpeciesName { get; init; }

    /// <summary>Game name (e.g., "Red", "Scarlet").</summary>
    public required string GameName { get; init; }

    /// <summary>Human-readable encounter type (e.g., "Wild", "Static", "Gift", "Trade", "Egg", "Mystery Gift").</summary>
    public required string EncounterTypeName { get; init; }

    /// <summary>Level range string (e.g., "Lv. 5–10" or "Lv. 15").</summary>
    public required string LevelRange { get; init; }

    /// <summary><see langword="true"/> if the encounter is shiny-locked (Shiny.Never).</summary>
    public bool IsShinyLocked { get; init; }

    /// <summary>Human-readable location name, or <see langword="null"/> when no location applies.</summary>
    public string? Location { get; init; }
}

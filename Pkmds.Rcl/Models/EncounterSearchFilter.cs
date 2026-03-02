namespace Pkmds.Rcl.Models;

/// <summary>
/// Holds all optional filter criteria for the Encounter Database feature.
/// Every field is nullable — a missing value means "any".
/// </summary>
public sealed record EncounterSearchFilter
{
    /// <summary>Species ID to search encounters for. Required — search is skipped when null.</summary>
    public ushort? Species { get; init; }

    /// <summary>Form index to match exactly, or <see langword="null"/> for any form.</summary>
    public byte? Form { get; init; }

    /// <summary>
    /// Specific game version to search, or <see langword="null"/> to search all versions
    /// compatible with the current save file's format.
    /// </summary>
    public GameVersion? Version { get; init; }

    /// <summary>Minimum encounter level (inclusive), or <see langword="null"/> for no lower bound.</summary>
    public byte? LevelMin { get; init; }

    /// <summary>Maximum encounter level (inclusive), or <see langword="null"/> for no upper bound.</summary>
    public byte? LevelMax { get; init; }

    /// <summary>
    /// Shiny lock filter:
    /// <see langword="true"/> = shiny-locked encounters only (Shiny.Never),
    /// <see langword="false"/> = encounters that can be shiny,
    /// <see langword="null"/> = any.
    /// </summary>
    public bool? IsShinyLocked { get; init; }

    /// <summary>Encounter type group filter, or <see langword="null"/> for all types.</summary>
    public EncounterTypeGroup? EncounterGroup { get; init; }
}

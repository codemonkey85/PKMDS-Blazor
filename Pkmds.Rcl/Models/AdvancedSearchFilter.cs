namespace Pkmds.Rcl.Models;

/// <summary>
///     Holds all optional filter criteria for the Advanced Search feature.
///     Every field is nullable or an empty collection — a missing value means "any".
/// </summary>
public sealed record AdvancedSearchFilter
{
    // ── Basic ─────────────────────────────────────────────────────────────

    /// <summary>Species ID to match exactly, or <see langword="null" /> for any.</summary>
    public ushort? Species { get; init; }

    /// <summary>Form index to match exactly, or <see langword="null" /> for any.</summary>
    public byte? Form { get; init; }

    /// <summary>
    ///     <see langword="true" /> = shiny only, <see langword="false" /> = non-shiny only,
    ///     <see langword="null" /> = any.
    /// </summary>
    public bool? IsShiny { get; init; }

    /// <summary>
    ///     <see langword="true" /> = eggs only, <see langword="false" /> = non-eggs only,
    ///     <see langword="null" /> = any.
    /// </summary>
    public bool? IsEgg { get; init; }

    /// <summary>
    ///     Gender filter: 0 = male, 1 = female, -1 = genderless, <see langword="null" /> = any.
    /// </summary>
    public int? Gender { get; init; }

    /// <summary>Nature index (0–24), or <see langword="null" /> for any.</summary>
    public byte? Nature { get; init; }

    /// <summary>Ability ID, or <see langword="null" /> for any.</summary>
    public int? Ability { get; init; }

    /// <summary>Held item ID, or <see langword="null" /> for any.</summary>
    public int? HeldItem { get; init; }

    /// <summary>Ball ID, or <see langword="null" /> for any.</summary>
    public int? Ball { get; init; }

    /// <summary>Origin game version, or <see langword="null" /> for any.</summary>
    public GameVersion? OriginGame { get; init; }

    /// <summary>
    ///     <see langword="true" /> = legal only (no Invalid checks), <see langword="false" /> = illegal only,
    ///     <see langword="null" /> = any. Evaluated last due to cost.
    /// </summary>
    public bool? IsLegal { get; init; }

    // ── Trainer ───────────────────────────────────────────────────────────

    /// <summary>
    ///     Substring filter for the OT name (case-insensitive),
    ///     or <see langword="null" /> to skip.
    /// </summary>
    public string? OriginalTrainerName { get; init; }

    /// <summary>Trainer ID (16-bit), or <see langword="null" /> for any.</summary>
    public uint? TrainerId { get; init; }

    /// <summary>Language ID, or <see langword="null" /> for any.</summary>
    public int? LanguageId { get; init; }

    // ── Level ─────────────────────────────────────────────────────────────

    /// <summary>Minimum current level (inclusive), or <see langword="null" /> for no lower bound.</summary>
    public byte? LevelMin { get; init; }

    /// <summary>Maximum current level (inclusive), or <see langword="null" /> for no upper bound.</summary>
    public byte? LevelMax { get; init; }

    // ── IVs — per-stat floor values (0–31) ────────────────────────────────

    public int? HpIvMin { get; init; }
    public int? AtkIvMin { get; init; }
    public int? DefIvMin { get; init; }
    public int? SpaIvMin { get; init; }
    public int? SpdIvMin { get; init; }
    public int? SpeIvMin { get; init; }

    // ── EVs — per-stat floor values (0–252) ───────────────────────────────

    public int? HpEvMin { get; init; }
    public int? AtkEvMin { get; init; }
    public int? DefEvMin { get; init; }
    public int? SpaEvMin { get; init; }
    public int? SpdEvMin { get; init; }
    public int? SpeEvMin { get; init; }

    // ── Moves ─────────────────────────────────────────────────────────────

    /// <summary>
    ///     Pokémon must know <em>at least one</em> of these move IDs (OR logic).
    ///     Empty list = skip check.
    /// </summary>
    public IReadOnlyList<ushort> AnyMoves { get; init; } = [];

    /// <summary>
    ///     Pokémon must know <em>all</em> of these move IDs (AND logic).
    ///     Empty list = skip check.
    /// </summary>
    public IReadOnlyList<ushort> AllMoves { get; init; } = [];

    // ── Hidden Power type (0–15) ──────────────────────────────────────────

    /// <summary>Required Hidden Power type index (0=Fighting … 15=Dark), or <see langword="null" /> for any.</summary>
    public int? HiddenPowerType { get; init; }

    // ── Ribbons / Marks ───────────────────────────────────────────────────

    /// <summary>
    ///     All named ribbon/mark properties must be <see langword="true" />.
    ///     Uses the PKHeX property name (e.g., <c>"RibbonChampionG3Hoenn"</c>).
    ///     Empty list = skip check.
    /// </summary>
    public IReadOnlyList<string> RequiredRibbons { get; init; } = [];
}

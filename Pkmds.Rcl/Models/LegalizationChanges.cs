namespace Pkmds.Rcl.Models;

/// <summary>
/// Buckets a single field-level change reported by <see cref="LegalizationOutcome.Changes" />.
/// Used to group rows in the change-summary dialog.
/// </summary>
public enum LegalizationChangeCategory
{
    /// <summary>Species, Form, Nickname, Gender, Language.</summary>
    Identity,

    /// <summary>Ball, met / egg location, met level / date, OT, TID/SID, version.</summary>
    Origin,

    /// <summary>Moves, relearn moves, held item, ability, Tera type.</summary>
    Battle,

    /// <summary>IVs, EVs, AVs/GVs, nature, stat alignment, Hyper Training flags.</summary>
    Stats,

    /// <summary>Shininess, friendship, markings, ribbon delta.</summary>
    Cosmetic,

    /// <summary>PID, encryption constant.</summary>
    Internal
}

/// <summary>
/// A single field-level diff between the original Pokémon and its legalized
/// counterpart.
/// </summary>
/// <param name="Category">Which group this change belongs to in the UI.</param>
/// <param name="FieldLabel">Human-readable field name (e.g. "Move 1", "HP IV").</param>
/// <param name="OldValue">Pre-legalization value, formatted for display.</param>
/// <param name="NewValue">Post-legalization value, formatted for display.</param>
public readonly record struct LegalizationChange(
    LegalizationChangeCategory Category,
    string FieldLabel,
    string? OldValue,
    string? NewValue);

/// <summary>
/// The set of field-level differences between the input and output of a successful
/// legalization. Empty for failures, timeouts, and Showdown-set generation (no
/// "before" exists).
/// </summary>
public sealed record LegalizationChanges(IReadOnlyList<LegalizationChange> Changes)
{
    /// <summary>Convenience instance returned when nothing changed.</summary>
    public static LegalizationChanges Empty { get; } = new([]);

    /// <summary>Total number of field changes across all categories.</summary>
    public int Count => Changes.Count;

    /// <summary>Whether there are any changes to report.</summary>
    public bool IsEmpty => Changes.Count == 0;

    /// <summary>Groups the changes by category, preserving insertion order within each group.</summary>
    public IEnumerable<IGrouping<LegalizationChangeCategory, LegalizationChange>> ByCategory() =>
        Changes.GroupBy(c => c.Category);
}

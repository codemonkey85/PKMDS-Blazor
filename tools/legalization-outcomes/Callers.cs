namespace Pkmds.Tools.LegalizationOutcomes;

// Projections of the real callers, without browser/DI side effects. Returning a
// decision keeps the switch expression exhaustive; a switch statement alone
// doesn't provide the same missing-case check.
internal readonly record struct EditorDecision(PKM? Replacement, LegalizationChanges? Changes, string? Warning);

internal static class Callers
{
    internal const string FailureWarning = "Could not find a legal encounter.";
    internal const string TimeoutWarning = "Legalization timed out.";
    internal const string HaXWarning = "Imported via HaX retry — illegal but populated.";

    internal static EditorDecision Editor(BaselineOutcome result) => result.Status switch
    {
        BaselineStatus.Success => new(result.Pokemon, result.Changes, null),
        BaselineStatus.Failed => new(null, null, result.FailureReason ?? FailureWarning),
        BaselineStatus.Timeout => new(null, null, result.FailureReason ?? TimeoutWarning),
        BaselineStatus.SuccessHaX => new(result.Pokemon, null, result.FailureReason ?? HaXWarning),
        _ => throw new ArgumentOutOfRangeException(nameof(result))
    };

    internal static EditorDecision Editor(UnionOutcome result) => result switch
    {
        Succeeded success => new(success.Pokemon, success.Changes, null),
        Failed failure => new(null, null, failure.Reason),
        TimedOut timeout => new(null, null, timeout.Reason),
        ImportedViaHaX hax => new(hax.Pokemon, null, hax.Warning),
        // Generated unions have null contents when default-initialized, even
        // with non-nullable cases. Don't silently treat that as legalization failure.
        null => throw new InvalidOperationException("Uninitialized legalization outcome.")
    };

    internal static EditorDecision Editor(LegalizationOutcome result) => result switch
    {
        LegalizationSuccess success => new(success.Pokemon, success.Changes, null),
        LegalizationFailure failure => new(null, null, failure.Reason),
        LegalizationTimeout timeout => new(null, null, timeout.Reason),
        LegalizationHaX hax => new(hax.Pokemon, null, hax.Warning)
    };

    // Storage and report batches accept only legal success. HaX must never be
    // counted as a successful batch write, although the editor accepts its PKM.
    internal static PKM? Batch(BaselineOutcome result) =>
        result.Status == BaselineStatus.Success ? result.Pokemon : null;

    internal static PKM? Batch(UnionOutcome result) => result is Succeeded success ? success.Pokemon : null;

    internal static PKM? Batch(LegalizationOutcome result) => result is LegalizationSuccess success ? success.Pokemon : null;

    // AppService.ConvertShowdownSetToPkm intentionally accepts populated
    // best-effort results, including failure/timeout. Preserve that distinction.
    internal static PKM? Generated(BaselineOutcome result) => Populated(result.Pokemon);

    internal static PKM? Generated(UnionOutcome result) => result switch
    {
        Succeeded success => Populated(success.Pokemon),
        Failed failure => Populated(failure.Pokemon),
        TimedOut timeout => Populated(timeout.Pokemon),
        ImportedViaHaX hax => Populated(hax.Pokemon),
        null => throw new InvalidOperationException("Uninitialized legalization outcome.")
    };

    internal static PKM? Generated(LegalizationOutcome result) => Populated(result.Pokemon);

    private static PKM? Populated(PKM pokemon) => pokemon.Species > 0 ? pokemon : null;
}

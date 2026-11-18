namespace Pkmds.Tools.LegalizationOutcomes;

// Frozen pre-adoption model from 272a491e. Keep the historical comparison stable
// while the linked production source evolves; this is not consumed by the app.
internal enum BaselineStatus { Success, Failed, Timeout, SuccessHaX }

internal record BaselineOutcome(PKM Pokemon, BaselineStatus Status, string? FailureReason = null)
{
    public LegalizationChanges Changes { get; init; } = LegalizationChanges.Empty;
}

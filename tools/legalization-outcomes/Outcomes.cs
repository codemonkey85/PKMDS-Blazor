namespace Pkmds.Tools.LegalizationOutcomes;

// Neither candidate is consumed by the app. Each case keeps the original PKM
// reference; cloning it here would change ownership and dominate allocations.
public sealed record Succeeded(PKM Pokemon, LegalizationChanges Changes);
public sealed record Failed(PKM Pokemon, string Reason);
public sealed record TimedOut(PKM Pokemon, string Reason);
public sealed record ImportedViaHaX(PKM Pokemon, string Warning);

public union UnionOutcome(Succeeded, Failed, TimedOut, ImportedViaHaX);

public closed record ClosedOutcome(PKM Pokemon);
public sealed record ClosedSuccess(PKM Pokemon, LegalizationChanges Changes) : ClosedOutcome(Pokemon);
public sealed record ClosedFailure(PKM Pokemon, string Reason) : ClosedOutcome(Pokemon);
public sealed record ClosedTimeout(PKM Pokemon, string Reason) : ClosedOutcome(Pokemon);
public sealed record ClosedHaX(PKM Pokemon, string Warning) : ClosedOutcome(Pokemon);

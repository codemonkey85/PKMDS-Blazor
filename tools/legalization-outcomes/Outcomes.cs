namespace Pkmds.Tools.LegalizationOutcomes;

// The union is not consumed by the app. Each case keeps the original PKM
// reference; cloning it here would change ownership and dominate allocations.
public sealed record Succeeded(PKM Pokemon, LegalizationChanges Changes);
public sealed record Failed(PKM Pokemon, string Reason);
public sealed record TimedOut(PKM Pokemon, string Reason);
public sealed record ImportedViaHaX(PKM Pokemon, string Warning);

public union UnionOutcome(Succeeded, Failed, TimedOut, ImportedViaHaX);

// The closed hierarchy is now linked directly from the production model.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using Pkmds.Tools.LegalizationOutcomes;

// Model-only allocation experiment, not a legalization throughput benchmark.
// PKM, strings and changes are allocated once and shared by all representations.
var pokemon = new PK9 { Species = 25 };
var changes = new LegalizationChanges([
    new(LegalizationChangeCategory.Battle, "Move 1", "Tackle", "Thunder Shock")
]);
LegalizationOutcome[] fixtures =
[
    new(pokemon, LegalizationStatus.Success),
    new(pokemon, LegalizationStatus.Success) { Changes = changes },
    new(pokemon, LegalizationStatus.Failed, "No legal result found after 12 encounters."),
    new(pokemon, LegalizationStatus.Failed, "Cancelled."),
    new(pokemon, LegalizationStatus.Timeout, "Timed out after 3s (12 encounters tried)."),
    new(pokemon, LegalizationStatus.SuccessHaX, "HaX retry produced an illegal result."),
    new(pokemon, LegalizationStatus.Failed),
    new(pokemon, LegalizationStatus.Timeout),
    new(pokemon, LegalizationStatus.SuccessHaX),
    new(new PK9(), LegalizationStatus.Failed, "No species specified.")
];

// Validate outside measurement: identity, warnings, summaries, best-effort and
// blank PKM behavior. This doesn't exercise the engine or async cancellation.
foreach (var fixture in fixtures)
{
    var union = Factories.Union(fixture);
    var closed = Factories.Closed(fixture);
    if (Callers.Editor(fixture) != Callers.Editor(union) || Callers.Editor(fixture) != Callers.Editor(closed)
        || !ReferenceEquals(Callers.Batch(fixture), Callers.Batch(union))
        || !ReferenceEquals(Callers.Batch(fixture), Callers.Batch(closed))
        || !ReferenceEquals(Callers.Generated(fixture), Callers.Generated(union))
        || !ReferenceEquals(Callers.Generated(fixture), Callers.Generated(closed)))
    {
        throw new InvalidOperationException($"Caller mismatch for {fixture.Status} / {fixture.FailureReason}.");
    }
}

const int iterations = 100_000;
const int repetitions = 5;
var rows = new List<AllocationRow>();
for (var repetition = 0; repetition < repetitions; repetition++)
{
    foreach (var fixture in fixtures)
    {
        // Rotate ordering and warm every path. GC is never forced. No timing
        // claims: current-thread allocations exclude unrelated runtime threads.
        for (var warmup = 0; warmup < 1_000; warmup++)
        {
            Sinks.Baseline = Factories.Baseline(fixture);
            Sinks.Union = Factories.Union(fixture);
            Sinks.Closed = Factories.Closed(fixture);
        }

        for (var offset = 0; offset < 3; offset++)
        {
            var representation = (repetition + offset) % 3;
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var iteration = 0; iteration < iterations; iteration++)
            {
                switch (representation)
                {
                    case 0:
                        Sinks.Baseline = Factories.Baseline(fixture);
                        break;
                    case 1:
                        Sinks.Union = Factories.Union(fixture);
                        break;
                    case 2:
                        Sinks.Closed = Factories.Closed(fixture);
                        break;
                }
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            rows.Add(new(repetition + 1, fixture.Status.ToString(), fixture.FailureReason,
                fixture.Changes.Count, representation switch { 0 => "baseline", 1 => "union", _ => "closed" },
                (double)allocated / iterations));
        }
    }
}

Console.WriteLine(JsonSerializer.Serialize(new
{
    Runtime = RuntimeInformation.FrameworkDescription,
    OS = RuntimeInformation.OSDescription,
    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
    IterationsPerSample = iterations,
    Repetitions = repetitions,
    FixtureCount = fixtures.Length,
    CallerEquivalence = "passed",
    Scope = "CoreCLR model construction only; excludes PKM/diff/strings, engine, Task, browser and UI",
    Rows = rows
}, new JsonSerializerOptions { WriteIndented = true }));

internal sealed record AllocationRow(int Repetition, string Status, string? Reason, int Changes,
    string Representation, double BytesPerOutcome);

internal static class Sinks
{
    internal static LegalizationOutcome? Baseline;
    internal static UnionOutcome Union;
    internal static ClosedOutcome? Closed;
}

internal static class Factories
{
    // NoInlining plus escaping static sinks prevents the benchmark from
    // eliminating allocations whose objects would otherwise be unobserved.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static LegalizationOutcome Baseline(LegalizationOutcome fixture) =>
        new(fixture.Pokemon, fixture.Status, fixture.FailureReason) { Changes = fixture.Changes };

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static UnionOutcome Union(LegalizationOutcome fixture) => fixture.Status switch
    {
        LegalizationStatus.Success => new Succeeded(fixture.Pokemon, fixture.Changes),
        LegalizationStatus.Failed => new Failed(fixture.Pokemon, fixture.FailureReason ?? Callers.FailureWarning),
        LegalizationStatus.Timeout => new TimedOut(fixture.Pokemon, fixture.FailureReason ?? Callers.TimeoutWarning),
        LegalizationStatus.SuccessHaX => new ImportedViaHaX(fixture.Pokemon, fixture.FailureReason ?? Callers.HaXWarning),
        _ => throw new ArgumentOutOfRangeException(nameof(fixture))
    };

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static ClosedOutcome Closed(LegalizationOutcome fixture) => fixture.Status switch
    {
        LegalizationStatus.Success => new ClosedSuccess(fixture.Pokemon, fixture.Changes),
        LegalizationStatus.Failed => new ClosedFailure(fixture.Pokemon, fixture.FailureReason ?? Callers.FailureWarning),
        LegalizationStatus.Timeout => new ClosedTimeout(fixture.Pokemon, fixture.FailureReason ?? Callers.TimeoutWarning),
        LegalizationStatus.SuccessHaX => new ClosedHaX(fixture.Pokemon, fixture.FailureReason ?? Callers.HaXWarning),
        _ => throw new ArgumentOutOfRangeException(nameof(fixture))
    };
}

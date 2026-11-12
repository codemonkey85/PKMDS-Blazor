namespace Pkmds.Rcl.Models;

public sealed record BenchmarkResult
{
    public required string Name { get; init; }
    public required int Iterations { get; init; }
    public required int OpsPerIteration { get; init; }
    public required double TotalMs { get; init; }
    public required double MeanMs { get; init; }
    public required double MinMs { get; init; }
    public required double MaxMs { get; init; }
    public required double StdDevMs { get; init; }
    public double OpsPerSecond => TotalMs > 0 ? Iterations * OpsPerIteration / (TotalMs / 1000.0) : 0;
    public long? ManagedAllocatedBytes { get; init; }
    public int[]? CollectionsByGeneration { get; init; }
    public long? ManagedHeapBeforeBytes { get; init; }
    public long? ManagedHeapAfterBytes { get; init; }
    public double? MaxEventLoopDelayMs { get; init; }
    public int? EventLoopSamples { get; init; }
    public string? Notes { get; init; }
}

public sealed record BenchmarkReport
{
    public required DateTimeOffset Date { get; init; }
    public required string UserAgent { get; init; }
    public required IReadOnlyList<BenchmarkResult> Results { get; init; }
    public string Suite { get; init; } = "hotpaths";
    public string? Notes { get; init; }
    public Dictionary<string, string>? Fixtures { get; init; }
}

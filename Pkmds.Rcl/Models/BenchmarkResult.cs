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
    public double OpsPerSecond => Iterations * OpsPerIteration / (TotalMs / 1000.0);
}

public sealed record BenchmarkReport
{
    public required DateTimeOffset Date { get; init; }
    public required string UserAgent { get; init; }
    public required IReadOnlyList<BenchmarkResult> Results { get; init; }
}

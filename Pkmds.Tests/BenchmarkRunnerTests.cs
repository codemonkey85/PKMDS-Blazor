namespace Pkmds.Tests;

public class BenchmarkRunnerTests
{
    [Fact]
    public void Run_WithMinimalIterations_CompletesAllWorkloads()
    {
        // Exercise fixture generation too: older-game encounters cannot be cast to PK9.
        var report = new BenchmarkRunner().Run(new BenchmarkRunner.BenchmarkConfig(
            LegalityIterations: 1, EncryptionIterations: 1, SearchIterations: 1,
            EncounterIterations: 1, BatchLookupIterations: 1));

        report.Results.Should().HaveCount(5);
        report.Results.Should().AllSatisfy(result =>
        {
            result.Iterations.Should().Be(1);
            result.OpsPerIteration.Should().BeGreaterThan(0);
            double.IsFinite(result.TotalMs).Should().BeTrue();
        });
    }
}

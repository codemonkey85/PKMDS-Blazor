using System.IO.Compression;
using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Pkmds.Tests;

public class ApplicationBenchmarkTests
{
    [Fact]
    public async Task RunAsync_RefusedContext_DoesNotOpenBank()
    {
        using var context = new BunitContext();
        context.JSInterop.SetupModule("./js/benchmark.js")
            .SetupVoid("requireIsolatedContext")
            .SetException(new JSException("Existing database refused."));
        using var http = new HttpClient();
        var runner = new ApplicationBenchmarkRunner(http, context.Services.GetRequiredService<IJSRuntime>());

        var action = () => runner.RunAsync(new ApplicationBenchmarkRunner.Config(1, 3, 1));

        await action.Should().ThrowAsync<JSException>().WithMessage("Existing database refused.");
        context.JSInterop.Invocations.Should().NotContain(invocation =>
            invocation.Identifier == "import" && invocation.Arguments.Contains("./js/bank.js"));
    }

    [Fact]
    public void BankFixture_IsDeterministicAndRehydratesWithoutLosingMetadata()
    {
        var pokemon = ApplicationBenchmarkFixtures.CreatePokemon(6);
        var bytes = ApplicationBenchmarkFixtures.GetStoredBytes(pokemon);
        var payload = ApplicationBenchmarkFixtures.CreateBankImport(pokemon, bytes);
        var otherPokemon = ApplicationBenchmarkFixtures.CreatePokemon(6);
        ApplicationBenchmarkFixtures.CreateBankImport(otherPokemon, ApplicationBenchmarkFixtures.GetStoredBytes(otherPokemon))
            .Should().Equal(payload);
        bytes.Select(Convert.ToBase64String).Distinct().Should().HaveCount(6);
        var raw = JsonSerializer.Deserialize(payload, BankService.BankJsonContext.Default.RawEntryArray)!;
        var entries = raw.Select(entry =>
        {
            FileUtil.TryGetPKM(Convert.FromBase64String(entry.BytesBase64), out var pkm, entry.Meta.Ext).Should().BeTrue();
            return new BankEntry
            {
                Id = entry.Id,
                Pokemon = pkm!,
                SpeciesName = "fixture",
                Tag = entry.Meta.Tag,
                SourceSave = entry.Meta.SourceSave,
                AddedAt = DateTimeOffset.Parse(entry.AddedAt),
            };
        }).ToArray();

        ApplicationBenchmarkRunner.VerifyBankEntries(entries, bytes, 6, importedStart: 0);
        entries[0] = entries[0] with { Tag = "changed" };
        var verify = () => ApplicationBenchmarkRunner.VerifyBankEntries(entries, bytes, 6, importedStart: 0);
        verify.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ArchiveFixture_RoundTripsEditedSaveAndDetectsLostMetadata(bool manic)
    {
        var original = ApplicationBenchmarkFixtures.CreateSave(12345);
        var edited = ApplicationBenchmarkFixtures.CreateSave(23456);
        var fixture = ApplicationBenchmarkFixtures.CreateArchive(original, manic);
        SaveFileLoader.TryLoad(fixture, "benchmark.3ds.sav", out _, out var context).Should().BeTrue();
        var rebuilt = SaveArchiveHelper.RebuildZip(context!, edited);
        var kind = manic ? SaveArchiveKind.ManicEmu : SaveArchiveKind.GenericZip;
        ApplicationBenchmarkFixtures.VerifyArchive(rebuilt, edited, kind, 23456);

        using var output = new MemoryStream();
        output.Write(rebuilt);
        output.Position = 0;
        using (var archive = new ZipArchive(output, ZipArchiveMode.Update, leaveOpen: true))
            archive.GetEntry("metadata.txt")!.Delete();
        var verify = () => ApplicationBenchmarkFixtures.VerifyArchive(output.ToArray(), edited, kind, 23456);
        verify.Should().Throw<InvalidOperationException>();
    }
}

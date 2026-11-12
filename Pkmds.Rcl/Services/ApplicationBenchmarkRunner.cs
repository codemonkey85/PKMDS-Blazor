using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Pkmds.Rcl.Services;

/// <summary>
/// Opt-in application-boundary measurements. The browser bridge refuses normal
/// profiles before any Bank API is called. Writes append synthetic records.
/// </summary>
public sealed class ApplicationBenchmarkRunner(HttpClient http, IJSRuntime js)
{
    public sealed record Config(int Iterations = 3, int PokemonCount = 1000, int ArchiveIterations = 5);

    private const int WarmupIterations = 1;

    public async Task<BenchmarkReport> RunAsync(Config? config = null)
    {
        config ??= new Config();
        if (config.Iterations is < 1 or > 20 || config.PokemonCount is < 1 or > 1000 || config.ArchiveIterations is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(config));
        }

        await using var bridge = await js.InvokeAsync<IJSObjectReference>("import", "./js/benchmark.js");
        await bridge.InvokeVoidAsync("requireIsolatedContext");
        var measure = new Measurement(bridge);
        var results = new List<BenchmarkResult>();
        var fixtures = new Dictionary<string, string>();

        // New service instance per iteration: include all three HTTP reads, JSON
        // source-gen deserialization, item index construction, and first queries.
        string? descriptionSignature = null;
        MoveSummary? move = null;
        AbilitySummary? ability = null;
        ItemSummary? item = null;
        results.Add(await measure.RunAsync("description-first-load", config.Iterations, 3, async () =>
        {
            var descriptions = new DescriptionService(http, NullLogger<DescriptionService>.Instance);
            move = await descriptions.GetMoveInfoAsync(33, GameVersion.SL);
            ability = await descriptions.GetAbilityInfoAsync(65, GameVersion.SL);
            item = await descriptions.GetItemInfoAsync("Master Ball", GameVersion.SL);
        }, () =>
        {
            if (move is null || ability is null || item is null ||
                string.IsNullOrWhiteSpace(move.Description) || string.IsNullOrWhiteSpace(ability.Description) ||
                string.IsNullOrWhiteSpace(item.Description))
            {
                throw new InvalidOperationException("Description fixture lookup returned missing data.");
            }
            var actualDescriptionSignature = $"{move.Name}|{move.Description}|{ability.Name}|{ability.Description}|{item.Name}|{item.Description}";
            descriptionSignature ??= actualDescriptionSignature;
            if (actualDescriptionSignature != descriptionSignature) throw new InvalidOperationException("Descriptions changed between iterations.");
            return Task.CompletedTask;
        }, "Fresh DescriptionService; three datasets/first queries. HTTP/server caches follow harness policy."));

        var pokemon = ApplicationBenchmarkFixtures.CreatePokemon(config.PokemonCount);
        var expectedBytes = ApplicationBenchmarkFixtures.GetStoredBytes(pokemon);
        var importBytes = ApplicationBenchmarkFixtures.CreateBankImport(pokemon, expectedBytes);
        fixtures["bank-json-sha256"] = await bridge.InvokeAsync<string>("fingerprint", importBytes);
        fixtures["bank-json-bytes"] = importBytes.Length.ToString(CultureInfo.InvariantCulture);
        await using var bank = new BankService(js);
        if ((await bank.GetAllAsync()).Count != 0) throw new InvalidOperationException("Benchmark Bank must start empty.");
        var expectedCount = 0;
        results.Add(await measure.RunAsync("bank-add-range", config.Iterations, pokemon.Length, async () =>
        {
            await bank.AddRangeAsync(pokemon, ApplicationBenchmarkFixtures.Tag, ApplicationBenchmarkFixtures.Source);
            expectedCount += pokemon.Length;
        }, () => VerifyBankAsync(bank, expectedBytes, expectedCount, importedStart: int.MaxValue),
            "Synthetic PK9 stored bytes, Base64/source-gen/interop and one committed IndexedDB transaction; append-only."));

        var importedStart = expectedCount;
        results.Add(await measure.RunAsync("bank-json-import", config.Iterations, pokemon.Length, async () =>
        {
            await bank.ImportAsync(importBytes);
            expectedCount += pokemon.Length;
        }, () => VerifyBankAsync(bank, expectedBytes, expectedCount, importedStart),
            $"Fixed {importBytes.Length}-byte JSON payload; byte-array interop, decode/parse, one IndexedDB commit. Existing synthetic rows retained."));

        IReadOnlyList<BankEntry> readEntries = [];
        results.Add(await measure.RunAsync("bank-read", config.Iterations, expectedCount, async () =>
        {
            readEntries = await bank.GetAllAsync();
        }, () =>
        {
            VerifyBankEntries(readEntries, expectedBytes, expectedCount, importedStart);
            return Task.CompletedTask;
        }, "Read all synthetic rows through real JSON/Base64/PKM rehydration. Verification is outside timing/counters."));

        // Verify the real export path too, outside the timed import/read operations.
        var exported = JsonSerializer.Deserialize(await bank.ExportAsync(), BankService.BankJsonContext.Default.RawEntryArray)
                       ?? throw new InvalidOperationException("Bank export returned null.");
        if (exported.Length != expectedCount || exported.Select(entry => entry.Id).Distinct().Count() != expectedCount)
        {
            throw new InvalidOperationException("Bank export lost rows or unique IDs.");
        }
        for (var i = 0; i < exported.Length; i++)
        {
            var entry = exported[i];
            var expected = pokemon[i % pokemon.Length];
            if (entry.BytesBase64 != Convert.ToBase64String(expectedBytes[i % pokemon.Length]) ||
                entry.Meta.Species != expected.Species || entry.Meta.Nickname != expected.Nickname ||
                entry.Meta.IsShiny != expected.IsShiny || entry.Meta.Ext != expected.Extension ||
                entry.Meta.Tag != ApplicationBenchmarkFixtures.Tag || entry.Meta.SourceSave != ApplicationBenchmarkFixtures.Source ||
                (i >= importedStart && DateTimeOffset.Parse(entry.AddedAt) != ApplicationBenchmarkFixtures.ImportedAt))
                throw new InvalidOperationException("Bank export changed Pokémon bytes or metadata.");
        }

        foreach (var manic in new[] { false, true })
        {
            var kind = manic ? SaveArchiveKind.ManicEmu : SaveArchiveKind.GenericZip;
            var prefix = manic ? "manic" : "generic-zip";
            var originalSave = ApplicationBenchmarkFixtures.CreateSave(12345);
            var editedSave = ApplicationBenchmarkFixtures.CreateSave(23456);
            var input = ApplicationBenchmarkFixtures.CreateArchive(originalSave, manic);
            var inputSnapshot = (byte[])input.Clone();
            fixtures[$"{prefix}-input-sha256"] = await bridge.InvokeAsync<string>("fingerprint", input);
            fixtures[$"{prefix}-save-sha256"] = await bridge.InvokeAsync<string>("fingerprint", originalSave);
            SaveFile? loaded = null;
            SaveArchiveContext? context = null;
            results.Add(await measure.RunAsync($"{prefix}-load", config.ArchiveIterations, 1, () =>
            {
                if (!SaveFileLoader.TryLoad(input, "benchmark.3ds.sav", out loaded, out context))
                    throw new InvalidOperationException("Synthetic archive could not be loaded.");
                return Task.CompletedTask;
            }, () =>
            {
                if (loaded is not SAV7SM || loaded.Money != 12345 || context?.Kind != kind)
                    throw new InvalidOperationException("Archive load lost its save or wrapper context.");
                if (!input.AsSpan().SequenceEqual(inputSnapshot)) throw new InvalidOperationException("Archive input mutated.");
                return Task.CompletedTask;
            }, $"{input.Length}-byte archive, {originalSave.Length}-byte synthetic Gen 7 save; production loader includes wrapper detection."));

            byte[] rebuilt = [];
            results.Add(await measure.RunAsync($"{prefix}-export", config.ArchiveIterations, 1, () =>
            {
                rebuilt = SaveArchiveHelper.RebuildZip(context!, editedSave);
                return Task.CompletedTask;
            }, () =>
            {
                ApplicationBenchmarkFixtures.VerifyArchive(rebuilt, editedSave, kind, 23456);
                if (!input.AsSpan().SequenceEqual(inputSnapshot)) throw new InvalidOperationException("Archive export mutated its original buffer.");
                return Task.CompletedTask;
            }, "Production archive rebuild; edited save bytes, metadata, paths, timestamps, wrapper kind and reload validated outside timing."));
        }

        return new BenchmarkReport
        {
            Date = DateTimeOffset.UtcNow,
            UserAgent = "",
            Suite = "application",
            Results = results,
            Fixtures = fixtures,
            Notes = "One warm-up per workload. Timings and managed counters exclude validation. Allocation deltas are process-wide, include harness/other managed activity, and exclude JS/native allocations. Heap values are snapshots without forced GC, not peak memory. Event-loop delay covers each whole measured batch including validation/counter overhead; it is not a real large-list interaction test. Bank is append-only in an initially empty harness context; discard the context to clean up.",
        };
    }

    private static async Task VerifyBankAsync(BankService bank, byte[][] bytes, int count, int importedStart) =>
        VerifyBankEntries(await bank.GetAllAsync(), bytes, count, importedStart);

    internal static void VerifyBankEntries(IReadOnlyList<BankEntry> entries, byte[][] bytes, int count, int importedStart)
    {
        if (entries.Count != count || entries.Select(entry => entry.Id).Distinct().Count() != count)
            throw new InvalidOperationException("Bank row count or IDs changed.");
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var actual = new byte[entry.Pokemon.SIZE_STORED];
            entry.Pokemon.WriteDecryptedDataStored(actual);
            if (!actual.AsSpan().SequenceEqual(bytes[i % bytes.Length]) ||
                entry.Tag != ApplicationBenchmarkFixtures.Tag || entry.SourceSave != ApplicationBenchmarkFixtures.Source ||
                entry.AddedAt == default || (i >= importedStart && entry.AddedAt != ApplicationBenchmarkFixtures.ImportedAt))
                throw new InvalidOperationException("Bank Pokémon bytes or metadata changed.");
        }
    }

    private sealed class Measurement(IJSObjectReference bridge)
    {
        private readonly bool allocationsAvailable = ProbeAllocationCounter();

        internal async Task<BenchmarkResult> RunAsync(string name, int iterations, int operations,
            Func<Task> operation, Func<Task> verify, string notes)
        {
            for (var i = 0; i < WarmupIterations; i++)
            {
                await operation();
                await verify();
            }

            var timings = new double[iterations];
            var collections = new int[GC.MaxGeneration + 1];
            long allocated = 0;
            var heapBefore = ReadHeap();
            string sampleJson;
            await bridge.InvokeVoidAsync("startSampling");
            try
            {
                var stopwatch = new Stopwatch();
                for (var i = 0; i < iterations; i++)
                {
                    var gcBefore = collections.Select((_, generation) => GC.CollectionCount(generation)).ToArray();
                    var allocatedBefore = allocationsAvailable ? GC.GetTotalAllocatedBytes(precise: true) : 0;
                    stopwatch.Restart();
                    await operation();
                    stopwatch.Stop();
                    timings[i] = stopwatch.Elapsed.TotalMilliseconds;
                    if (allocationsAvailable) allocated += GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
                    for (var generation = 0; generation < collections.Length; generation++)
                        collections[generation] += GC.CollectionCount(generation) - gcBefore[generation];
                    await verify();
                }
            }
            finally
            {
                sampleJson = await bridge.InvokeAsync<string>("stopSampling");
            }
            var heapAfter = ReadHeap();
            using var sample = JsonDocument.Parse(sampleJson);
            return BenchmarkRunner.Summarize(name, timings, operations) with
            {
                ManagedAllocatedBytes = allocationsAvailable ? allocated : null,
                CollectionsByGeneration = collections,
                ManagedHeapBeforeBytes = heapBefore,
                ManagedHeapAfterBytes = heapAfter,
                MaxEventLoopDelayMs = sample.RootElement.GetProperty("MaxDelayMs").GetDouble(),
                EventLoopSamples = sample.RootElement.GetProperty("Samples").GetInt32(),
                Notes = notes,
            };
        }

        private static bool ProbeAllocationCounter()
        {
            try
            {
                var before = GC.GetTotalAllocatedBytes(precise: true);
                var probe = new byte[4096];
                var after = GC.GetTotalAllocatedBytes(precise: true);
                GC.KeepAlive(probe);
                return after - before >= probe.Length;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }

        private static long? ReadHeap()
        {
            try { return GC.GetTotalMemory(forceFullCollection: false); }
            catch (NotSupportedException) { return null; }
        }
    }
}

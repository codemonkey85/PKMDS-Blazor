using System.Diagnostics;
using Pkmds.Rcl.Models;

namespace Pkmds.Rcl.Services;

/// <summary>
/// Runs deterministic micro-benchmarks against PKHeX hot paths.
/// Used by the /bench page to compare runtime perf between WASM build configs
/// (SIMD, AOT, trim, ...) — see issue #883 and measure-publish.ps1.
/// </summary>
public sealed class BenchmarkRunner
{
    private static readonly ushort[] BenchSpecies =
    [
        (ushort)Species.Pikachu,        // 25
        (ushort)Species.Charizard,      // 6
        (ushort)Species.Garchomp,       // 445
        (ushort)Species.Sprigatito,     // 906
        (ushort)Species.IronHands,      // 1003
        (ushort)Species.IronValiant,    // 1006
    ];

    private const int CopiesPerSpecies = 5;
    private const int WarmupIterations = 5;

    public record BenchmarkConfig(
        int LegalityIterations = 100,
        int EncryptionIterations = 5000,
        int SearchIterations = 200,
        int EncounterIterations = 10,
        int BatchLookupIterations = 1000);

    public BenchmarkReport Run(BenchmarkConfig? config = null)
    {
        config ??= new BenchmarkConfig();

        var sav = BlankSaveFile.Get(GameVersion.SL, "BENCH", LanguageID.English);
        var pkms = BuildBenchPkms(sav);
        PopulateBox(sav, pkms);

        var results = new List<BenchmarkResult>
        {
            RunLegality(pkms, config.LegalityIterations),
            RunEncryption(pkms, config.EncryptionIterations),
            RunSearch(sav, config.SearchIterations),
            RunEncounterGeneration(sav, config.EncounterIterations),
            RunBatchLookup(pkms, config.BatchLookupIterations),
        };

        return new BenchmarkReport
        {
            Date = DateTimeOffset.UtcNow,
            UserAgent = "",
            Results = results,
        };
    }

    private static PK9[] BuildBenchPkms(SaveFile sav)
    {
        var list = new List<PK9>(BenchSpecies.Length * CopiesPerSpecies);
        foreach (var species in BenchSpecies)
        {
            var template = (PK9)sav.BlankPKM.Clone();
            template.Species = species;
            var enc = EncounterMovesetGenerator.GenerateEncounters(template, sav, ReadOnlyMemory<ushort>.Empty).FirstOrDefault();
            if (enc is null)
            {
                throw new InvalidOperationException($"No encounter generated for species {species} — bench cannot proceed.");
            }
            var pkm = (PK9)enc.ConvertToPKM(sav);
            for (var i = 0; i < CopiesPerSpecies; i++)
            {
                list.Add((PK9)pkm.Clone());
            }
        }
        return [.. list];
    }

    private static void PopulateBox(SaveFile sav, PK9[] pkms)
    {
        for (var i = 0; i < pkms.Length && i < sav.BoxCount * sav.BoxSlotCount; i++)
        {
            var box = i / sav.BoxSlotCount;
            var slot = i % sav.BoxSlotCount;
            sav.SetBoxSlotAtIndex(pkms[i], box, slot);
        }
    }

    private static BenchmarkResult RunLegality(PK9[] pkms, int iterations)
    {
        for (var w = 0; w < WarmupIterations; w++)
        {
            foreach (var pkm in pkms)
            {
                _ = new LegalityAnalysis(pkm);
            }
        }

        var timings = new double[iterations];
        var sw = new Stopwatch();
        for (var it = 0; it < iterations; it++)
        {
            sw.Restart();
            foreach (var pkm in pkms)
            {
                _ = new LegalityAnalysis(pkm);
            }
            sw.Stop();
            timings[it] = sw.Elapsed.TotalMilliseconds;
        }

        return Summarize("legality", timings, opsPerIteration: pkms.Length);
    }

    private static BenchmarkResult RunEncryption(PK9[] pkms, int iterations)
    {
        var buf = new byte[pkms[0].SIZE_PARTY];

        for (var w = 0; w < WarmupIterations; w++)
        {
            foreach (var pkm in pkms)
            {
                pkm.WriteEncryptedDataParty(buf);
                _ = new PK9(buf.AsMemory().ToArray());
            }
        }

        var timings = new double[iterations];
        var sw = new Stopwatch();
        for (var it = 0; it < iterations; it++)
        {
            sw.Restart();
            foreach (var pkm in pkms)
            {
                pkm.WriteEncryptedDataParty(buf);
                _ = new PK9(buf.AsMemory().ToArray());
            }
            sw.Stop();
            timings[it] = sw.Elapsed.TotalMilliseconds;
        }

        return Summarize("encryption", timings, opsPerIteration: pkms.Length);
    }

    private static BenchmarkResult RunSearch(SaveFile sav, int iterations)
    {
        var totalSlots = sav.BoxCount * sav.BoxSlotCount;
        var targetSpecies = BenchSpecies[2];

        for (var w = 0; w < WarmupIterations; w++)
        {
            _ = CountMatches(sav, targetSpecies);
        }

        var timings = new double[iterations];
        var sw = new Stopwatch();
        for (var it = 0; it < iterations; it++)
        {
            sw.Restart();
            _ = CountMatches(sav, targetSpecies);
            sw.Stop();
            timings[it] = sw.Elapsed.TotalMilliseconds;
        }

        return Summarize("search", timings, opsPerIteration: totalSlots);
    }

    private static int CountMatches(SaveFile sav, ushort targetSpecies)
    {
        var count = 0;
        for (var box = 0; box < sav.BoxCount; box++)
        {
            for (var slot = 0; slot < sav.BoxSlotCount; slot++)
            {
                var pkm = sav.GetBoxSlotAtIndex(box, slot);
                if (pkm.Species == targetSpecies && pkm.IV_HP >= 0 && pkm.CurrentLevel > 0)
                {
                    count++;
                }
            }
        }
        return count;
    }

    private static BenchmarkResult RunEncounterGeneration(SaveFile sav, int iterations)
    {
        var template = (PK9)sav.BlankPKM.Clone();

        var opsPerIteration = 0;
        for (var w = 0; w < WarmupIterations; w++)
        {
            opsPerIteration = CountAllEncounters(sav, template);
        }

        var timings = new double[iterations];
        var sw = new Stopwatch();
        for (var it = 0; it < iterations; it++)
        {
            sw.Restart();
            CountAllEncounters(sav, template);
            sw.Stop();
            timings[it] = sw.Elapsed.TotalMilliseconds;
        }

        return Summarize("encounter-generation", timings, opsPerIteration: opsPerIteration);
    }

    private static int CountAllEncounters(SaveFile sav, PK9 template)
    {
        var total = 0;
        foreach (var species in BenchSpecies)
        {
            template.Species = species;
            foreach (var _ in EncounterMovesetGenerator.GenerateEncounters(template, sav, ReadOnlyMemory<ushort>.Empty))
            {
                total++;
            }
        }
        return total;
    }

    // Smoke test for the reflective property-lookup path that BatchEditingBase
    // uses (see issue #883 trim work). EntityBatchEditor's ctor enumerates
    // ReflectUtil.GetAllPropertyInfoCanWritePublic for each PKM type at startup
    // and caches the PropertyInfo dicts. Under TrimMode=full, if PKM members
    // were stripped, those dicts would be empty and TryGetHasProperty would
    // silently return false for every name — the user-visible symptom is the
    // Batch Editor tab returning zero matches or no-op modifies.
    //
    // We make this loud by asserting at start: if any of the six well-known
    // writable PKM properties fails to resolve, throw with a clear message
    // that the bench page propagates to data-bench-state="error" and the
    // Playwright runner surfaces as a failed publish.
    private static readonly string[] BatchLookupProperties =
    [
        nameof(PKM.Species),
        nameof(PKM.HeldItem),
        nameof(PKM.Nature),
        nameof(PKM.Move1),
        nameof(PKM.OriginalTrainerFriendship),
        nameof(PKM.IV_HP),
    ];

    private static BenchmarkResult RunBatchLookup(PK9[] pkms, int iterations)
    {
        var editor = EntityBatchEditor.Instance;
        var pkm = pkms[0];

        foreach (var name in BatchLookupProperties)
        {
            if (!editor.TryGetHasProperty(pkm, name.AsSpan(), out var pi) || pi is null)
            {
                throw new InvalidOperationException(
                    $"BatchEditor reflection check failed: TryGetHasProperty returned no PropertyInfo for PK9.{name}. " +
                    "PKM members likely got trimmed away — verify Pkmds.Web/PreservePkmTypes.xml covers this PKM subclass.");
            }
        }

        for (var w = 0; w < WarmupIterations; w++)
        {
            foreach (var name in BatchLookupProperties)
            {
                _ = editor.TryGetHasProperty(pkm, name.AsSpan(), out _);
            }
        }

        var timings = new double[iterations];
        var sw = new Stopwatch();
        for (var it = 0; it < iterations; it++)
        {
            sw.Restart();
            foreach (var name in BatchLookupProperties)
            {
                _ = editor.TryGetHasProperty(pkm, name.AsSpan(), out _);
            }
            sw.Stop();
            timings[it] = sw.Elapsed.TotalMilliseconds;
        }

        return Summarize("batch-lookup", timings, opsPerIteration: BatchLookupProperties.Length);
    }

    private static BenchmarkResult Summarize(string name, double[] timings, int opsPerIteration)
    {
        var total = 0.0;
        var min = double.MaxValue;
        var max = double.MinValue;
        foreach (var t in timings)
        {
            total += t;
            if (t < min) min = t;
            if (t > max) max = t;
        }
        var mean = total / timings.Length;

        var varianceSum = 0.0;
        foreach (var t in timings)
        {
            var d = t - mean;
            varianceSum += d * d;
        }
        var stdDev = timings.Length > 1 ? Math.Sqrt(varianceSum / (timings.Length - 1)) : 0.0;

        return new BenchmarkResult
        {
            Name = name,
            Iterations = timings.Length,
            OpsPerIteration = opsPerIteration,
            TotalMs = total,
            MeanMs = mean,
            MinMs = min,
            MaxMs = max,
            StdDevMs = stdDev,
        };
    }
}
